using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models
{
    public class BaseEntityDTO
    {
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }

        public BaseEntityDTO()
        {
            // تعيين القيم الافتراضية عند إنشاء الكيان
            IsDeleted = false;
            CreatedOn = DateTime.UtcNow;
        }
    }
}
