using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test
{
	public class VerifyingContainerTest : ContainerTest
	{
		public Func<IReadOnlyRow, int, int> OnExecuteCore;
		public Func<TileInfo, int> OnCompleteTile;
		public Func<TileInfo, IReadOnlyRow, int, int> OnCachedRow;

		public VerifyingContainerTest([NotNull] params IReadOnlyTable[] tables) :
			base(tables) { }

		public void SetSearchDistance(double searchDistance)
		{
			SearchDistance = searchDistance;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
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

					IFeatureClassFilter filter =
						new AoFeatureClassFilter(
							search, esriSpatialRelEnum.esriSpatialRelIntersects);

					int tableIndex = 0;
					foreach (IReadOnlyTable table in InvolvedTables)
					{
						var filterHelper = new QueryFilterHelper(table, null, false);
						filterHelper.ForNetwork = true;

						foreach (IReadOnlyRow cachedRow in Search(table, filter, filterHelper))
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
