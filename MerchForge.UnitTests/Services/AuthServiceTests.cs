using MerchForge.api.Factory;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MerchForge.UnitTests.Services
{
    internal class AuthServiceTests
    {
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
        private readonly Mock<IRegistrationFactory> _registrationFactoryMock;

        public AuthServiceTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _jwtServiceMock = new Mock<IJwtService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _registrationFactoryMock = new Mock<IRegistrationFactory>();
        }




    }
}
