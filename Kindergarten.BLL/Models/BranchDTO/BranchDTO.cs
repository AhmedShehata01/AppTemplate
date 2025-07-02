using System.ComponentModel.DataAnnotations;

namespace Kindergarten.BLL.Models.BranchDTO
{
    public class BranchDTO : BaseEntityDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Arabic name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameAr { get; set; }

        [Required(ErrorMessage = "English name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameEn { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10)]
        [MaxLength(100)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string BranchCode { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public int KindergartenId { get; set; }
    }

    public class BranchCreateDTO : BaseEntityDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Arabic name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameAr { get; set; }

        [Required(ErrorMessage = "English name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameEn { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10)]
        [MaxLength(100)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "KindergartenId is required.")]
        public int KindergartenId { get; set; }
    }

    public class BranchUpdateDTO : BaseEntityDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Arabic name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameAr { get; set; }

        [Required(ErrorMessage = "English name is required.")]
        [MinLength(3)]
        [MaxLength(50)]
        public string NameEn { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10)]
        [MaxLength(100)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string BranchCode { get; set; } = string.Empty;

        public int? KindergartenId { get; set; } // optional in update context
    }
}
