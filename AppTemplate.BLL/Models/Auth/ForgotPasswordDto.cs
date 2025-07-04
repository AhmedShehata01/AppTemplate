namespace AppTemplate.BLL.Models.Auth
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
        public string LoginUrl { get; set; } // رابط صفحة تسجيل الدخول
    }
}
