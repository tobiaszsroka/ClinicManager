using Microsoft.AspNetCore.Identity;

namespace ClinicManager.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Tworzenie domyślnych ról w systemie
            string[] roleNames = { "Admin", "Lekarz", "Rejestratorka" };
            
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Tworzenie głównego konta administratora
            var adminEmail = "admin@clinic.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Tworzenie kont lekarzy do testowania
            var doctorsEmails = new[] { "lekarz@clinic.com", "nowak@clinic.com", "kowalski@clinic.com" };
            foreach (var email in doctorsEmails)
            {
                var doctorUser = await userManager.FindByEmailAsync(email);
                if (doctorUser == null)
                {
                    doctorUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createDoctor = await userManager.CreateAsync(doctorUser, "Lekarz123!");
                    if (createDoctor.Succeeded) await userManager.AddToRoleAsync(doctorUser, "Lekarz");
                }
            }

            // Tworzenie domyślnego konta rejestratorki
            var receptionistEmail = "rejestracja@clinic.com";
            var receptionistUser = await userManager.FindByEmailAsync(receptionistEmail);
            if (receptionistUser == null)
            {
                receptionistUser = new IdentityUser { UserName = receptionistEmail, Email = receptionistEmail, EmailConfirmed = true };
                var createReceptionist = await userManager.CreateAsync(receptionistUser, "Rejestracja123!");
                if (createReceptionist.Succeeded) await userManager.AddToRoleAsync(receptionistUser, "Rejestratorka");
            }
        }
    }
}
