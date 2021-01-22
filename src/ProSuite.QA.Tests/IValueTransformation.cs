using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	public interface IValueTransformation
	{
		[CanBeNull]
		object TransformValue([NotNull] IRow row, [CanBeNull] object value);
	}
}
