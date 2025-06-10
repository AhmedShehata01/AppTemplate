using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models.BranchDTO
{
    public class BranchUpdateDTO : BaseEntityDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Arabic name is required.")]
        [MinLength(3, ErrorMessage = "Arabic name must be at least 3 characters.")]
        [MaxLength(50, ErrorMessage = "Arabic name must not exceed 50 characters.")]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "English name is required.")]
        [MinLength(3, ErrorMessage = "English name must be at least 3 characters.")]
        [MaxLength(50, ErrorMessage = "English name must not exceed 50 characters.")]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10, ErrorMessage = "Address must be at least 10 characters.")]
        [MaxLength(100, ErrorMessage = "Address must not exceed 100 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email number is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email address format.")]
        public string Email { get; set; } = string.Empty;
        public int KindergartenId { get; set; }

    }
}
