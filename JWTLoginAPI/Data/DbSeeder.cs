using Microsoft.AspNetCore.Identity;
using JWTLoginAPI.Entities;

namespace JWTLoginAPI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Ambil service UserManager dan RoleManager dari DI Container
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // 1. Seed Roles (Admin & User) jika belum ada
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            // 2. Seed Default Admin User jika belum ada
            var adminUsername = "adminutama";
            var adminEmail = "admin@authapi.com";

            var adminUser = await userManager.FindByNameAsync(adminUsername);
            if (adminUser == null)
            {
                var newAdmin = new User
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Buat user admin default dengan password super kuat
                var createAdminResult = await userManager.CreateAsync(newAdmin, "Admin#1234");

                if (createAdminResult.Succeeded)
                {
                    // Tempelkan role Admin ke user baru ini
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}