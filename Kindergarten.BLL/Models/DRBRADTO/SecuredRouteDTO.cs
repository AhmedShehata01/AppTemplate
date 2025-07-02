using System.ComponentModel.DataAnnotations;

namespace Kindergarten.BLL.Models.DRBRADTO
{
    public class SecuredRouteDTO
    {
        public int Id { get; set; }
        public string BasePath { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedById { get; set; }
        public string? CreatedByName { get; set; }

        public List<string> AssignedRoles { get; set; } = new();
    }

    public class CreateSecuredRouteDTO
    {
        public string BasePath { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string> RoleIds { get; set; } = new(); // ✅ فقط هذا الحقل يبقى
    }



    public class UpdateSecuredRouteDTO
    {
        public int Id { get; set; }
        public string BasePath { get; set; } = string.Empty;
        public string? Description { get; set; }

        public List<string> RoleIds { get; set; } = new(); // ✅ مضافة جديدة لتحديث الأدوار
    }


    public class AssignRolesToRouteDTO
    {
        [Required]
        public int SecuredRouteId { get; set; }

        [Required]
        public List<string> RoleIds { get; set; }
    }

    public class UnassignRoleFromRouteDTO
    {
        [Required]
        public int SecuredRouteId { get; set; }

        [Required]
        public string RoleId { get; set; }
    }

    public class RouteWithRolesDTO
    {
        public int RouteId { get; set; }
        public string BasePath { get; set; }
        public List<string> Roles { get; set; }
    }

}
