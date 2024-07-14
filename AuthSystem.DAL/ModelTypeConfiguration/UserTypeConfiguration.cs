
namespace AuthSystem.DAL.ModelsConfigurations;

public class UserTypeConfiguration:IEntityTypeConfiguration<ApplicationUser>
{
	public void Configure(EntityTypeBuilder<ApplicationUser> modelBuilder)
	{
		modelBuilder.Property(U => U.Address).HasMaxLength(80);
		modelBuilder.Property(U => U.Address).IsRequired();

		modelBuilder.HasMany(U => U.RefreshTokens)
			.WithOne()
			.HasForeignKey(T => T.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
