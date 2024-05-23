
namespace E_Commerce.DAL.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
	public AppDbContext(DbContextOptions<AppDbContext> option) : base(option)
	{
		//> pass the options to the base class(IdentityDbContext)
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);

		optionsBuilder.EnableSensitiveDataLogging();
	}


    protected override void OnModelCreating(ModelBuilder modelBuilder)
	{

		base.OnModelCreating(modelBuilder);

		//> call external configurations
		new UserTypeConfiguration().Configure(modelBuilder.Entity<ApplicationUser>());
	}
}

