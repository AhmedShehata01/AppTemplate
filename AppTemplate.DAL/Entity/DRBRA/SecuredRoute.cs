using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppTemplate.DAL.Extend;

namespace AppTemplate.DAL.Entity.DRBRA
{
    public class SecuredRoute
    {
        public int Id { get; set; }

        [Required]
        public string BasePath { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        public string CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; }

        public ICollection<RoleSecuredRoute> RoleSecuredRoutes { get; set; }
    }
}
