﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Renci.SshNet.Channels;
using Renci.SshNet.Common;

namespace Renci.SshNet.Tests.Classes
{
    [TestClass]
    public class ForwardedPortDynamicTest_Started_SocketVersionNotSupported
    {
        private Mock<ISession> _sessionMock;
        private Mock<IChannelDirectTcpip> _channelMock;
        private Mock<IConnectionInfo> _connectionInfoMock;
        private ForwardedPortDynamic _forwardedPort;
        private Socket _client;
        private IList<EventArgs> _closingRegister;
        private IList<ExceptionEventArgs> _exceptionRegister;
        private int _bytesReceived;

        [TestInitialize]
        public void Initialize()
        {
            Arrange();
            Act();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_forwardedPort != null && _forwardedPort.IsStarted)
            {
                _sessionMock.Setup(p => p.ConnectionInfo).Returns(_connectionInfoMock.Object);
                _connectionInfoMock.Setup(p => p.Timeout).Returns(TimeSpan.FromSeconds(5));
                _forwardedPort.Stop();
            }
            if (_client != null)
            {
                if (_client.Connected)
                {
                    _client.Shutdown(SocketShutdown.Both);
                    _client.Close();
                    _client = null;
                }
            }
        }

        private void Arrange()
        {
            _closingRegister = new List<EventArgs>();
            _exceptionRegister = new List<ExceptionEventArgs>();

            _sessionMock = new Mock<ISession>(MockBehavior.Strict);
            _channelMock = new Mock<IChannelDirectTcpip>(MockBehavior.Strict);
            _connectionInfoMock = new Mock<IConnectionInfo>(MockBehavior.Strict);

            _sessionMock.Setup(p => p.IsConnected).Returns(true);
            _sessionMock.Setup(p => p.CreateChannelDirectTcpip()).Returns(_channelMock.Object);
            _channelMock.Setup(p => p.Close());
            _channelMock.Setup(p => p.Dispose());

            _forwardedPort = new ForwardedPortDynamic(8122);
            _forwardedPort.Closing += (sender, args) => _closingRegister.Add(args);
            _forwardedPort.Exception += (sender, args) => _exceptionRegister.Add(args);
            _forwardedPort.Session = _sessionMock.Object;
            _forwardedPort.Start();

            var endPoint = new IPEndPoint(IPAddress.Loopback, 8122);

            _client = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _client.Connect(endPoint);
        }

        private void Act()
        {
            var buffer = new byte[] {0x07};
            _client.Send(buffer, 0, buffer.Length, SocketFlags.None);
            _bytesReceived = _client.Receive(buffer, 0, buffer.Length, SocketFlags.None);
        }

        [TestMethod]
        public void SocketShouldBeConnected()
        {
            Assert.IsTrue(_client.Connected);
        }

        [TestMethod]
        public void ForwardedPortShouldShutdownSendOnSocket()
        {
            Assert.AreEqual(0, _bytesReceived);
        }

        [TestMethod]
        public void ClosingShouldNotHaveFired()
        {
            Assert.AreEqual(0, _closingRegister.Count);
        }

        [TestMethod]
        public void ExceptionShouldHaveFiredOnce()
        {
            Assert.AreEqual(1, _exceptionRegister.Count, GetExceptions());

            var exception = _exceptionRegister[0].Exception;
            Assert.IsNotNull(exception);
            var notSupportedException = exception as NotSupportedException;
            Assert.IsNotNull(notSupportedException, exception.ToString());
            Assert.AreEqual("SOCKS version 7 is not supported.", notSupportedException.Message);
        }

        [TestMethod]
        public void CreateChannelDirectTcpipOnSessionShouldBeInvokedOnce()
        {
            _sessionMock.Verify(p => p.CreateChannelDirectTcpip(), Times.Once);
        }

        [TestMethod]
        public void CloseOnChannelShouldBeInvokedOnce()
        {
            _channelMock.Verify(p => p.Close(), Times.Once);
        }

        [TestMethod]
        public void DisposeOnChannelShouldBeInvokedOnce()
        {
            _channelMock.Verify(p => p.Dispose(), Times.Once);
        }

        private string GetExceptions()
        {
            var sb = new StringBuilder();

            foreach (var exceptionEventArgs in _exceptionRegister)
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(exceptionEventArgs.Exception);
            }

            return sb.ToString();
        }
    }
}
