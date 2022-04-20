using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class InvolvesTablesBase : ProcessBase, IInvolvesTables
	{
		protected InvolvesTablesBase([NotNull] IEnumerable<ITable> tables)
			: base(tables) { }

		internal ISearchable DataContainer { get; set; }

		protected sealed override ISpatialReference GetSpatialReference()
		{
			return TestUtils.GetUniqueSpatialReference(
				this,
				requireEqualVerticalCoordinateSystems: false);
		}

		[NotNull]
		protected IEnumerable<IRow> Search([NotNull] ITable table,
		                                   [NotNull] IQueryFilter queryFilter,
		                                   [NotNull] QueryFilterHelper filterHelper,
		                                   [CanBeNull] IGeometry cacheGeometry = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNull(filterHelper, nameof(filterHelper));

			if (DataContainer != null)
			{
				IEnumerable<IRow> rows = DataContainer.Search(table, queryFilter,
				                                              filterHelper, cacheGeometry);

				if (rows != null)
				{
					return rows;
				}
			}

			// this could be controlled by a flag on the filterHelper or a parameter
			// on the Search() method: AllowRecycling
			const bool recycle = false;
			var cursor = new EnumCursor(table, queryFilter, recycle);

			// TestUtils.AddGarbageCollectionRequest();

			return cursor;
		}
	}
}
