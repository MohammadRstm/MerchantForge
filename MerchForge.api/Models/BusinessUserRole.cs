using MerchForge.api.Enums;

namespace MerchForge.api.Models
{
    public class BusinessUserRole
    {
        public Guid Id { get; set; }
        public BusinessRole Role { get; set; }


        public ICollection<BusinessUser> BusinessMemberships { get; set; }
                = new List<BusinessUser>();
    }
}
