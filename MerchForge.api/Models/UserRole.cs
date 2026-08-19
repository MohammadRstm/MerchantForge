using MerchForge.api.Enums;
using System.Reflection;

namespace MerchForge.api.Models
{
    public class UserRole
    {
        public Guid Id { get; set; }
        public SystemRole Role { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; }
                = new List<User>();
    }
}
