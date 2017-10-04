﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Renci.SshNet.Common;

namespace Renci.SshNet.Tests.Classes
{
    /// <summary>
    /// ConnectionInfo provides 'keyboard-interactive', 'password', 'publickey' and  authentication methods, and partial
    /// success limit is set to 2.
    ///
    /// Authentication proceeds as follows:
    /// 
    /// 1 x     * Client performs 'none' authentication attempt.
    ///         * Server responds with 'failure', and 'password' allowed authentication method.
    /// 
    /// 1 x     * Client performs 'password' authentication attempt.
    ///         * Server responds with 'partial success', and 'password' & 'publickey' allowed authentication methods.
    /// 
    /// 1 x     * Client performs 'publickey' authentication attempt.
    ///         * Server responds with 'failure'.
    ///
    /// 1 x     * Client performs 'password' authentication attempt.
    ///         * Server responds with 'partial success', and 'keyboard-interactive' allowed authentication methods.
    /// 
    /// 1 x     * Client performs 'keyboard-interactive' authentication attempt.
    ///         * Server responds with 'failure'.
    ///
    /// Since the server only ever allowed the 'password' authentication method, there are no
    /// authentication methods left to try after reaching the partial success limit for 'password'
    /// and as such authentication fails.
    /// </summary>
    [TestClass]
    public class ClientAuthenticationTest_Success_MultiList_PartialSuccessLimitReachedFollowedByFailureInSameBranch : ClientAuthenticationTestBase
    {
        private int _partialSuccessLimit;
        private ClientAuthentication _clientAuthentication;
        private SshAuthenticationException _actualException;

        protected override void SetupData()
        {
            _partialSuccessLimit = 2;
        }

        protected override void SetupMocks()
        {
            var seq = new MockSequence();

            SessionMock.InSequence(seq).Setup(p => p.RegisterMessage("SSH_MSG_USERAUTH_FAILURE"));
            SessionMock.InSequence(seq).Setup(p => p.RegisterMessage("SSH_MSG_USERAUTH_SUCCESS"));
            SessionMock.InSequence(seq).Setup(p => p.RegisterMessage("SSH_MSG_USERAUTH_BANNER"));

            ConnectionInfoMock.InSequence(seq).Setup(p => p.CreateNoneAuthenticationMethod())
                              .Returns(NoneAuthenticationMethodMock.Object);

            NoneAuthenticationMethodMock.InSequence(seq).Setup(p => p.Authenticate(SessionMock.Object))
                                        .Returns(AuthenticationResult.Failure);
            ConnectionInfoMock.InSequence(seq)
                              .Setup(p => p.AuthenticationMethods)
                              .Returns(new List<IAuthenticationMethod>
                                  {
                                      KeyboardInteractiveAuthenticationMethodMock.Object,
                                      PasswordAuthenticationMethodMock.Object,
                                      PublicKeyAuthenticationMethodMock.Object
                                  });
            NoneAuthenticationMethodMock.InSequence(seq)
                                        .Setup(p => p.AllowedAuthentications)
                                        .Returns(new[] {"password"});

            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive");
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("password");
            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("publickey");
            PasswordAuthenticationMethodMock.InSequence(seq)
                                            .Setup(p => p.Authenticate(SessionMock.Object))
                                            .Returns(AuthenticationResult.PartialSuccess);
            PasswordAuthenticationMethodMock.InSequence(seq)
                                            .Setup(p => p.AllowedAuthentications)
                                            .Returns(new[] {"password", "publickey"});

            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive");
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("password");
            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("publickey");
            PublicKeyAuthenticationMethodMock.InSequence(seq)
                                             .Setup(p => p.Authenticate(SessionMock.Object))
                                             .Returns(AuthenticationResult.Failure);
            PublicKeyAuthenticationMethodMock.InSequence(seq)
                                             .Setup(p => p.Name)
                                             .Returns("publickey-failure");

            PasswordAuthenticationMethodMock.InSequence(seq)
                                            .Setup(p => p.Authenticate(SessionMock.Object))
                                            .Returns(AuthenticationResult.PartialSuccess);
            PasswordAuthenticationMethodMock.InSequence(seq)
                                            .Setup(p => p.AllowedAuthentications)
                                            .Returns(new[] {"keyboard-interactive"});

            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive");
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("password");
            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("publickey");
            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq)
                                                       .Setup(p => p.Authenticate(SessionMock.Object))
                                                       .Returns(AuthenticationResult.Failure);
            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive-failure");

            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_FAILURE"));
            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_SUCCESS"));
            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_BANNER"));
        }

        protected override void Arrange()
        {
            base.Arrange();

            _clientAuthentication = new ClientAuthentication(_partialSuccessLimit);
        }

        protected override void Act()
        {
            try
            {
                _clientAuthentication.Authenticate(ConnectionInfoMock.Object, SessionMock.Object);
                Assert.Fail();
            }
            catch (SshAuthenticationException ex)
            {
                _actualException = ex;
            }
        }

        [TestMethod]
        public void AuthenticateOnKeyboardInteractiveAuthenticationMethodShouldHaveBeenInvokedOnce()
        {
            KeyboardInteractiveAuthenticationMethodMock.Verify(p => p.Authenticate(SessionMock.Object), Times.Once);
        }

        [TestMethod]
        public void AuthenticateShouldThrowSshAuthenticationException()
        {
            Assert.IsNotNull(_actualException);
            Assert.IsNull(_actualException.InnerException);
            Assert.AreEqual("Permission denied (keyboard-interactive-failure).", _actualException.Message);
        }
    }
}
