using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class InvolvesTablesBase : ProcessBase, IInvolvesTables
	{
		protected InvolvesTablesBase([NotNull] IEnumerable<IReadOnlyTable> tables)
			: base(tables) { }

		internal IDataContainer DataContainer { get; set; }

		protected sealed override ISpatialReference GetSpatialReference()
		{
			return TestUtils.GetUniqueSpatialReference(
				this,
				requireEqualVerticalCoordinateSystems: false);
		}

		[NotNull]
		protected IEnumerable<IReadOnlyRow> Search([NotNull] IReadOnlyTable table,
		                                           [NotNull] IQueryFilter queryFilter,
		                                           [NotNull] QueryFilterHelper filterHelper,
		                                           [CanBeNull] IGeometry cacheGeometry = null)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(queryFilter, nameof(queryFilter));
			Assert.ArgumentNotNull(filterHelper, nameof(filterHelper));

			if (DataContainer != null)
			{
				IEnumerable<IReadOnlyRow> rows = DataContainer.Search(table, queryFilter,
					filterHelper, cacheGeometry);

				if (rows != null)
				{
					return rows;
				}
			}

			// this could be controlled by a flag on the filterHelper or a parameter
			// on the Search() method: AllowRecycling
			var cursor = table.EnumRows(queryFilter, recycle: false);

			// TestUtils.AddGarbageCollectionRequest();

			return cursor;
		}
	}
}
