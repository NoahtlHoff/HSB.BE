using System.ComponentModel.DataAnnotations;
namespace HSB.BE.Tests
{
	public static class ValidationHelper
	{
		public static List<ValidationResult> ValidateObject(object obj)
		{
			var results = new List<ValidationResult>();
			var context = new ValidationContext(obj, serviceProvider: null, items: null);
			Validator.TryValidateObject(obj, context, results, validateAllProperties: true);
			return results;
		}
	}
}
