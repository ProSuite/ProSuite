using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Core.Spatial;

namespace ProSuite.AGP.Editing.PickerUI
{
	public static class PickableItemsReducer
	{
		public class DistanceToGeometryComparer : IComparer<IPickableItem>
		{
			private readonly Geometry _referenceGeometry;

			public DistanceToGeometryComparer(Geometry referenceGeometry)
			{
				_referenceGeometry = referenceGeometry;
			}

			public int Compare(IPickableItem x, IPickableItem y)
			{
				if (x == y)
				{
					return 0;
				}

				if (x == null)
				{
					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				Geometry xGeometry = x.Geometry;
				Geometry yGeometry = y.Geometry;

				if (GeometryUtils.Disjoint(xGeometry, yGeometry))
				{
					return CompareByNormal(x, y, _referenceGeometry);
				}

				// todo daro nearest point works for overlapping geometries as well?
				return CompareByDistance(x, y, _referenceGeometry);
			}

			private static int CompareByNormal(IPickableItem x, IPickableItem y, Geometry referenceGeometry)
			{
				Geometry xGeometry = x.Geometry;
				Geometry yGeometry = y.Geometry;

				if (xGeometry == null)
				{
					return -1;
				}

				if (yGeometry == null)
				{
					return 1;
				}

				if (GetShapeDimension(xGeometry.GeometryType) != 1 && GetShapeDimension(yGeometry.GeometryType) != 1)
				{
					// todo daro what to do?!?
				}

				Polyline polyline = (Polyline) x.Geometry;

				Polyline queryNormal = GeometryEngine.Instance.QueryNormal(polyline, SegmentExtension.NoExtension, 42, AsRatioOrLength.AsLength, 10);

				return 0;
			}

			private static int GetShapeDimension(GeometryType geometryType)
			{
				switch (geometryType)
				{
					case GeometryType.Point:
					case GeometryType.Multipoint:
						return 0;
					case GeometryType.Polyline:
						return 1;
					case GeometryType.Polygon:
					case GeometryType.Multipatch:
					case GeometryType.Envelope:
						return 2;

					default:
						throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType,
						                                      $"Unexpected geometry type: {geometryType}");
				}
			}

			private static int CompareByDistance(IPickableItem x, IPickableItem y, Geometry referenceGeometry)
			{
				double xToSketch = GeometryEngine.Instance.Distance(x.Geometry, referenceGeometry);
				double yToSketch = GeometryEngine.Instance.Distance(y.Geometry, referenceGeometry);

				if (xToSketch < yToSketch)
				{
					return -1;
				}

				if (xToSketch > yToSketch)
				{
					return 1;
				}

				return 0;
			}
		}

		public static IEnumerable<IPickableItem> Reduce(IEnumerable<IPickableItem> candidates,
		                                                Geometry referenceGeometry)
		{
			return candidates
			       .OrderBy(candidate => candidate, new DistanceToGeometryComparer(referenceGeometry))
			       .Select(ordered => ordered);
		}
	}
}
