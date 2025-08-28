using Microsoft.AspNetCore.Identity;
using HospitalApp.Models;

namespace HospitalApp.Seed
{
    public static class DefaultSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            var roles = new[] { "Admin", "Doctor", "Patient" };

            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
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
        }
    }
}
