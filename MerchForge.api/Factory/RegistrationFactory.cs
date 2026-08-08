using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace MerchForge.api.Factory
{
    public class RegistrationFactory : IRegistrationFactory
    {
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegistrationFactory(IPasswordHasher<User> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public (User User, Business Business, BusinessUser BusinessUser)
            Create(RegisterRequest request)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = request.BusinessName,
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var businessUser = new BusinessUser
            {
                BusinessId = business.Id,
                UserId = user.Id,
                Role = BusinessRole.Owner,
                CreatedAt = business.CreatedAt
            };

            return (user, business, businessUser);
        }
    }
}
