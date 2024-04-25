using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Selection
{
	// Note: the Selection is a property of the map (or its layers) and has nothing to do with the MapView
	// Therefore, pass in a Map and not a MapView
	// Note: SelectionUtils MUST NEVER use MapView.Active

	public static class SelectionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ClearSelection()
		{
			var map = MapView.Active?.Map;
			map?.ClearSelection();
		}

		public static void ClearSelection(Map map)
		{
			map?.ClearSelection();
		}

		public static void SelectFeature(BasicFeatureLayer basicFeatureLayer,
		                                 SelectionCombinationMethod selectionMethod,
		                                 long objectId,
		                                 bool clearExistingSelection = false)
		{
			SelectRows(basicFeatureLayer, selectionMethod, new[] { objectId },
			           clearExistingSelection);
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layers or stand-alone tables.
		/// Selections are only performed on visible selectable layers, preferably on the first
		/// layer or table without definition query.
		/// </summary>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows([NotNull] Map map,
		                              [NotNull] Predicate<IDisplayTable> mapMemberPredicate,
		                              [NotNull] IReadOnlyList<long> objectIds)
		{
			long totalSelected = 0;

			Predicate<BasicFeatureLayer> layerPredicate =
				l => l is IDisplayTable displayTable &&
				     mapMemberPredicate(displayTable);

			foreach (BasicFeatureLayer featureLayer in
			         MapUtils.GetFeatureLayersForSelection(map, layerPredicate))
			{
				totalSelected +=
					SelectRows(featureLayer, SelectionCombinationMethod.Add, objectIds);
			}

			Predicate<StandaloneTable> tablePredicate =
				t => t is IDisplayTable displayTable &&
				     mapMemberPredicate(displayTable);

			foreach (IDisplayTable standaloneTable in
			         MapUtils.GetStandaloneTablesForSelection(map, tablePredicate))
			{
				totalSelected +=
					SelectRows(standaloneTable, SelectionCombinationMethod.Add, objectIds);
			}

			return totalSelected;
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layer or stand-alone table
		/// and immediately disposes the selection to avoid selection and immediate de-selection
		/// (for selection method XOR) because it is called in 2 threads.
		/// </summary>
		/// <param name="tableBasedMapMember"></param>
		/// <param name="combinationMethod"></param>
		/// <param name="objectIds"></param>
		/// <param name="clearExistingSelection"></param>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows([NotNull] IDisplayTable tableBasedMapMember,
		                              SelectionCombinationMethod combinationMethod,
		                              [NotNull] IReadOnlyList<long> objectIds,
		                              bool clearExistingSelection = false)
		{
			if (objectIds.Count == 0)
			{
				return 0;
			}

			if (clearExistingSelection)
			{
				ClearSelection();
			}

			var queryFilter = new QueryFilter
			                  {
				                  ObjectIDs = objectIds
			                  };

			long actualSelectionCount;

			using (ArcGIS.Core.Data.Selection selection =
			       tableBasedMapMember.Select(queryFilter, combinationMethod))
			{
				actualSelectionCount = selection.GetCount();

				LogFeatureSelection(tableBasedMapMember, selection, actualSelectionCount);
			}

			return actualSelectionCount;
		}

		public static void SelectFeatures([NotNull] IEnumerable<Feature> features,
		                                  [NotNull] IList<BasicFeatureLayer> inLayers)
		{
			foreach (IGrouping<IntPtr, Feature> featuresByClassHandle in features.GroupBy(
				         f => f.GetTable().Handle))
			{
				long classHandle = featuresByClassHandle.Key.ToInt64();

				List<long> objectIds = featuresByClassHandle.Select(f => f.GetObjectID()).ToList();

				foreach (var layer in inLayers.Where(
					         fl => fl.GetTable().Handle.ToInt64() == classHandle))
				{
					SelectRows(layer, SelectionCombinationMethod.Add, objectIds);
				}
			}
		}

		public static long SelectFeatures([NotNull] FeatureSelectionBase featuresPerLayer,
		                                  SelectionCombinationMethod selectionCombinationMethod,
		                                  bool clearExistingSelection = false)
		{
			Assert.ArgumentNotNull(featuresPerLayer, nameof(featuresPerLayer));

			return SelectRows(featuresPerLayer.BasicFeatureLayer,
			                  selectionCombinationMethod,
			                  featuresPerLayer.GetOids().ToList(),
			                  clearExistingSelection);
		}

		public static long SelectFeatures(
			[NotNull] IEnumerable<FeatureSelectionBase> featuresPerLayers,
			SelectionCombinationMethod selectionCombinationMethod,
			bool clearExistingSelection = false)
		{
			Assert.ArgumentNotNull(featuresPerLayers, nameof(featuresPerLayers));

			if (clearExistingSelection)
			{
				ClearSelection();
			}

			long result = 0;

			foreach (FeatureSelectionBase featuresPerLayer in featuresPerLayers)
			{
				result += SelectRows(featuresPerLayer.BasicFeatureLayer,
				                     selectionCombinationMethod,
				                     featuresPerLayer.GetOids().ToList());
			}

			return result;
		}

		public static IEnumerable<Feature> GetSelectedFeatures([NotNull] MapView activeView)
		{
			const bool withoutJoins = false;
			SpatialReference sref = activeView.Map.SpatialReference;

			foreach (Feature feature in MapUtils.GetFeatures(
				         GetSelection(activeView.Map), withoutJoins, sref))
			{
				yield return feature;
			}
		}

		public static IEnumerable<Feature> GetSelectedFeatures([CanBeNull] BasicFeatureLayer layer)
		{
			ArcGIS.Core.Data.Selection selection = layer?.GetSelection();

			if (selection == null)
			{
				yield break;
			}

			using (RowCursor cursor = selection.Search(null, false))
			{
				while (cursor.MoveNext())
				{
					yield return (Feature) cursor.Current;
				}
			}
		}

		public static Dictionary<MapMember, List<long>> GetSelection(Map map)
		{
			SelectionSet selectionSet = map.GetSelection();

			return GetSelection(selectionSet);
		}

		public static Dictionary<T, List<long>> GetSelection<T>(Map map) where T : MapMember
		{
			SelectionSet selectionSet = map.GetSelection();

			return GetSelection<T>(selectionSet);
		}

		public static Dictionary<MapMember, List<long>> GetSelection(
			SelectionSet selectionSet)
		{
			return selectionSet.ToDictionary();
		}

		public static Dictionary<T, List<long>> GetSelection<T>(
			SelectionSet selectionSet) where T : MapMember
		{
			return selectionSet.ToDictionary<T>();
		}

		public static Dictionary<MapMember, List<long>> GetSelection(
			MapSelectionChangedEventArgs selectionChangedArgs)
		{
			return GetSelection(selectionChangedArgs.Selection);
		}

		public static int GetFeatureCount(
			[NotNull] IEnumerable<FeatureSelectionBase> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Sum(set => set.GetCount());
		}

		private static void LogFeatureSelection([NotNull] IDisplayTable tableBasedMapMember,
		                                        ArcGIS.Core.Data.Selection selection,
		                                        long selectionCount)
		{
			var mapMember = (MapMember) tableBasedMapMember;

			string mapMemberName = mapMember.Name;

			if (_msg.IsVerboseDebugEnabled)
			{
				var tableDefinition = tableBasedMapMember as ITableDefinitionQueries;

				_msg.Debug(
					$"Selected OIDs: {StringUtils.Concatenate(selection.GetObjectIDs(), ", ")} " +
					$"from {mapMemberName}. Definition query: {tableDefinition?.DefinitionQuery}");
			}

			if (selectionCount == 0)
			{
				return;
			}

			string mapMemberType = mapMember is StandaloneTable ? "table" : "layer";

			string format = selectionCount == 1
				                ? "-> {0:N0} feature selected in {1} '{2}'"
				                : "-> {0:N0} features selected in {1} '{2}'";

			_msg.InfoFormat(format, selectionCount, mapMemberType, mapMemberName);
		}
	}
}
