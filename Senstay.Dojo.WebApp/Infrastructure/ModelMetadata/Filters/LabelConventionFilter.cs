using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Senstay.Dojo.Infrastructure.ModelMetadata.Filters
{
	public class LabelConventionFilter : IModelMetadataFilter
	{
		public void TransformMetadata(System.Web.Mvc.ModelMetadata metadata,
			IEnumerable<Attribute> attributes)
		{
            if (!string.IsNullOrEmpty(metadata.PropertyName) && string.IsNullOrEmpty(metadata.DisplayName))
            {
                // field name ends with the hint, will display the hint text
                string[] displayNameHints = { "StartDate", "EndDate", "PublishingDate" };
                bool transformed = false;
                foreach (string hint in displayNameHints)
                {
                    if (CanTrimDisplayName(metadata.PropertyName, hint))
                    {
                        metadata.DisplayName = GetStringWithSpaces(hint);
                        transformed = true;
                        break;
                    }
                }

                if (!transformed)
                {
                    if (metadata.PropertyName.Contains("_"))
                    {
                        metadata.DisplayName = metadata.PropertyName.Replace("_", " ");
                        metadata.DisplayName = GetStringWithSpaces(metadata.DisplayName);
                    }
                    else if (CanTrimDisplayName(metadata.PropertyName, "Title"))
                    {
                        metadata.DisplayName = GetStringWithSpaces(metadata.PropertyName.Replace("Title", ""));
                        metadata.DisplayName = GetStringWithSpaces(metadata.DisplayName);
                    }
                    else
                        metadata.DisplayName = GetStringWithSpaces(metadata.PropertyName);
                }
            }
		}

		private string GetStringWithSpaces(string input)
		{
			return Regex.Replace(
			   input,
			   "(?<!^)" +
			   "(" +
			   "  [A-Z][a-z] |" +
			   "  (?<=[a-z])[A-Z] |" +
			   "  (?<![A-Z])[A-Z]$" +
			   ")",
			   " $1",
			   RegexOptions.IgnorePatternWhitespace);
		}

        private bool CanTrimDisplayName(string property, string what)
        {
            return property.EndsWith(what);
        }
	}
}