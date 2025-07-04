using AppTemplate.DAL.Entity;
using AppTemplate.DAL.Entity.DRBRA;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppTemplate.DAL.Database
{
    public class ApplicationContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> ops) : base(ops) { }

        public DbSet<UserBasicProfile> UserBasicProfiles { get; set; }
        public DbSet<SidebarItem> SidebarItem { get; set; }



        #region DRBRA
        public DbSet<SecuredRoute> SecuredRoutes { get; set; }
        public DbSet<RoleSecuredRoute> RoleSecuredRoutes { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        #endregion


        #region OTP
        public DbSet<Otp> Otp { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoleSecuredRoute>()
            .HasIndex(r => new { r.SecuredRouteId, r.RoleId })
            .IsUnique();

            // ربط علاقة One-to-One بين ApplicationUser و UserBasicProfile
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.BasicProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserBasicProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // لو اتحذف المستخدم، يتم حذف البروفايل كمان

            // إضافة Conversion للـ Enum ActionType لو هنسجله كـ String
            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.ActionType)
                .HasConversion<string>();

            // إعداد فهرس للأداء لو حابب:
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(x => new { x.EntityName, x.EntityId });


            base.OnModelCreating(modelBuilder);
        }
    }
}
