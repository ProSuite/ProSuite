using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Carto
{
	public static class SelectionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ClearSelection()
		{
			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				return;
			}

			Dictionary<MapMember, List<long>> selection = mapView.Map.GetSelection();

			foreach (MapMember mapMembersWithSelection in selection.Keys)
			{
				var basicLayer = mapMembersWithSelection as BasicFeatureLayer;

				if (basicLayer != null)
				{
					basicLayer.ClearSelection();
				}
			}
		}

		public static void SelectFeature(FeatureLayer featureLayer,
		                                 SelectionCombinationMethod selectionMethod, long objectId)
		{
			SelectFeatures(featureLayer, selectionMethod, new[] {objectId});
		}

		/// <summary>
		/// Selects the requested features from the specified layer and immediately disposes
		/// the selection to avoid selection and immediate de-selection (for selection method XOR)
		/// because it is called in 2 threads.
		/// </summary>
		/// <param name="featureLayer"></param>
		/// <param name="combinationMethod"></param>
		/// <param name="objectIds"></param>
		public static void SelectFeatures([NotNull] BasicFeatureLayer featureLayer,
		                                  SelectionCombinationMethod combinationMethod,
		                                  [NotNull] IReadOnlyList<long> objectIds)
		{
			var queryFilter = new QueryFilter
			                  {
				                  ObjectIDs = objectIds
			                  };

			using (Selection selection = featureLayer.Select(queryFilter, combinationMethod))
			{
				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.Debug(
						$"Selected OIDs {StringUtils.Concatenate(selection.GetObjectIDs(), ", ")} " +
						$"from {featureLayer.Name}");
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

		public static IEnumerable<Feature> GetSelectedFeatures([NotNull] MapView activeView)
		{
			Dictionary<MapMember, List<long>> selection = activeView.Map.GetSelection();

			foreach (Feature feature1 in MapUtils.GetFeatures(selection))
			{
				yield return feature1;
			}
		}

		public static IEnumerable<Feature> GetSelectedFeatures([CanBeNull] BasicFeatureLayer layer)
		{
			Selection selection = layer?.GetSelection();

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
			return map.GetSelection();
		}

		public static Dictionary<MapMember, List<long>> GetSelection(
			MapSelectionChangedEventArgs selectionChangedArgs)
		{
			return selectionChangedArgs.Selection;
		}
	}
}
