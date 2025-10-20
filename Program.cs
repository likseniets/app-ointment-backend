using Microsoft.EntityFrameworkCore;
using app_ointment_backend.DAL;
using Microsoft.AspNetCore.Identity;


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

// FIKSET: Endret fra User til IdentityUser for å bruke ASP.NET Core Identity
builder.Services.AddDefaultIdentity<IdentityUser>().AddEntityFrameworkStores<UserDbContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

// FIKSET: Lagt til RazorPages og Session for Identity-støtte
builder.Services.AddRazorPages();
builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    DBInit.Seed(app);
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
