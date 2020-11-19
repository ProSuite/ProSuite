using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public static class GeometryReducer
	{
		public static IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> GetReducedset(
			Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer)
		{
			var _sorted = featuresPerLayer.GroupBy(kvp => kvp.Key.ShapeType)
			                              .OrderBy(group => group.Key,
			                                       new GeometryTypeComparer());

			//get the first group of kvp's representing layers with the same shapeType..
			return _sorted.First().Select(el => el);
		}

		public static bool ContainsManyFeatures(
			IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> candidates)
		{
			//several features of different layers
			if (candidates.Count() > 1)
			{
				return true;
			}

			//several features of the same layer
			if (candidates.First().Value.Count() > 1)
			{
				return true;
			}

			;

			return false;
		}
	}
}
