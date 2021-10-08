using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test
{
	public class VerifyingContainerTest : ContainerTest
	{
		public Func<IRow, int, int> OnExecuteCore;
		public Func<TileInfo, int> OnCompleteTile;
		public Func<TileInfo, IRow, int, int> OnCachedRow;

		public VerifyingContainerTest([NotNull] params ITable[] tables) :
			base(tables) { }

		public void SetSearchDistance(double searchDistance)
		{
			SearchDistance = searchDistance;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			return OnExecuteCore?.Invoke(row, tableIndex) ?? NoError;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (OnCachedRow != null)
			{
				if (args.CurrentEnvelope != null && args.State != TileState.Initial)
				{
					IEnvelope search = GeometryFactory.Clone(args.CurrentEnvelope);
					search.Expand(SearchDistance, SearchDistance, false);

					ISpatialFilter filter = new SpatialFilterClass();
					filter.Geometry = search;
					filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

					int tableIndex = 0;
					foreach (ITable table in InvolvedTables)
					{
						var filterHelper = new QueryFilterHelper(table, null, false);
						filterHelper.ForNetwork = true;

						foreach (IRow cachedRow in Search(table, filter, filterHelper))
						{
							OnCachedRow(args, cachedRow, tableIndex);
						}

						tableIndex++;
					}
				}
			}

			return OnCompleteTile?.Invoke(args) ?? NoError;
		}
	}
}
