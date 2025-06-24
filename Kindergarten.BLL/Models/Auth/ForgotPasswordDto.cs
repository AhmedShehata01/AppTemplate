using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models.Auth
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
        public string LoginUrl { get; set; } // رابط صفحة تسجيل الدخول
    }
}
