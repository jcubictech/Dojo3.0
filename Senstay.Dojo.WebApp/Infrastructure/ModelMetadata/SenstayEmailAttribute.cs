using System.ComponentModel.DataAnnotations;

namespace Senstay.Dojo.Infrastructure.ModelMetadata
{
    public class SenstayEmailAttribute : ValidationAttribute
    {
        private readonly string _msEmailTemplate = "@senstay.com";
        public SenstayEmailAttribute() : base("{0} is not a valid SentStay email account.")
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                if (!value.ToString().ToLower().EndsWith(_msEmailTemplate))
                {
                    string errorMessage = FormatErrorMessage(validationContext.DisplayName);
                    return new ValidationResult(errorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}