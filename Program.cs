using Microsoft.EntityFrameworkCore;
using app_ointment_backend.DAL;
using Microsoft.AspNetCore.Identity;
using app_ointment_backend.Models;
using app_ointment_backend.Services;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("UserDbContextConnection") ?? throw new InvalidOperationException("Connection string 'UserDbContextConnection' not found.");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("UserDbContextConnection"));
});

// FIKSET: Endret fra IdentityUser til ApplicationUser for å bruke custom brukerfelter
builder.Services.AddDefaultIdentity<ApplicationUser>().AddEntityFrameworkStores<UserDbContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserMigrationService>();

// FIKSET: Lagt til RazorPages og Session for Identity-støtte
builder.Services.AddRazorPages();
builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    DBInit.Seed(app);
    
    // Migrer eksisterende brukere til Identity-systemet
    using (var scope = app.Services.CreateScope())
    {
        var migrationService = scope.ServiceProvider.GetRequiredService<UserMigrationService>();
        await migrationService.MigrateExistingUsersAsync();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// FIKSET: UseAuthentication MÅ komme FØR UseAuthorization (riktig rekkefølge)
app.UseAuthentication();
app.UseAuthorization();
// FIKSET: MapRazorPages lagt til for Identity Razor Pages støtte
app.MapRazorPages();

app.MapDefaultControllerRoute();

app.Run();
