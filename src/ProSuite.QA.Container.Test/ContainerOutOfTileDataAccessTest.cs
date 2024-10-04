using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test
{
	public class ContainerOutOfTileDataAccessTest : ContainerTest
	{
		private IList<IFeatureClassFilter> _filter;
		private IList<QueryFilterHelper> _helper;

		public ContainerOutOfTileDataAccessTest([NotNull] params IReadOnlyTable[] tables) :
			base(tables) { }

		public void SetSearchDistance(double searchDistance)
		{
			SearchDistance = searchDistance;
		}

		public double SearchDistanceIntoNeighbourTiles { get; set; }

		public Func<TileInfo, IList<IReadOnlyRow>, int> TileProcessed;

		#region Overrides of VerifyingContainerTest

		private IEnvelope OutsideTileLeftSearchGeometry { get; set; }
		private IEnvelope OutsideTileRightSearchGeometry { get; set; }

		/// <summary>
		/// Property used on FilterHelper e.g. by filters or transformers to signal full search.
		/// </summary>
		public bool UseFullGeometrySearch { get; set; }

		/// <summary>
		/// Property used in TileAdmin (second-level cache) to signal full search during tile loading.
		/// </summary>
		public bool UseTileEnvelope { get; set; }

		protected override void BeginTileCore(BeginTileParameters parameters)
		{
			// Adapt properties relevant to out-of-tile search:
			CopyFilters(out _filter, out _helper);
			foreach (var helper in _helper)
			{
				helper.FullGeometrySearch = UseFullGeometrySearch;
			}

			if (UseTileEnvelope)
			{
				foreach (IFeatureClassFilter featureClassFilter in _filter)
				{
					var tileFilter = (AoFeatureClassFilter) featureClassFilter;
					tileFilter.TileExtent = parameters.TileEnvelope;
				}
			}

			IEnvelope currentTile = parameters.TileEnvelope;

			double tolerance = 0.01;
			Assert.True(SearchDistance > tolerance, "Invalid search distance");

			IEnvelope tilePlusSearchDistance =
				GeometryUtils.GetExpandedEnvelope(currentTile, SearchDistanceIntoNeighbourTiles);

			OutsideTileLeftSearchGeometry = GeometryFactory.CreateEnvelope(
				tilePlusSearchDistance.XMin, tilePlusSearchDistance.YMin,
				currentTile.XMin - tolerance, tilePlusSearchDistance.YMax,
				currentTile.SpatialReference);

			OutsideTileRightSearchGeometry = GeometryFactory.CreateEnvelope(
				currentTile.XMax + tolerance, tilePlusSearchDistance.YMin,
				tilePlusSearchDistance.XMax, tilePlusSearchDistance.YMax,
				currentTile.SpatialReference);
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			for (int involvedIndex = 0; involvedIndex < InvolvedTables.Count; involvedIndex++)
			{
				IReadOnlyTable involvedTable = InvolvedTables[involvedIndex];

				IReadOnlyFeatureClass featureClass = involvedTable as IReadOnlyFeatureClass;

				if (featureClass == null)
				{
					continue;
				}

				// Search inside the tile:
				IReadOnlyFeature feature = (IReadOnlyFeature) row;

				bool thisRowFound = false;
				foreach (IReadOnlyRow foundRow in Search(involvedIndex, feature.Shape))
				{
					if (IsSameObject(foundRow, row))
					{
						thisRowFound = true;
					}
				}

				Assert.True(thisRowFound, "Tested row not found inside tile!");
			}

			return 0;
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				return 0;
			}

			var foundOutsideTileRows = new List<IReadOnlyRow>();

			for (int involvedIndex = 0; involvedIndex < InvolvedTables.Count; involvedIndex++)
			{
				IReadOnlyTable involvedTable = InvolvedTables[involvedIndex];

				IReadOnlyFeatureClass featureClass = involvedTable as IReadOnlyFeatureClass;

				if (featureClass == null)
				{
					continue;
				}

				foundOutsideTileRows.AddRange(Search(involvedIndex,
				                                     OutsideTileLeftSearchGeometry));
				foundOutsideTileRows.AddRange(Search(involvedIndex,
				                                     OutsideTileRightSearchGeometry));
			}

			TileProcessed?.Invoke(args, foundOutsideTileRows);

			return 0;
		}

		#endregion

		private IEnumerable<IReadOnlyRow> Search(int involvedTableIndex, IGeometry searchGeometry)
		{
			IReadOnlyTable involvedTable = InvolvedTables[involvedTableIndex];

			IFeatureClassFilter filter = _filter[involvedTableIndex];
			filter.FilterGeometry = searchGeometry;

			QueryFilterHelper filterHelper = _helper[involvedTableIndex];

			IEnumerable<IReadOnlyRow> readOnlyRows = Search(involvedTable, filter, filterHelper);
			return readOnlyRows;
		}

		private static bool IsSameObject(IReadOnlyRow row1, IReadOnlyRow row2)
		{
			return row1.OID == row2.OID && row1.Table.Equals(row2.Table);
		}
	}
}
