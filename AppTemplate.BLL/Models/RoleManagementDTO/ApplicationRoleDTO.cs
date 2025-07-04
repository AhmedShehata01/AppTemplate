using System.ComponentModel.DataAnnotations;

namespace AppTemplate.BLL.Models.RoleManagementDTO
{
    public class ApplicationRoleDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsExternal { get; set; }
        public string CreatedOn { get; set; }
    }

    public class CreateRoleDTO
    {
        [Required]
        public string Name { get; set; }
    }

    public class UpdateRoleDTO
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; }

    }

    public class ToggleRoleStatusDTO
    {
        [Required]
        public string RoleId { get; set; }

        public bool IsActive { get; set; }
    }

    public class RoleWithRoutesDTO
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }

        public List<string> AllowedRoutes { get; set; } = new();
    }

    public class DropdownRoleDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class RemoveUserRoleDTO
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
    }

}
