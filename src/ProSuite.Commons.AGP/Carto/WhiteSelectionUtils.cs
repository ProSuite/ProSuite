using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.Commons.AGP.Carto;

public static class WhiteSelectionUtils
{
	/// <returns>true iff the feature with the given <paramref name="oid"/>
	/// in the given <paramref name="layer"/> has a filled polygon symbol
	/// (there is a <see cref="CIMFill"/> symbol layer)</returns>
	/// <remarks>Must call on MCT</remarks>
	public static bool IsFilledPolygon(MapView mapView, FeatureLayer layer, long oid)
	{
		if (layer.ShapeType != esriGeometryType.esriGeometryPolygon)
		{
			return true; // not a polygon layer
		}

		if (!layer.CanLookupSymbol())
		{
			return false; // assume filled or not polygon
		}

		var symbol = layer.LookupSymbol(oid, mapView);

		if (symbol is not CIMPolygonSymbol)
		{
			return false; // not a polygon
		}

		bool hasFill = symbol is CIMMultiLayerSymbol { SymbolLayers: not null } mls &&
		               mls.SymbolLayers.Any(sl => sl is CIMFill);

		return hasFill;
	}

	public static IEnumerable<FeatureSelectionBase> RemoveUnfilledPolygons(
		this IEnumerable<FeatureSelectionBase> candidates,
		Geometry userInput, double tolerance, MapView mapView)
	{
		foreach (var fsb in candidates)
		{
			var layer = (FeatureLayer) fsb.BasicFeatureLayer;

			if (layer.ShapeType != esriGeometryType.esriGeometryPolygon)
			{
				yield return fsb;
			}
			else if (!layer.CanLookupSymbol())
			{
				yield return fsb;
			}
			else
			{
				var list = new List<Feature>();

				foreach (var feature in fsb.GetFeatures())
				{
					var oid = feature.GetObjectID();

					if (IsFilledPolygon(mapView, layer, oid))
					{
						list.Add(feature);
					}
					else
					{
						var shape = (Polygon) feature.GetShape();
						var boundary = GeometryUtils.Boundary(shape);
						var input =
							GeometryUtils.EnsureSpatialReference(userInput, boundary.SpatialReference);
						var distance = GeometryEngine.Instance.Distance(boundary, input);
						if (distance <= tolerance)
						{
							list.Add(feature);
						}
						// else: selection is away from unfilled polygon's boundary, skip
					}
				}

				if (list.Count > 0)
				{
					// FeatureSelection takes ownership of list:
					yield return new FeatureSelection(layer, list);
				}
			}
		}
	}
}
