using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Extend;
using Kindergarten.DAL.Enum;

namespace Kindergarten.DAL.Entity
{
    public class Otp
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public string CodeHash { get; set; }           // الكود المشفر (مثلاً SHA256)

        public int Attempts { get; set; } = 0;         // عدد المحاولات الفاشلة

        public bool IsUsed { get; set; } = false;      // هل تم استخدام الكود بالفعل
        public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime Expiry { get; set; }           // تاريخ انتهاء صلاحية الكود
    }
}
