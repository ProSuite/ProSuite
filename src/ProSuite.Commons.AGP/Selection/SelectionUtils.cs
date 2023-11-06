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
	public static class SelectionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ClearSelection()
		{
			MapView.Active?.Map.ClearSelection();
		}

		public static void SelectFeature(BasicFeatureLayer basicFeatureLayer,
		                                 SelectionCombinationMethod selectionMethod,
		                                 long objectId,
		                                 bool clearExistingSelection = false)
		{
			SelectFeatures(basicFeatureLayer, selectionMethod, new[] { objectId },
			               clearExistingSelection);
		}

		/// <summary>
		/// Selects the requested features from the specified layer and immediately disposes
		/// the selection to avoid selection and immediate de-selection (for selection method XOR)
		/// because it is called in 2 threads.
		/// </summary>
		/// <param name="basicFeatureLayer"></param>
		/// <param name="combinationMethod"></param>
		/// <param name="objectIds"></param>
		/// <param name="clearExistingSelection"></param>
		public static void SelectFeatures([NotNull] BasicFeatureLayer basicFeatureLayer,
		                                  SelectionCombinationMethod combinationMethod,
		                                  [NotNull] IReadOnlyList<long> objectIds,
		                                  bool clearExistingSelection = false)
		{
			if (objectIds.Count == 0)
			{
				return;
			}

			if (clearExistingSelection)
			{
				ClearSelection();
			}

			var queryFilter = new QueryFilter
			                  {
				                  ObjectIDs = objectIds
			                  };

			using (ArcGIS.Core.Data.Selection selection =
			       basicFeatureLayer.Select(queryFilter, combinationMethod))
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.Debug(
						$"Selected OIDs {StringUtils.Concatenate(selection.GetObjectIDs(), ", ")} " +
						$"from {basicFeatureLayer.Name}");
				}
			}
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
					SelectFeatures(layer, SelectionCombinationMethod.Add, objectIds);
				}
			}
		}

		public static void SelectFeatures([NotNull] FeatureSelectionBase featuresPerLayer,
		                                  SelectionCombinationMethod selectionCombinationMethod,
		                                  bool clearExistingSelection = false)
		{
			Assert.ArgumentNotNull(featuresPerLayer, nameof(featuresPerLayer));

			SelectFeatures(featuresPerLayer.BasicFeatureLayer,
			               selectionCombinationMethod,
			               featuresPerLayer.GetOids().ToList(),
			               clearExistingSelection);
		}

		public static void SelectFeatures(
			[NotNull] IEnumerable<FeatureSelectionBase> featuresPerLayers,
			SelectionCombinationMethod selectionCombinationMethod,
			bool clearExistingSelection = false)
		{
			Assert.ArgumentNotNull(featuresPerLayers, nameof(featuresPerLayers));

			if (clearExistingSelection)
			{
				ClearSelection();
			}

			foreach (FeatureSelectionBase featuresPerLayer in featuresPerLayers)
			{
				SelectFeatures(featuresPerLayer.BasicFeatureLayer,
				               selectionCombinationMethod,
				               featuresPerLayer.GetOids().ToList());
			}
		}

		public static IEnumerable<Feature> GetSelectedFeatures([NotNull] MapView activeView)
		{
			SpatialReference sref = activeView.Map.SpatialReference;

			foreach (Feature feature in MapUtils.GetFeatures(GetSelection(activeView.Map), sref))
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
			return GetSelection(map.GetSelection());
		}

		public static Dictionary<MapMember, List<long>> GetSelection(
			SelectionSet selectionSet)
		{
			return selectionSet.ToDictionary();
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
	}
}
