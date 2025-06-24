using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Enum;
using Kindergarten.DAL.Extend;

namespace Kindergarten.BLL.Models.UsersManagementDTO
{
    public class GetUsersProfilesDTO : BaseEntityDTO
    {
        public string UserId { get; set; } // PK + FK

        [Required(ErrorMessage = "FullName is required.")]
        [MinLength(3, ErrorMessage = "FullName must be at least 3 characters.")]
        [MaxLength(50, ErrorMessage = "FullName must not exceed 50 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        public string PrimaryPhone { get; set; }

        public string? SecondaryPhone1 { get; set; }
        public string? SecondaryPhone2 { get; set; }

        [Required(ErrorMessage = "BirthDate is required.")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "GraduationYear is Required.")]
        public int GraduationYear { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MinLength(10, ErrorMessage = "Address must be at least 10 characters.")]
        [MaxLength(100, ErrorMessage = "Address must not exceed 100 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Personal photo is required.")]
        public string PersonalPhotoPath { get; set; }

        [Required(ErrorMessage = "National ID front image is required.")]
        public string NationalIdFrontPath { get; set; }
        [Required(ErrorMessage = "National ID back image is required.")]
        public string NationalIdBackPath { get; set; }


        public bool AgreementAccepted { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public string? SubmitterIp { get; set; }

        public UserStatus Status { get; set; } = UserStatus.draft;
        public string? StatusText => Status.ToString();

        public string? ReviewedBy { get; set; } // UserId of the admin
        public DateTime? ReviewedAt { get; set; } // When reviewed
        public string? RejectionReason { get; set; } // Why rejected if rejected
    }
}
