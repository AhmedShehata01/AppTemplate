using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Extend;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.DAL.Database
{
    public class ApplicationContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> ops) : base(ops) { }

        public DbSet<KG> Kindergartens { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<UserBasicProfile> UserBasicProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // إعداد فهرس فريد على كود الفرع
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.BranchCode)
                .IsUnique();

            // ربط علاقة One-to-One بين ApplicationUser و UserBasicProfile
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.BasicProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserBasicProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // لو اتحذف المستخدم، يتم حذف البروفايل كمان


            base.OnModelCreating(modelBuilder);
        }
    }
}
