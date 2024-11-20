using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Generator to create a new TIN using the spike free algorithm to exclude certain points to eliminate spikes in the raster.
	/// </summary>
	public class SpikeFreeTinGenerator : FeatureTinGenerator
	{
		private readonly double _freezeDistance;
		private readonly double _insertionBuffer;
		private const int TagValue = 0;
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public SpikeFreeTinGenerator([NotNull] SimpleTerrain simpleTerrain, double freezeDistance, double insertionBuffer, double? tinBufferDistance)
			: base(simpleTerrain, tinBufferDistance)
		{
			_freezeDistance = freezeDistance;
			_insertionBuffer = insertionBuffer;
		}

		protected override void AddFeaturesToTin(ITinEdit tin, esriTinSurfaceType surfaceType,
		                                    IFeatureClass featureClass, IQueryFilter filter,
		                                    IGeometry inExtent)
		{
			var geometries = GdbQueryUtils.GetFeatures(featureClass, filter, recycle: true)
			                              .Select(feature => feature.Shape)
			                              .Where(shape => !shape.IsEmpty);

			bool isClipping =
				(surfaceType == esriTinSurfaceType.esriTinHardClip ||
				 surfaceType == esriTinSurfaceType.esriTinSoftClip) &&
				featureClass.ShapeType == esriGeometryType.esriGeometryPolygon;
			if (isClipping)
			{
				geometries = geometries.Where(shape => !IsClipped(inExtent, shape));
			}

			AddGeometriesToTin(tin, geometries);
		}

		public void AddGeometriesToTin(ITinEdit tin, IEnumerable<IGeometry> geometries)
		{
			// Unfortunately some of the methods we required for the spike free tin are in the ITinEdit and some are in the ITinAdvancade interface.
			// However, to avoid having to use the actual class we use two variables both pointing at the same object here.
			var advancedTin = tin as ITinAdvanced;
			Assert.ArgumentNotNull(advancedTin);

			var coordinates = geometries
			             .SelectMany(ExpandToCoodinates)
			             .OrderByDescending(p => p.z);
			
			int addedPoints = 0;
			int ignoredPoints = 0;

			var point = new PointClass();
			foreach ((double x, double y, double z) in coordinates)
			{
				point.PutCoords(x, y);
				point.Z = z;
				if (IsPointSpike(advancedTin, point))
				{
					ignoredPoints++;
					continue;
				}

				addedPoints++;
				tin.AddPointZ(point, TagValue);
			}

			_msg.InfoFormat("Added {0} points to the TIN. {1} points where identified as spike and ignored.", addedPoints, ignoredPoints);
		}

		// The Spike-Free algorithm yields different results if points are sorted and added to the TIN feature by feature instead of globally.
		// Therefore, it is necessary to globally sort the points before adding them to the TIN.
		// To conserve memory, a list of tuples was chosen as one of the most efficient ways to store the coordinates.
		private IEnumerable<(double x, double y, double z)> ExpandToCoodinates(IGeometry shape)
		{
			CoordinateTransformer?.Transform(shape);
			switch (shape)
			{
				case IPointCollection pointCollection:
				{
					for (int i = 0; i < pointCollection.PointCount; i++)
					{
						IPoint point = pointCollection.get_Point(i);
						yield return (point.X, point.Y, point.Z);
					}

					yield break;
				}
				case IPoint point:
					yield return (point.X, point.Y, point.Z);
					yield break;
				default:
					_msg.WarnFormat("Unexpected feature type {0}", shape.GeometryType.ToString());
					yield break;
			}
		}

		private static bool IsClipped(IGeometry inExtent, IGeometry shape)
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(shape);

			if (GeometryUtils.Disjoint(polyline, inExtent))
			{
				Marshal.ReleaseComObject(polyline);

				return true;
			}

			return false;
		}

		private bool IsPointSpike(ITinAdvanced advancedTin, IPoint point)
		{
			// The unit test transforms here first into a wkspoint. Is that needed?
			ITinTriangle triangle = advancedTin.FindTriangle(point);
			if (triangle is null)
			{
				return false;
			}

			var edgeLength = GetEdges(triangle).Sum(t => t.Length);
			if (edgeLength > _freezeDistance)
			{
				return false;
			}

			// Points where sorted in descending z order before, thus value is always positive.
			var zDistance = GetEdges(triangle).Min(edge => edge.FromNode.Z) - point.Z;
			return zDistance > _insertionBuffer;
		}

		private static IEnumerable<ITinEdge> GetEdges(ITinTriangle triangle)
		{
			for (int i = 0; i < 3; i++)
			{
				yield return triangle.Edge[i];
			}
		}
	}
}
