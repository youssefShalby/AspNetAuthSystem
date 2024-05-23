
namespace E_Commerce.DAL.ModelsConfigurations;

public class UserTypeConfiguration:IEntityTypeConfiguration<ApplicationUser>
{
	public void Configure(EntityTypeBuilder<ApplicationUser> modelBuilder)
	{
		modelBuilder.Property(U => U.Address).HasMaxLength(80);
		modelBuilder.Property(U => U.Address).IsRequired();
	}
}
