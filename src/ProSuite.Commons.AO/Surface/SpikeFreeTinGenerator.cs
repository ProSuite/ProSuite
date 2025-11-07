using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

			if(_areasWithSpikes != null)
			{
				_msg.Info("SpikeFree algorithm is only applied to specified areas");
				var areasWithSpikes =
					GdbQueryUtils.GetFeatures(_areasWithSpikes, filter, false)
					             .Select(f => f.Shape)
					             .ToList();

				var pointInSpikeFreeAreas = new List<(double x, double y, double z)>();
				var intersectingAreasWithSpikes = new List<IGeometry>(4);


				foreach (var geometry in geometries)
				{
					intersectingAreasWithSpikes.Clear();
					foreach (var area in areasWithSpikes)
					{
						if (GeometryUtils.Intersects(area, geometry))
							intersectingAreasWithSpikes.Add(area);
					}

					if (intersectingAreasWithSpikes.Count > 0)
					{
						CoordinateTransformer?.Transform(geometry);

						switch (geometry)
						{
							case IPointCollection pointCollection:
							{
								int pc = pointCollection.PointCount;
								for (int i = 0; i < pc; i++)
								{
									IPoint point = pointCollection.get_Point(i);

									bool inside = false;
									foreach (IGeometry t in intersectingAreasWithSpikes)
									{
										if (GeometryUtils.Contains(t, point))
										{
											inside = true;
											break;
										}
									}

									if (inside)
										pointInSpikeFreeAreas.Add((point.X, point.Y, point.Z));
									else
										tin.AddPointZ(point, TagValue);
								}

								break;
							}
							case IPoint point:
							{
								bool inside = false;
								foreach (IGeometry t in intersectingAreasWithSpikes)
								{
									if (GeometryUtils.Contains(t, point))
									{
										inside = true;
										break;
									}
								}

								if (inside)
									pointInSpikeFreeAreas.Add((point.X, point.Y, point.Z));
								else
									tin.AddPointZ(point, TagValue);
								break;
							}
							default:
								_msg.WarnFormat("Unexpected feature type {0}", geometry.GeometryType.ToString());
								break;
						}
					}
					else
					{
						// Nicht Spike Free
						CoordinateTransformer?.Transform(geometry);
						tin.AddShapeZ(geometry, surfaceType, 0, ref useShapeZ);
					}
				}
				AddPointsToTinUsingSpikeFree(tin, pointInSpikeFreeAreas);
			}
			else
			{
				_msg.Info("SpikeFree algorithm is applied to entire area.");
				AddPointsToTinUsingSpikeFree(tin, geometries.SelectMany(ExpandPointCollectionToCoodinates));
			}

		}

		public void AddPointsToTinUsingSpikeFree(ITinEdit tin,
		                                         IEnumerable<(double x, double y, double z)> points)
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
			var adjacentTriangle1 = new TinTriangleClass();
			var adjacentTriangle2 = new TinTriangleClass();
			var adjacentTriangle3 = new TinTriangleClass();

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

				triangle.QueryAdjacentTriangles(adjacentTriangle1, adjacentTriangle2, adjacentTriangle3);
				if (! IsFrozen(adjacentTriangle1) && IsPointSpike(adjacentTriangle1, point))
				{
					Freeze(tin, adjacentTriangle1);
				}

				if (!IsFrozen(adjacentTriangle2) && IsPointSpike(adjacentTriangle2, point))
				{
					Freeze(tin, adjacentTriangle2);
				}

				if (!IsFrozen(adjacentTriangle3) && IsPointSpike(adjacentTriangle3, point))
				{
					Freeze(tin, adjacentTriangle3);
				}

				addedPoints++;
				tin.AddPointZ(point, TagValue);
			}

			_msg.InfoFormat("Added {0} points to the TIN. {1} points where identified as spike and ignored.", addedPoints, ignoredPoints);
		}

		private IEnumerable<(double x, double y, double z)> ExpandPointCollectionToCoodinates(
			IGeometry geometry)
		{
			var pointCollection = geometry as IPointCollection;
			for (int i = 0; i < pointCollection.PointCount; i++)
			{
				IPoint point = pointCollection.get_Point(i);
				yield return (point.X, point.Y, point.Z);
			}
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
			if (triangle.IsEmpty)
			{
				return false;
			}


			double freezeSq = _freezeDistance * _freezeDistance;
			double thresholdZ = point.Z + _insertionBuffer;

			WKSPointZ v0, v1, v2;
			triangle.QueryVertices(out v0, out v1, out v2);

			if (SquaredDist2D(v0, v1) >= freezeSq) return false;
			if (SquaredDist2D(v1, v2) >= freezeSq) return false;
			if (SquaredDist2D(v2, v0) >= freezeSq) return false;

			if (v0.Z < thresholdZ)
			{
				return false;
			}

			if (v1.Z < thresholdZ)
			{
				return false;
			}

			if (v2.Z < thresholdZ)
			{
				return false;
			}

			return true;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double SquaredDist2D(WKSPointZ a, WKSPointZ b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		private static bool IsFrozen(ITinTriangle triangle)
		{
			return ! triangle.IsEmpty && triangle.TagValue == FrozenTag;
		}

		private static void Freeze(ITinEdit tin, ITinTriangle triangle)
		{
			tin.SetTriangleTagValue(triangle.Index, FrozenTag);
			for (int i = 0; i < 3; i++)
			{
				tin.SetEdgeType(triangle.Edge[i].Index, esriTinEdgeType.esriTinHardEdge);
			}
		}
	}
}
