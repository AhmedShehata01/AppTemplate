using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Extend;

namespace Kindergarten.DAL.Entity.DRBRA
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
