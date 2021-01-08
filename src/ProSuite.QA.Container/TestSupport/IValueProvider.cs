using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public interface IValueProvider<T> where T : struct
	{
		T? GetValue([NotNull] IRow row);
	}
}
