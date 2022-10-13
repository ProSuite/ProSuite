using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.CreateFootprint
{
	public class FootprintZCalculator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly FootprintSourceTargetMapping _mapping;
		private readonly IZSettingsModel _zSettingsModel;
		private readonly double _zOffset;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapping">The source-target mapping defining the relevant z-offset field name</param>
		/// <param name="updateZIfNoBuffer">Whether the Z values should be updated even if no geometry is derived - not used, could be removed here</param>
		/// <param name="zSettingsModel">The Z Settings model containing some information required for Z-calculation such as the DTM, CurrentZ</param>
		/// <param name="zOffset">The global downward-offset to be applied for the Z-calculation methods Dtm, SourceOffsetHorizontal, SourceOffsetParallel</param>
		public FootprintZCalculator([NotNull] FootprintSourceTargetMapping mapping,
		                            bool updateZIfNoBuffer, IZSettingsModel zSettingsModel,
		                            double zOffset)
		{
			_mapping = mapping;
			UpdateZIfNoBuffer = updateZIfNoBuffer;
			_zSettingsModel = zSettingsModel;
			_zOffset = zOffset;
		}

		public bool UpdateZIfNoBuffer { get; private set; }

		public IGeometry CalculateZs([NotNull] IGeometry forGeometry, IFeature fromFeature)
		{
			Assert.ArgumentNotNull(forGeometry, nameof(forGeometry));

			switch (_mapping.ZCalculationMethod)
			{
				case ZCalculationMethod.None:
					return GeometryFactory.Clone(forGeometry);
				case ZCalculationMethod.DtmOffset:
					Assert.False(double.IsNaN(_zOffset), "No Z-Offset from DTM specified.");
					return CalculateHorizontalZsFromDtm(forGeometry, _zOffset);
				case ZCalculationMethod.SourceOffsetHorizontal:
					return CalculateHorizontalZsFromSource(fromFeature, forGeometry, _mapping,
					                                       _zOffset);
				case ZCalculationMethod.SourceOffsetParallel:
					return CalculateZsParallelToSource(fromFeature, forGeometry, _mapping,
					                                   _zOffset);
				case ZCalculationMethod.CurrentZ:
					return CalculateZsFromCurrentZ(forGeometry);
				default:
					throw new NotImplementedException("Unexpected Z-calculation method");
			}
		}

		public bool CanUpdateFeatureZs([NotNull] IFeature forFeature,
		                               out string reason)
		{
			reason = null;
			switch (_mapping.ZCalculationMethod)
			{
				case ZCalculationMethod.None:
					return true;
				case ZCalculationMethod.DtmOffset:
					return true;
				case ZCalculationMethod.SourceOffsetHorizontal:
				case ZCalculationMethod.SourceOffsetParallel:

					double? zOffset = TryGetZOffsetFromField(forFeature, _mapping, _zOffset);

					if (zOffset == null)
					{
						reason =
							$"Cannot get valid Z-offset from field {_mapping.ZOffsetFieldName}";
					}

					return zOffset != null;

				case ZCalculationMethod.CurrentZ:
					return true;
				default:
					throw new NotImplementedException("Unexpected Z-calculation method");
			}
		}

		private static IGeometry CalculateZsParallelToSource(
			[NotNull] IFeature sourceFeature,
			[NotNull] IGeometry forGeometry,
			[NotNull] FootprintSourceTargetMapping mapping,
			double globalZOffset)
		{
			_msg.DebugFormat(
				"Assigning Z values parallel to source feature with offset");

			var resultPoints = (IPointCollection) GeometryFactory.Clone(forGeometry);

			double zOffset = TryGetZOffsetFromField(sourceFeature, mapping, globalZOffset) ?? 0;

			// get the lowest points in Z along the multipatch boundary to build a plane:
			var sourceMultipatch = (IMultiPatch) sourceFeature.Shape;

			Plane sourcePlane = FitPlaneThroughBasePoints(sourceMultipatch);

			for (var i = 0; i < resultPoints.PointCount; i++)
			{
				IPoint targetPoint = resultPoints.get_Point(i);

				targetPoint.Z = sourcePlane.Z(targetPoint.X, targetPoint.Y) - zOffset;

				resultPoints.UpdatePoint(i, targetPoint);
			}

			return (IGeometry) resultPoints;
		}

		private IGeometry CalculateZsFromCurrentZ([NotNull] IGeometry forGeometry)
		{
			_zSettingsModel.CurrentMode = ZMode.ConstantZ;

			_msg.DebugFormat(
				"Assigning Z values from current Z ({0})", _zSettingsModel.ConstantZ);

			IGeometry resultGeometry = GeometryUtils.ConstantZ(forGeometry,
			                                                   _zSettingsModel.ConstantZ);

			return resultGeometry;
		}

		/// <summary>
		/// Calculates a plane that goes through the lowest points of the provided boundary points
		/// </summary>
		/// <param name="sourceMultipatch"></param>
		/// <returns></returns>
		private static Plane FitPlaneThroughBasePoints(IMultiPatch sourceMultipatch)
		{
			IPointCollection4 lowestBoundaryPoints =
				GetBoundaryPointsWithLowestZ(sourceMultipatch);

			Plane sourcePlane = GeometryUtils.FitPlane(lowestBoundaryPoints);

			// consider a higher tolerance?
			double tolerance = GeometryUtils.GetXyTolerance(sourceMultipatch);

			int highestOutlierIdx;
			while (! AllPointsInPlane(lowestBoundaryPoints,
			                          sourcePlane, tolerance, out highestOutlierIdx))
			{
				lowestBoundaryPoints.RemovePoints(highestOutlierIdx, 1);
				sourcePlane = GeometryUtils.FitPlane(lowestBoundaryPoints);
			}

			return sourcePlane;
		}

		private static bool AllPointsInPlane(IPointCollection4 points,
		                                     Plane nonVerticalPlane,
		                                     double tolerance, out int highestOutlierIndex)
		{
			highestOutlierIndex = -1;
			double highestOutlierDeltaZ = 0;
			var result = true;
			for (var i = 0; i < points.PointCount; i++)
			{
				IPoint point = points.get_Point(i);

				double dZ = point.Z - nonVerticalPlane.Z(point.X, point.Y);

				if (Math.Abs(dZ) > tolerance)
				{
					result = false;

					if (highestOutlierDeltaZ < dZ)
					{
						highestOutlierDeltaZ = dZ;
						highestOutlierIndex = i;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the boundary points including their z values, if they have a corresponding vertex in the
		/// input multipatch. Where there are several points with the same xy values in the multipatch, the 
		/// lowest point is returned.
		/// </summary>
		/// <param name="multiPatch"></param>
		/// <returns></returns>
		private static IPointCollection4 GetBoundaryPointsWithLowestZ(IMultiPatch multiPatch)
		{
			var sourcePoints = (IPointCollection) multiPatch;

			var footprintPoints = (IPointCollection4) GeometryFactory.CreatePolygon(multiPatch);

			double tolerance = GeometryUtils.GetXyTolerance(multiPatch);

			var resultPoints =
				(IPointCollection4) GeometryFactory.CreateEmptyMultipoint(multiPatch);

			for (var i = 0; i < footprintPoints.PointCount; i++)
			{
				IPoint resultPoint = footprintPoints.get_Point(i);

				IList<int> sourceIndexes = GeometryUtils.FindVertexIndices(sourcePoints,
					resultPoint, tolerance);

				// NOTES: 
				// - Sometimes footprints have vertices on mutipatch segments where there is no vertex
				// - Footprints can have vertices where multipatch segments cross but the multipatch has no vertex (e.g. 2 intersecting rectangular parts)
				if (sourceIndexes.Count == 0)
				{
					_msg.DebugFormat(
						"No point found in source multipatch at {0}. Point will not be used",
						GeometryUtils.ToString(resultPoint));

					continue;
				}

				// if several: use the lowest value: (z values).Min()
				resultPoint.Z = sourceIndexes.Count == 1
					                ? sourcePoints.Point[sourceIndexes[0]].Z
					                : sourceIndexes.Select(
						                               sourceIndex =>
							                               sourcePoints.Point[sourceIndex].Z)
					                               .Min();

				resultPoints.AddPoint(resultPoint);
			}

			return resultPoints;
		}

		private static IGeometry CalculateHorizontalZsFromSource(
			[NotNull] IFeature sourceFeature,
			[NotNull] IGeometry forGeometry,
			[NotNull] FootprintSourceTargetMapping mapping,
			double globalZOffset)
		{
			_msg.DebugFormat(
				"Assigning horizontal Z values from lowest point of source feature with offset");

			double baseHeight = GetLowestZ(sourceFeature.Shape);

			double zOffset = TryGetZOffsetFromField(sourceFeature, mapping, globalZOffset) ?? 0;

			baseHeight -= zOffset;

			IGeometry basePlateZ = GeometryUtils.ConstantZ(forGeometry, baseHeight);

			return basePlateZ;
		}

		private static double? TryGetZOffsetFromField(IFeature sourceFeature,
		                                              FootprintSourceTargetMapping mapping,
		                                              double globalFallbackZOffset)
		{
			double? zOffset;

			if (string.IsNullOrEmpty(mapping.ZOffsetFieldName))
			{
				// use global z-offset only if no z-source field is defined in XML
				_msg.DebugFormat(
					"Using global Z-offset {0} (No ZOffsetFieldName is defined for source-target mapping with source {1})",
					globalFallbackZOffset, mapping.Source);

				zOffset = globalFallbackZOffset;
			}
			else
			{
				zOffset = GetZOffsetFromField(sourceFeature, mapping);

				_msg.DebugFormat(
					"Using Z-offset from field {0} with value {1}", mapping.ZOffsetFieldName,
					zOffset);
			}

			return zOffset;
		}

		private static double? GetZOffsetFromField(IFeature forFeature,
		                                           FootprintSourceTargetMapping mapping)
		{
			int zOffsetFieldIndex = GetZOffsetFieldIdx(forFeature, mapping);

			const bool useCodedDomainName = true;

			double? zOffset = CreateFootprintUtils.GetNumericFieldValue(
				forFeature, zOffsetFieldIndex, useCodedDomainName);

			return zOffset;
		}

		private static int GetZOffsetFieldIdx([NotNull] IFeature feature,
		                                      [NotNull] FootprintSourceTargetMapping mapping)
		{
			if (string.IsNullOrEmpty(mapping.ZOffsetFieldName))
			{
				throw new InvalidConfigurationException(
					string.Format(
						"No Z-offset field for source-target mapping with source {0}",
						mapping.Source));
			}

			int zOffsetFieldIdx = feature.Fields.FindField(mapping.ZOffsetFieldName);

			if (zOffsetFieldIdx < 0)
			{
				throw new InvalidConfigurationException(
					string.Format("Roof Z-offset field {0} not found in feature class {1}",
					              mapping.ZOffsetFieldName, feature.Class.AliasName));
			}

			return zOffsetFieldIdx;
		}

		private IGeometry CalculateHorizontalZsFromDtm([NotNull] IGeometry geometry,
		                                               double zOffest)
		{
			_zSettingsModel.CurrentMode = ZMode.Dtm;

			_msg.DebugFormat("Assigning Z values from DTM with offset {0}", zOffest);

			IGeometry zAppliedGeometry = ApplyDtmZ(geometry, _zSettingsModel);

			if (_zSettingsModel.CurrentMode == ZMode.Dtm)
			{
				double lowestDtmZ = GetLowestZ(zAppliedGeometry);

				zAppliedGeometry = GeometryUtils.ConstantZ(geometry, lowestDtmZ + zOffest);
			}

			return zAppliedGeometry;
		}

		[NotNull]
		private static IGeometry ApplyDtmZ(
			[NotNull] IGeometry geometry, [NotNull] IZSettingsModel zSettingsModel)
		{
			var sketchPoints = (IPointCollection) geometry;

			if (zSettingsModel.CurrentMode == ZMode.Dtm && sketchPoints.PointCount <= 2)
			{
				// NOTE: ApplyZ with dtm does not work for non-simple geometries -> they become empty
				zSettingsModel.CurrentMode = ZMode.ConstantZ;
				_msg.Warn(
					"Calculate Roof Zs: Unable to apply Z values from dtm. Not enough points.");
			}

			IGeometry zAppliedSketch = ApplyZService.ApplyZ(geometry, zSettingsModel);

			return zAppliedSketch;
		}

		private static double GetLowestZ(IGeometry geometry)
		{
			var sketchPoints = geometry as IPointCollection;

			Assert.NotNull(sketchPoints, "Sketch geometry is no point collection");

			return ((IZCollection) geometry).ZMin;
		}
	}
}
