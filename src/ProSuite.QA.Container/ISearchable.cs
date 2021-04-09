using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container
{
	public interface ISearchable
	{
		///// <summary>
		///// returns the extent of the area for which data are currently loaded in the cache of the ISearchable instance
		///// Querying data outside this extend may return incomplete data
		///// </summary>
		//WKSEnvelope CurrentTileExtent { get; }

		IEnumerable<IRow> Search([NotNull] ITable table,
		                         [NotNull] IQueryFilter queryFilter,
		                         [NotNull] QueryFilterHelper filterHelper,
		                         [CanBeNull] IGeometry cacheGeometry = null);

		[CanBeNull]
		UniqueIdProvider GetUniqueIdProvider([NotNull] ITable table);
	}
}
