﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Renci.SshNet.Tests.Classes
{
    [TestClass]
    public class ClientAuthenticationTest_Success_MultiList_DifferentAllowedAuthenticationsAfterPartialSuccess : ClientAuthenticationTestBase
    {
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
            ConnectionInfoMock.InSequence(seq).Setup(p => p.AuthenticationMethods)
                            .Returns(new List<IAuthenticationMethod>
                {
                    PasswordAuthenticationMethodMock.Object,
                    PublicKeyAuthenticationMethodMock.Object,
                    KeyboardInteractiveAuthenticationMethodMock.Object,
                });
            NoneAuthenticationMethodMock.InSequence(seq).Setup(p => p.AllowedAuthentications).Returns(new[] { "publickey", "password" });
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("password");
            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("publickey");
            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive");

            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Authenticate(SessionMock.Object))
                .Returns(AuthenticationResult.PartialSuccess);
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.AllowedAuthentications)
                .Returns(new[] { "keyboard-interactive", "publickey" });
            PasswordAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("password");
            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("publickey");
            KeyboardInteractiveAuthenticationMethodMock.InSequence(seq).Setup(p => p.Name).Returns("keyboard-interactive");

            PublicKeyAuthenticationMethodMock.InSequence(seq).Setup(p => p.Authenticate(SessionMock.Object)).Returns(AuthenticationResult.Success);

            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_FAILURE"));
            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_SUCCESS"));
            SessionMock.InSequence(seq).Setup(p => p.UnRegisterMessage("SSH_MSG_USERAUTH_BANNER"));
        }

        protected override void Act()
        {
            ClientAuthentication.Authenticate(ConnectionInfoMock.Object, SessionMock.Object);
        }
    }
}
