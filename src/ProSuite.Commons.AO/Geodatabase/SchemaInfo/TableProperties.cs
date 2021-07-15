using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class TableProperties : ObjectClassProperties
	{
		public TableProperties([NotNull] IObjectClass objectClass) : base(objectClass) { }
	}
}
