using MerchForge.api.Enums;

namespace MerchForge.api.Models
{
    public class BusinessUserRole
    {
        public Guid Id;
        public BusinessRole Role;


        public ICollection<BusinessUser> BusinessMemberships { get; set; }
                = new List<BusinessUser>();
    }
}
