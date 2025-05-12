using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using BookShopping1.Constants;

namespace BookShopping1.Data
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider serviceProvider)
        {
            // Resolve UserManager and RoleManager
            var userMgr = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleMgr = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Define roles
            var roles = new[] { Roles.Admin.ToString(), Roles.User.ToString() };

            // Seed roles
            foreach (var role in roles)
            {
                if (!await roleMgr.RoleExistsAsync(role))
                {
                    await roleMgr.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user
            var adminEmail = "admin1@gmail.com";
            var adminPassword = "Admin@123";
            var adminUser = await userMgr.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userMgr.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userMgr.AddToRoleAsync(adminUser, Roles.Admin.ToString());
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}
