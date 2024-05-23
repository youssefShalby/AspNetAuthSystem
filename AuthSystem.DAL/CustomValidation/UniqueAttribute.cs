

namespace E_Commerce.DAL.CustomValidation;

public class UniqueAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		return default;
	}
}
