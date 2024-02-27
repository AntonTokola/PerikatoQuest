using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Perikato.Data.Carriers;
using Perikato.Data.Dealers;
using System.Diagnostics;
using System.IO;

namespace Perikato.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Routes> Routes { get; set; }
        public DbSet<RouteDates> RouteDates { get; set; }
        public DbSet<DeliveryRequest> DeliveryRequest {get;set;}
        public DbSet<Package> Packages { get; set; }
        public DbSet<PreferredPickUpDates> preferredPickUpDates { get; set; }
        public DbSet<TimeRange> timeRanges { get; set; }
        public DbSet<MatchedDealIds> MatchedDealIds { get; set; }

    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseSqlServer(connectionString);

            return new ApplicationDbContext(builder.Options);
        }
    }
}
