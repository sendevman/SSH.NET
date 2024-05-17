﻿using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Renci.SshNet.Channels;
using Renci.SshNet.Common;

namespace Renci.SshNet.Tests.Classes
{
    [TestClass]
    public class ServiceFactoryTest_CreateShellStream_SendPseudoTerminalRequestReturnsFalse
    {
        private Mock<ISession> _sessionMock;
        private Mock<IConnectionInfo> _connectionInfoMock;
        private Mock<IChannelSession> _channelSessionMock;
        private ServiceFactory _serviceFactory;
        private string _terminalName;
        private uint _columns;
        private uint _rows;
        private uint _width;
        private uint _height;
        private IDictionary<TerminalModes, uint> _terminalModeValues;
        private int _bufferSize;
        private SshException _actualException;

        private void SetupData()
        {
            _terminalName = "test";
            _columns = 80;
            _rows = 20;
            _width = 300;
            _height = 100;
            _terminalModeValues = new Dictionary<TerminalModes, uint>();
            _bufferSize = 512;
            _actualException = null;
        }

        private void CreateMocks()
        {
            _sessionMock = new Mock<ISession>(MockBehavior.Strict);
            _connectionInfoMock = new Mock<IConnectionInfo>(MockBehavior.Strict);
            _channelSessionMock = new Mock<IChannelSession>(MockBehavior.Strict);
        }

        private void SetupMocks()
        {
            var sequence = new MockSequence();

            _sessionMock.InSequence(sequence)
                        .Setup(p => p.ConnectionInfo)
                        .Returns(_connectionInfoMock.Object);
            _connectionInfoMock.InSequence(sequence)
                               .Setup(p => p.Encoding)
                               .Returns(new UTF8Encoding());
            _sessionMock.InSequence(sequence)
                        .Setup(p => p.CreateChannelSession())
                        .Returns(_channelSessionMock.Object);
            _channelSessionMock.InSequence(sequence)
                               .Setup(p => p.Open());
            _channelSessionMock.InSequence(sequence)
                               .Setup(p => p.SendPseudoTerminalRequest(_terminalName, _columns, _rows, _width, _height, _terminalModeValues))
                               .Returns(false);
            _channelSessionMock.InSequence(sequence)
                               .Setup(p => p.Dispose());
        }

        private void Arrange()
        {
            SetupData();
            CreateMocks();
            SetupMocks();

            _serviceFactory = new ServiceFactory();
        }

        [TestInitialize]
        public void Initialize()
        {
            Arrange();
            Act();
        }

        private void Act()
        {
            try
            {
                _serviceFactory.CreateShellStream(_sessionMock.Object,
                                                  _terminalName,
                                                  _columns,
                                                  _rows,
                                                  _width,
                                                  _height,
                                                  _terminalModeValues,
                                                  _bufferSize);
                Assert.Fail();
            }
            catch (SshException ex)
            {
                _actualException = ex;
            }
        }

        [TestMethod]
        public void CreateShellStreamShouldThrowSshException()
        {
            Assert.IsNotNull(_actualException);
            Assert.IsNull(_actualException.InnerException);
            Assert.AreEqual("The pseudo-terminal request was not accepted by the server. Consult the server log for more information.", _actualException.Message);
        }

        [TestMethod]
        public void DisposeOnChannelSessionShouldHaveBeenInvokedOnce()
        {
            _channelSessionMock.Verify(p => p.Dispose(), Times.Once);
        }
    }
}
