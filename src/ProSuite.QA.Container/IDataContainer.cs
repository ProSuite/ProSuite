using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container
{
	public interface IDataContainer
	{
		///// <summary>
		///// returns the extent of the area for which data are currently loaded in the cache of the ISearchable instance
		///// Querying data outside this extend may return incomplete data
		///// </summary>
		WKSEnvelope CurrentTileExtent { get; }

		IEnvelope GetLoadedExtent(IReadOnlyTable table);

		double GetSearchTolerance(IReadOnlyTable table);

		IEnumerable<IReadOnlyRow> Search([NotNull] IReadOnlyTable table,
		                                 [NotNull] IQueryFilter queryFilter,
		                                 [NotNull] QueryFilterHelper filterHelper);

		[CanBeNull]
		IUniqueIdProvider GetUniqueIdProvider([NotNull] IReadOnlyTable table);
	}
}
