using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models.Auth
{
    public class ChangePasswordFirstTimeDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match new password")]
        public string ConfirmPassword { get; set; }
    }
}
