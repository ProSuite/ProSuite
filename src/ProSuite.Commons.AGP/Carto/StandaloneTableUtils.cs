using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class StandaloneTableUtils
	{
		public static bool HasSelection([CanBeNull] StandaloneTable table)
		{
			return table?.SelectionCount > 0;
		}
	}
}
