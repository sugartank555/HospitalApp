using Microsoft.AspNetCore.Identity;
using HospitalApp.Models;

namespace HospitalApp.Seed
{
    public static class DefaultSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            // ===== Roles =====
            var roles = new[] { "Admin", "Doctor", "Patient", "Receptionist" };

            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // ===== Admin =====
            var adminEmail = "admin@hospital.local";
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Admin"
                };
                await userMgr.CreateAsync(admin, "Admin!123");
                await userMgr.AddToRoleAsync(admin, "Admin");
            }

            // ===== Receptionists =====
            var receptionists = new[]
            {
                new { Email = "receptionist1@hospital.local", FullName = "Lễ tân 01" },
                new { Email = "receptionist2@hospital.local", FullName = "Lễ tân 02" },
                new { Email = "receptionist3@hospital.local", FullName = "Lễ tân 03" }
            };

            foreach (var rc in receptionists)
            {
                var u = await userMgr.FindByEmailAsync(rc.Email);
                if (u == null)
                {
                    u = new ApplicationUser
                    {
                        UserName = rc.Email,
                        Email = rc.Email,
                        EmailConfirmed = true,
                        FullName = rc.FullName
                    };

                    await userMgr.CreateAsync(u, "Reception!123");
                    await userMgr.AddToRoleAsync(u, "Receptionist");
                }
            }
        }
    }
}
