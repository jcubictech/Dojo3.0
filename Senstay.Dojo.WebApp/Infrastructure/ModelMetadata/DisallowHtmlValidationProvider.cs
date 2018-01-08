using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Senstay.Dojo.Infrastructure.ModelMetadata
{
    public class DisallowHtmlMetadataValidationProvider : DataAnnotationsModelValidatorProvider
    {
        protected override IEnumerable<ModelValidator> GetValidators(System.Web.Mvc.ModelMetadata metadata,
           ControllerContext context, IEnumerable<Attribute> attributes)
        {
            if (attributes == null)
                return base.GetValidators(metadata, context, null);
            if (string.IsNullOrEmpty(metadata.PropertyName))
                return base.GetValidators(metadata, context, attributes);
            //DisallowHtml should not be added if a property allows html input
            var isHtmlInput = attributes.OfType<AllowHtmlAttribute>().Any();
            if (isHtmlInput) return base.GetValidators(metadata, context, attributes);
            attributes = new List<Attribute>(attributes) { new DisallowHtmlAttribute() };
            return base.GetValidators(metadata, context, attributes);
        }
    }
    public class DisallowHtmlAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var tagWithoutClosingRegex = new Regex(@"<[^>]+>");

            var hasTags = tagWithoutClosingRegex.IsMatch(value.ToString());

            if (!hasTags)
                return ValidationResult.Success;

            return new ValidationResult("The field cannot contain html tags");
        }
    }
}