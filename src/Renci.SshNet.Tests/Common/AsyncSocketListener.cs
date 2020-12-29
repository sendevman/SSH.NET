﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
#if !FEATURE_SOCKET_DISPOSE
using Renci.SshNet.Common;
#endif // !FEATURE_SOCKET_DISPOSE

namespace Renci.SshNet.Tests.Common
{
    public class AsyncSocketListener : IDisposable
    {
        private readonly IPEndPoint _endPoint;
        private readonly ManualResetEvent _acceptCallbackDone;
        private List<Socket> _connectedClients;
        private Socket _listener;
        private Thread _receiveThread;
        private bool _started;
        private object _syncLock;

        public delegate void BytesReceivedHandler(byte[] bytesReceived, Socket socket);
        public delegate void ConnectedHandler(Socket socket);

        public event BytesReceivedHandler BytesReceived;
        public event ConnectedHandler Connected;
        public event ConnectedHandler Disconnected;

        public AsyncSocketListener(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
            _acceptCallbackDone = new ManualResetEvent(false);
            _connectedClients = new List<Socket>();
            _syncLock = new object();
            ShutdownRemoteCommunicationSocket = true;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Socket.Shutdown(SocketShutdown)"/> is invoked on the <see cref="Socket"/>
        /// that is used to handle the communication with the remote host, when the remote host has closed the connection.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to invoke <see cref="Socket.Shutdown(SocketShutdown)"/> on the <see cref="Socket"/> that is used
        /// to handle the communication with the remote host, when the remote host has closed the connection; otherwise,
        /// <see langword="false""/>. The default is <see langword="true"/>.
        /// </value>
        public bool ShutdownRemoteCommunicationSocket { get; set; }

        public void Start()
        {
            _listener = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(_endPoint);
            _listener.Listen(1);

            _started = true;

            _receiveThread = new Thread(StartListener);
            _receiveThread.Start(_listener);
        }

        public void Stop()
        {
            _started = false;

            lock (_syncLock)
            {
                foreach (var connectedClient in _connectedClients)
                {
                    try
                    {
                        connectedClient.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("[{0}] Failure shutting down socket: {1}",
                                                typeof(AsyncSocketListener).FullName,
                                                ex);
                    }

                    DrainSocket(connectedClient);
                }
            }

            if (_listener != null)
            {
                _listener.Dispose();
            }

            if (_receiveThread != null)
            {
                _receiveThread.Join();
                _receiveThread = null;
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private void StartListener(object state)
        {
            var listener = (Socket)state;
            while (_started)
            {
                _acceptCallbackDone.Reset();
                listener.BeginAccept(AcceptCallback, listener);
                _acceptCallbackDone.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue
            _acceptCallbackDone.Set();

            // Get the socket that listens for inbound connections
            var listener = (Socket) ar.AsyncState;

            // Get the socket that handles the client request
            var handler = listener.EndAccept(ar);

            // Signal new connection
            SignalConnected(handler);

            // Register client socket
            _connectedClients.Add(handler);

            try
            {
                var state = new SocketStateObject(handler);
                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, ReadCallback, state);
            }
            catch (ObjectDisposedException)
            {
                // when the socket is closed, an ObjectDisposedException is thrown
                // by Socket.EndAccept(IAsyncResult)
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object
            var state = (SocketStateObject) ar.AsyncState;
            var handler = state.Socket;

            int bytesRead;
            try
            {
                // Read data from the client socket.
                bytesRead = handler.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                // when the socket is closed, the callback will be invoked for any pending BeginReceive
                // we could use the Socket.Connected property to detect this here, but the proper thing
                // to do is invoke EndReceive knowing that it will throw an ObjectDisposedException
                return;
            }

            if (bytesRead > 0)
            {
                var bytesReceived = new byte[bytesRead];
                Array.Copy(state.Buffer, bytesReceived, bytesRead);
                SignalBytesReceived(bytesReceived, handler);
                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, ReadCallback, state);
            }
            else
            {
                SignalDisconnected(handler);

                if (ShutdownRemoteCommunicationSocket)
                {
                    lock (_syncLock)
                    {
                        handler.Shutdown(SocketShutdown.Send);
                        handler.Close();

                        _connectedClients.Remove(handler);
                    }
                }
            }
        }

        private void SignalBytesReceived(byte[] bytesReceived, Socket client)
        {
            var subscribers = BytesReceived;
            if (subscribers != null)
                subscribers(bytesReceived, client);
        }

        private void SignalConnected(Socket client)
        {
            var subscribers = Connected;
            if (subscribers != null)
                subscribers(client);
        }

        private void SignalDisconnected(Socket client)
        {
            var subscribers = Disconnected;
            if (subscribers != null)
                subscribers(client);
        }

        private static void DrainSocket(Socket socket)
        {
            var buffer = new byte[128];

            try
            {
                while (true && socket.Connected)
                {
                    var bytesRead = socket.Receive(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine("[{0}] Failure draining socket ({1}): {2}",
                                        typeof(AsyncSocketListener).FullName,
                                        ex.SocketErrorCode.ToString("G"),
                                        ex);
            }
        }

        private class SocketStateObject
        {
            public Socket Socket { get; private set; }

            public readonly byte[] Buffer = new byte[1024];

            public SocketStateObject(Socket handler)
            {
                Socket = handler;
            }
        }
    }
}
