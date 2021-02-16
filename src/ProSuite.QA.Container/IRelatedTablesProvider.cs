using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	public interface IRelatedTablesProvider
	{
		[CanBeNull]
		RelatedTables GetRelatedTables([NotNull] IRow row);
	}
}
