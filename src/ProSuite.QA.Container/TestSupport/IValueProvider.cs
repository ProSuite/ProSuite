using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public interface IValueProvider<T> where T : struct
	{
		T? GetValue([NotNull] IRow row);
	}
}
