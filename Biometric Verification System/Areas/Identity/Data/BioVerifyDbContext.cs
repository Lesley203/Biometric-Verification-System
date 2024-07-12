using Biometric_Verification_System.Areas.Identity.Data;
using Biometric_Verification_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Biometric_Verification_System.Areas.Identity.Data;

public class BioVerifyDbContext : IdentityDbContext<BioVerifyUser>
{
    public BioVerifyDbContext(DbContextOptions<BioVerifyDbContext> options)
        : base(options)
    {


    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }

    public DbSet<FingerprintDeviceModel> FingerprintData { get; set; }
}
