using System;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	public static class ControlPointTransferUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Public Methods

		/// <summary>
		/// TransferPairwise not yet implemented, only TransferAllPossibles
		/// </summary>
		/// <param name="sourceInside"></param>
		/// <param name="sourceOutside"></param>
		/// <param name="perimeter"></param>
		/// <param name="target"></param>
		/// <param name="pairwise"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static Multipart TransferAfterSplitMerge([NotNull] Multipart sourceInside,
		                                          [NotNull] Multipart sourceOutside,
		                                          [CanBeNull] Polygon perimeter,
		                                          [NotNull] Multipart target, bool pairwise, ref int noTargetFoundCount)
		{

			Assert.ArgumentNotNull(sourceInside, nameof(sourceInside));
			Assert.ArgumentNotNull(sourceOutside, nameof(sourceOutside));
			Assert.ArgumentNotNull(target, nameof(target));

			ControlPointUtils.ResetControlPoints(target);

			noTargetFoundCount = 0;
			Multipart newTarget = target;

			if (pairwise)
			{
				throw new NotImplementedException("TransferPairwise not yet implemented");
			}
			else
			{
				newTarget = TransferAllPossibles(sourceInside, newTarget, perimeter, true, ref noTargetFoundCount);
				newTarget= TransferAllPossibles(sourceOutside, newTarget, perimeter, false, ref noTargetFoundCount);

			}
			return newTarget;
		}

		#endregion Public Methods

		#region Private Methods

		private static Multipart TransferAllPossibles(Multipart source, Multipart target, Polygon perimeter, bool inside, ref int noTargetFoundCount)
		{
			//GeometryUtils.MakePointIDAware(target);
			MultipartBuilderEx targetGeometryBuilder = null;

			Multipart newGeometry = target;

			//GeometryBuilderEx targetGeometryBuilder = null;
			switch (target.GeometryType)	
			{
				case GeometryType.Unknown:
					break;
				case GeometryType.Point:
					break;
				case GeometryType.Envelope:
					break;
				case GeometryType.Multipoint:
					//targetGeometryBuilder = new MultipointBuilderEx((Multipoint) target);
					break;
				case GeometryType.Polyline:
					targetGeometryBuilder = new PolylineBuilderEx((Polyline) target);
					break;
				case GeometryType.Polygon:
					targetGeometryBuilder = new PolygonBuilderEx((Polygon)target);
					break;
				case GeometryType.Multipatch:
					break;
				case GeometryType.GeometryBag:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (targetGeometryBuilder != null)
			{
				double searchRadius = Math.Max(GetSearchRadiusVoodoo(source),
				                               GetSearchRadiusVoodoo(target));

				source = GeometryUtils.Accelerate(source);
				target = GeometryUtils.Accelerate(target);
				targetGeometryBuilder.HasID = true;
				foreach (MapPoint sourcePoint in source.Points)
				{
					if (sourcePoint.HasID && sourcePoint.ID != 0)
					{
						if (perimeter != null &&
						    GeometryUtils.Contains(perimeter, sourcePoint) != inside)
						{
							continue;
						}

						ProximityResult proximity = GeometryEngine.Instance.NearestVertex(target, sourcePoint);
						if (proximity.Distance <= searchRadius)
						{
							int? pointIndex = proximity.PointIndex;
							if (pointIndex != null)
							{
								int partIndex = proximity.PartIndex;
								int idx = pointIndex.Value;
								MapPoint mapPoint = targetGeometryBuilder.GetPoint(partIndex, idx);
								//MapPointBuilderEx builder = new MapPointBuilderEx(proximity.Point);
								MapPointBuilderEx builder = new MapPointBuilderEx(mapPoint);
								builder.HasID = true;
								builder.ID = sourcePoint.ID;
								builder.HasID = sourcePoint.ID != 0;
								targetGeometryBuilder.SetPoint(partIndex, idx, builder.ToGeometry());
							}
						}
						else
						{
							noTargetFoundCount++;
						}
					}
				}

				newGeometry = (Multipart)targetGeometryBuilder.ToGeometry();
			}

			return newGeometry;
		}

		private static double GetSearchRadiusVoodoo(Geometry curve)
		{
			double result = 0.0;

			if (curve?.SpatialReference != null)
			{
				result = GeometryUtils.GetXyTolerance(curve);
				if (double.IsNaN(result))
				{
					result = 0.0;
				}
			}

			return result > 0
				       ? result
				       : 0.0125;
		}

		#endregion Private Methods

		#region Nested class: ControlPointPair

		private class ControlPointPair
		{
			public ControlPointPair(MapPoint firstPoint, MapPoint lastPoint)
			{
				FirstPoint = firstPoint;
				LastPoint = lastPoint;
			}

			public MapPoint FirstPoint { get; }

			public MapPoint LastPoint { get; }
		}

		#endregion Nested class: ControlPointPair
	}
}
