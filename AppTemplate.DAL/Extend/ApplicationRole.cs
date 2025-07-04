using AppTemplate.DAL.Entity.DRBRA;
using Microsoft.AspNetCore.Identity;

namespace AppTemplate.DAL.Extend
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole()
        {
            IsActive = true;
            IsExternal = false;
            CreatedOn = DateTime.Now.ToShortDateString();
            IsDeleted = false;
        }

        public bool IsActive { get; set; }
        public bool IsExternal { get; set; }
        public string CreatedOn { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<RoleSecuredRoute> RoleSecuredRoutes { get; set; }

    }
}
