using System.ComponentModel.DataAnnotations;

namespace Kindergarten.BLL.Models.UserManagementDTO
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAgree { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class CreateApplicationUserDTO
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class UpdateApplicationUserDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAgree { get; set; }
    }

    public class CreateUserByAdminDTO
    {
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public List<string> Roles { get; set; } = new();

        [Required]
        public string RedirectUrlAfterResetPassword { get; set; }
    }
}
