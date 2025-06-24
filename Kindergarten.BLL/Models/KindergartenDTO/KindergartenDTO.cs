using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.BLL.Models.BranchDTO;

namespace Kindergarten.BLL.Models.KindergartenDTO
{
    public class KindergartenDTO : BaseEntityDTO
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

        public string KGCode { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10)]
        [MaxLength(100)]
        public string Address { get; set; }

        public List<BranchDTO.BranchDTO> Branches { get; set; }
    }

    public class KindergartenCreateDTO : BaseEntityDTO
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

        public List<BranchCreateDTO>? Branches { get; set; }
    }

    public class KindergartenUpdateDTO : BaseEntityDTO
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

        [Required(ErrorMessage = "Kindergarten code is required.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Kindergarten code must be exactly 8 characters.")]
        [RegularExpression(@"^KG\d{6}$", ErrorMessage = "Kindergarten code must follow the format KG000001.")]
        public string KGCode { get; set; } = string.Empty;

        public List<BranchUpdateDTO> Branches { get; set; }
    }
}