
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Biometric_Verification_System.Areas.Identity.Data;
using Sample;
using Biometric_Verification_System.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BioVerifyDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<BioVerifyUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
     .AddDefaultUI()
     .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<BioVerifyDbContext>();
    
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSession();
builder.Services.AddSingleton<BitmapFormat>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapHub<FingerprintHub>("/fingerprintHub");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "ADMIN", "Accesor" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<BioVerifyUser>>();

    string email = "admin@gmail.com";
    string password = "Adminn@123";

    if (await userManager.FindByEmailAsync(email) == null)
    {

        var user = new BioVerifyUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Ronewa";
        user.LastName = "Mafhuwa";
        user.Gender = "Male"; 
        user.PhoneNumb = "0764585149";
        user.Date_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "ADMIN");
    }
}


app.Run();
