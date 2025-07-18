﻿using System.Security.Claims;
using AppTemplate.DAL.Extend;
using Microsoft.AspNetCore.Identity;

namespace AppTemplate.DAL.StaticData
{
    public class SeedData
    {
        public static async Task SeedRolesAndAdminUser(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            #region Seed Super Admin User/Role/User Claims

            // Seed Roles
            var roles = new List<string> { "Super Admin", "Admin", "User" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        IsActive = true,
                        CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            var adminEmail = "a.shehata.code@gmail.com";
            var adminUserName = "SupAdmin";  // أو غيره حسب رغبتك
            var adminPassword = "Abc@1234";  // خلي بالك تستخدم باسورد قوي وآمن
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    NormalizedUserName = adminUserName.ToUpper(),
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Super Admin");
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to add role to admin user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                    // إضافة الصلاحيات (Claims)
                    var claims = new List<Claim>
                        {
                            new Claim("View Role", "true"),
                            new Claim("Create Role", "true"),
                            new Claim("Edit Role", "true"),
                            new Claim("Delete Role", "true")
                        };

                    foreach (var claim in claims)
                    {
                        var claimsResult = await userManager.AddClaimAsync(adminUser, claim);
                        if (!claimsResult.Succeeded)
                        {
                            throw new InvalidOperationException($"Failed to add claim '{claim.Type}': {string.Join(", ", claimsResult.Errors.Select(e => e.Description))}");
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            #endregion
        }
    }
}
