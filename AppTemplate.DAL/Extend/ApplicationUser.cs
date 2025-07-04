using AppTemplate.DAL.Entity;
using Microsoft.AspNetCore.Identity;

namespace AppTemplate.DAL.Extend
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            IsAgree = true;
            IsDeleted = false;
            CreatedOn = DateTime.Now;
        }

        // public UserType UserType { get; set; }

        public bool IsAgree { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsDeleted { get; set; }
        public string? FullName { get; set; }


        // خصائص خاصة بتسجيل الدخول الخارجي
        public string? Provider { get; set; } // "Google" أو "Facebook"
        public string? ProviderUserId { get; set; } // الـ ID الخاص بالمستخدم في المنصة

        public bool IsFirstLogin { get; set; } = false;

        public UserBasicProfile? BasicProfile { get; set; }

    }
}
