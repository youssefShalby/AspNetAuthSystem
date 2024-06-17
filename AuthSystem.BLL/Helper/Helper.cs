
namespace AuthSystem.BLL;

public class Helper
{
	public static List<string> GetErrorsOfIdentityResult(IEnumerable<IdentityError> errors)
	{
		List<string> errorResult = new List<string>(5);
		foreach (var error in errors)
		{
			errorResult.Add(error.Description ?? "NA");
		}
		return errorResult;
	}

	public static string GetFirstFiveChardsFromId(string id)
	{
		return id.Substring(0, 5);
	}
}
