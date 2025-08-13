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
		[CanBeNull] private readonly IFeatureClass _areasWithSpikes;
		private const int TagValue = 0;
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private const int FrozenTag = 42;


		public SpikeFreeTinGenerator([NotNull] SimpleTerrain simpleTerrain, double freezeDistance, double insertionBuffer, double? tinBufferDistance, [CanBeNull] IFeatureClass areasWithSpikes)
			: base(simpleTerrain, tinBufferDistance)
		{
			_freezeDistance = freezeDistance;
			_insertionBuffer = insertionBuffer;
			_areasWithSpikes = areasWithSpikes;
		}

		protected override void AddFeaturesToTin(ITinEdit tin, esriTinSurfaceType surfaceType,
		                                    IFeatureClass featureClass, IQueryFilter filter,
		                                    IGeometry inExtent)
		{
			// Spike Free only makes sense for points. Lines should be ignored.
			var isPointClass = featureClass.ShapeType == esriGeometryType.esriGeometryPoint ||
			                   featureClass.ShapeType == esriGeometryType.esriGeometryMultipoint;
			if (! isPointClass)
			{
				base.AddFeaturesToTin(tin, surfaceType, featureClass, filter, inExtent);
				return;
			}

			object useShapeZ = true;

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

			if(_areasWithSpikes != null)
			{
				_msg.Info("SpikeFree algorithm is only applied to specified areas");
				var areasWithSpikes =
					GdbQueryUtils.GetFeatures(_areasWithSpikes, filter, false)
					             .Select(f => f.Shape)
					             .ToList();

				var pointInSpikeFreeAreas = new List<(double x, double y, double z)>();


				foreach (var geometry in geometries)
				{
					if (areasWithSpikes.Any(area => GeometryUtils.Intersects(area, geometry)))
					{
						pointInSpikeFreeAreas.AddRange(ExpandToCoodinates(geometry));
					}
					else
					{
						// Not Spike Free
						CoordinateTransformer?.Transform(geometry);
						tin.AddShapeZ(geometry, surfaceType, 0, ref useShapeZ);
					}
				}

				AddPointsToTinUsingSpikeFree(tin, pointInSpikeFreeAreas);
			}
			else
			{
				_msg.Info("SpikeFree algorithm is applied to entire area.");
				AddPointsToTinUsingSpikeFree(tin, geometries.SelectMany(ExpandToCoodinates));
			}

		}

		public void AddPointsToTinUsingSpikeFree(ITinEdit tin, IEnumerable<(double x, double y, double z)> points)
		{
			// Unfortunately some of the methods we required for the spike free tin are in the ITinEdit and some are in the ITinAdvancade interface.
			// However, to avoid having to use the actual class we use two variables both pointing at the same object here.
			var advancedTin = tin as ITinAdvanced;

			Assert.ArgumentNotNull(advancedTin);

			var coordinates = points
			             .OrderByDescending(p => p.z);
			
			int addedPoints = 0;
			int ignoredPoints = 0;

			var point = new PointClass();
			var adjacentTriangles = new ITinTriangle[]
			                        {
										new TinTriangleClass(),
										new TinTriangleClass(),
										new TinTriangleClass(),
			                        };
			foreach ((double x, double y, double z) in coordinates)
			{
				point.PutCoords(x, y);
				point.Z = z;

				ITinTriangle triangle = advancedTin.FindTriangle(point);
				if(IsFrozen(triangle))
				{
					ignoredPoints++;
					continue;
				}

				if (IsPointSpike(triangle, point))
				{
					Freeze(tin, triangle);
					ignoredPoints++;
					continue;
				}

				GetAdjacentTriangles(triangle, adjacentTriangles)
					.Where(t => !IsFrozen(t) && IsPointSpike(t, point))
					.ToList()
					.ForEach(t => Freeze(tin, t));

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

		private bool IsPointSpike(ITinTriangle triangle, IPoint point)
		{
			if(triangle.IsEmpty)
			{
				return false;
			}

			if (GetEdges(triangle).Any(e => e.Length >= _freezeDistance))
			{
				return false;
			}

			// Points where sorted in descending z order before, thus value is always positive.
			var zDistance = GetEdges(triangle).Min(edge => edge.FromNode.Z) - point.Z;
			var result = zDistance >= _insertionBuffer;
			return result;
		}

		private static IEnumerable<ITinEdge> GetEdges(ITinTriangle triangle)
		{
			if(triangle.IsEmpty)
			{
				yield break;
			}

			for (int i = 0; i < 3; i++)
			{
				yield return triangle.Edge[i];
			}
		}

		private static IEnumerable<ITinTriangle> GetAdjacentTriangles(ITinTriangle triangle, ITinTriangle[] triangleArray)
		{
			triangle.QueryAdjacentTriangles(triangleArray[0], triangleArray[1], triangleArray[2]);
			return triangleArray;
		}

		private static bool IsFrozen(ITinTriangle triangle)
		{
			return ! triangle.IsEmpty && triangle.TagValue == FrozenTag;
		}

		private static void Freeze(ITinEdit tin, ITinTriangle triangle)
		{
			tin.SetTriangleTagValue(triangle.Index, FrozenTag);
			foreach (var edge in GetEdges(triangle))
			{
				tin.SetEdgeType(edge.Index, esriTinEdgeType.esriTinHardEdge);
			}
		}
	}
}
