using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public static class SelectionUtils
	{
		public static IEnumerable<Feature> GetSelectedFeatures(MapView activeView)
		{
			Dictionary<MapMember, List<long>> selection = activeView.Map.GetSelection();

			foreach (MapMember mapMember in selection.Keys)
			{
				foreach (var feature in GetSelectedFeatures(mapMember as BasicFeatureLayer))
				{
					yield return feature;
				}
			}
		}

		public static void ClearSelection(Map map)
		{
			Dictionary<MapMember, List<long>> selection = map.GetSelection();

			foreach (MapMember mapMembersWithSelection in selection.Keys)
			{
				var basicLayer = mapMembersWithSelection as BasicFeatureLayer;

				if (basicLayer != null)
				{
					basicLayer.ClearSelection();
				}
			}
		}

		public static IEnumerable<Feature> GetSelectedFeatures([CanBeNull] BasicFeatureLayer layer)
		{
			Selection selection = layer?.GetSelection();

			if (selection == null)
			{
				yield break;
			}

			RowCursor cursor = selection.Search(null, false);

			try
			{
				while (cursor.MoveNext())
				{
					var feature = (Feature) cursor.Current;

					yield return feature;
				}
			}
			finally
			{
				cursor.Dispose();
			}
		}
	}
}
