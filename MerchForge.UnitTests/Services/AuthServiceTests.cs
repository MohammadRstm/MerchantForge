using FluentAssertions;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Identity;
using MerchForge.api.DTOs.Auth;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MerchForge.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<IPasswordHasher<User>> _passwordHasher;
        private readonly Mock<IJwtService> _jwtService;
        private readonly Mock<IRefreshTokenService> _refreshTokenService;

        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepository = new Mock<IUserRepository>();
            _passwordHasher = new Mock<IPasswordHasher<User>>();
            _jwtService = new Mock<IJwtService>();
            _refreshTokenService = new Mock<IRefreshTokenService>();

            _authService = new AuthService(
                _userRepository.Object,
                _passwordHasher.Object,
                _jwtService.Object,
                _refreshTokenService.Object);
        }

        [Fact]
        public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowInvalidCredentialsException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            _userRepository
                .Setup(x => x.GetByEmailAsync(
                    request.Email,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> act = () => _authService.LoginAsync(request);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidCredentialsException>();
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowInvalidCredentialsException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@test.com",
                Password = "WrongPassword"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = "hashed-password"
            };

            _userRepository
                .Setup(x => x.GetByEmailAsync(
                    request.Email,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasher
                .Setup(x => x.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password))
                .Returns(PasswordVerificationResult.Failed);

            // Act
            Func<Task> act = () => _authService.LoginAsync(request);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidCredentialsException>();
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = "hashed-password"
            };

            var expiration = DateTime.UtcNow.AddMinutes(15);

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id
            };

            _userRepository
                .Setup(x => x.GetByEmailAsync(
                    request.Email,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasher
                .Setup(x => x.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password))
                .Returns(PasswordVerificationResult.Success);

            _refreshTokenService
                .Setup(x => x.CreateAsync(
                    user,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(("refresh-token", refreshTokenEntity));

            _jwtService
                .Setup(x => x.GenerateAccessToken(user))
                .ReturnsAsync("access-token");

            _jwtService
                .Setup(x => x.GetExpirationTime())
                .Returns(expiration);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.AccessTokenExpiresAt.Should().Be(expiration);
        }

        [Fact]
        public async Task RefreshAsync_WhenTokenIsInvalid_ShouldThrowInvalidRefreshTokenException()
        {
            // Arrange
            _refreshTokenService
                .Setup(x => x.GetValidTokenAsync(
                    "invalid-token",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Func<Task> act = () =>
                _authService.RefreshAsync("invalid-token");

            // Assert
            await act.Should()
                .ThrowAsync<InvalidRefreshTokenException>();
        }



    }
}
