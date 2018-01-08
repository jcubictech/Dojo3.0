using System;
using System.Collections.Generic;

namespace Senstay.Dojo.Infrastructure.ModelMetadata
{
	public interface IModelMetadataFilter
	{
		void TransformMetadata(System.Web.Mvc.ModelMetadata metadata,
			IEnumerable<Attribute> attributes);
	}
}