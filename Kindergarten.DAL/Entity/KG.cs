using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kindergarten.DAL.Entity
{
    [Table("Kindergartens")]
    public class KG : BaseEntity
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

        [Required(ErrorMessage = "Kindergarten code is required.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Kindergarten code must be exactly 8 characters.")]
        [RegularExpression(@"^KG\d{6}$", ErrorMessage = "Kindergarten code must follow the format KG000001.")]
        public string KGCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10, ErrorMessage = "Address must be at least 10 characters.")]
        [MaxLength(100, ErrorMessage = "Address must not exceed 100 characters.")]
        public string Address { get; set; } = string.Empty;
        public ICollection<Branch> Branches { get; set; }

    }
}
