using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
					return FootprintZCalculationUtils.CalculateHorizontalZsFromSource(
						fromFeature, forGeometry, _mapping, _zOffset);
				case ZCalculationMethod.SourceOffsetParallel:
					return FootprintZCalculationUtils.CalculateZsParallelToSource(
						fromFeature, forGeometry, _mapping,
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

					double? zOffset =
						FootprintZCalculationUtils.TryGetZOffsetFromField(
							forFeature, _mapping, _zOffset);

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

		private IGeometry CalculateZsFromCurrentZ([NotNull] IGeometry forGeometry)
		{
			_zSettingsModel.CurrentMode = ZMode.ConstantZ;

			_msg.DebugFormat(
				"Assigning Z values from current Z ({0})", _zSettingsModel.ConstantZ);

			IGeometry resultGeometry = GeometryUtils.ConstantZ(forGeometry,
			                                                   _zSettingsModel.ConstantZ);

			return resultGeometry;
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
