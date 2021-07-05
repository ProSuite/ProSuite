using System;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.AO.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry
{
	public static class ApplyZService
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Public members

		/// <summary>
		/// Applies the Z from model to geometry, returns new geometry
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="zSettingsModel">The z settings model.</param>
		/// <param name="draper">draping done via Drape() from this interface.
		/// If null, default draping is used</param>
		/// <returns>geoemtry with new Z values</returns>
		/// <typeparam name="T"></typeparam>
		[NotNull]
		public static T ApplyZ<T>([NotNull] T geometry,
		                          [NotNull] IZSettingsModel zSettingsModel,
		                          [CanBeNull] IDrapeZ draper = null)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(zSettingsModel, nameof(zSettingsModel));

			T newZGeometry = default(T);

			switch (zSettingsModel.CurrentMode)
			{
				case ZMode.None:
					newZGeometry = GeometryFactory.Clone(geometry);
					break;
				case ZMode.ConstantZ:
					newZGeometry = ConstantZ(geometry, zSettingsModel);
					break;
				case ZMode.Dtm:
					newZGeometry = Dtm(geometry, zSettingsModel, draper);
					break;
				case ZMode.Extrapolate:
					newZGeometry = Extrapolate(geometry, zSettingsModel);
					break;
				case ZMode.Interpolate:
					newZGeometry = Interpolate(geometry);
					break;
				case ZMode.Offset:
					newZGeometry = Offset(geometry, zSettingsModel);
					break;
				case ZMode.Targets:
					newZGeometry = ApplyTargetZs(geometry, zSettingsModel);
					break;
				default:
					_msg.Error(LocalizableStrings.ApplyZServices_UnknownZMode);
					break;
			}

			if (newZGeometry == null || newZGeometry.IsEmpty)
			{
				_msg.Error(LocalizableStrings.ApplyZServices_Unsuccessful);
			}

			// post conditions
			// - geometry.SpatialReference != null
			// - if newZGeometry is not empty -> newZGeometry.SpatialReference = geometry.SpatialReference
			return newZGeometry == null
				       ? GeometryFactory.Clone(geometry) // unchanged copy of input, instead of null
				       : newZGeometry;
		}

		#endregion Public members

		#region Non-public members

		[NotNull]
		private static T ConstantZ<T>([NotNull] T geometry,
		                              [NotNull] IZSettingsModel zSettingsModel)
			where T : IGeometry
		{
			double constantZ = zSettingsModel.ConstantZ;
			return GeometryUtils.ConstantZ(geometry, constantZ);
		}

		[NotNull]
		private static T Dtm<T>([NotNull] T source,
		                        [NotNull] IZSettingsModel zSettingsModel,
		                        [CanBeNull] IDrapeZ draper)
			where T : IGeometry
		{
			ISurface surface = zSettingsModel.DtmSurface;

			Assert.NotNull(surface, LocalizableStrings.ApplyZ_NoSurface);

			// TODO: where to configure the surface? (if the surface implements IVirtualSurface)
			// - resolution depending on ocat
			// - max point count

			bool drape =
				zSettingsModel.CurrentDtmSubMode == DtmSubMode.DtmDrape ||
				zSettingsModel.CurrentDtmSubMode == DtmSubMode.DtmDrapeOffset;

			IGeometry newGeometry;
			if (source is IPolyline || source is IPolygon)
			{
				if (drape)
				{
					newGeometry = GeometryUtils.AssignZ(
						source, surface,
						drapeService: draper,
						drapeTolerance: zSettingsModel.DtmDrapeTolerance);
				}
				else
				{
					newGeometry = GeometryUtils.AssignZ(source, surface);
				}
			}
			else
			{
				if (source is IPoint)
				{
					// ISurface.GetElevation can set the spatialreference (and possible other properties)
					// of the passed geometry to null
					// -> use clone to avoid side effect on source
					newGeometry = GeometryFactory.Clone(source);

					IGeometry clonedNewGeometry = GeometryFactory.Clone(newGeometry);
					double elevation = surface.GetElevation((IPoint) clonedNewGeometry);

					if (newGeometry.SpatialReference == null)
					{
						newGeometry.SpatialReference = source.SpatialReference;
					}

					if (! surface.IsVoidZ(elevation))
					{
						((IPoint) newGeometry).Z = elevation;
					}
				}
				else if (source is IMultipoint)
				{
					newGeometry = Dtm((IPointCollection) source, surface);
				}
				else
				{
					throw new ArgumentException(
						string.Format(LocalizableStrings.ApplyZ_NoSurfaceUsed,
						              source.GeometryType));
				}
			}

			bool offset = (zSettingsModel.CurrentDtmSubMode == DtmSubMode.DtmOffset ||
			               zSettingsModel.CurrentDtmSubMode == DtmSubMode.DtmDrapeOffset) &&
			              Math.Abs(zSettingsModel.DtmOffset) > double.Epsilon;
			if (offset)
			{
				newGeometry = GeometryUtils.OffsetZ(newGeometry,
				                                    zSettingsModel.DtmOffset);
			}

			return (T) newGeometry;
		}

		[NotNull]
		private static T Extrapolate<T>([NotNull] T geometry,
		                                [NotNull] IZSettingsModel zSettingsModel)
			where T : IGeometry
		{
			return GeometryUtils.ExtrapolateZ(geometry,
			                                  (IPolycurve) zSettingsModel.SourceGeometry);
		}

		[NotNull]
		private static T Interpolate<T>([NotNull] T geometry)
			where T : IGeometry
		{
			var polyline = geometry as IPolyline;
			if (polyline == null)
			{
				_msg.Warn("Interpolate only supported for polylines");

				return GeometryFactory.Clone(geometry);
			}

			return (T) GeometryUtils.InterpolateZ(polyline);
		}

		[NotNull]
		private static T Offset<T>([NotNull] T geometry,
		                           [NotNull] IZSettingsModel zSettingsModel)
			where T : IGeometry
		{
			return GeometryUtils.OffsetZ(geometry, zSettingsModel.Offset);
		}

		[NotNull]
		private static T ApplyTargetZs<T>([NotNull] T geometry,
		                                  [NotNull] IZSettingsModel zSettingsModel)
			where T : IGeometry
		{
			return GeometryUtils.ApplyTargetZs(geometry,
			                                   zSettingsModel.TargetGeometries,
			                                   zSettingsModel.CurrentMultiTargetSubMode);
		}

		[NotNull]
		private static IGeometry Dtm([NotNull] IPointCollection source,
		                             [NotNull] ISurface surface)
		{
			var newPoints =
				(IPointCollection) GeometryFactory.Clone((IGeometry) source);
			IPoint vertex = new PointClass();
			int pointCount = newPoints.PointCount;

			for (int i = 0; i < pointCount; i++)
			{
				newPoints.QueryPoint(i, vertex);

				double elevation = surface.GetElevation(vertex);

				if (! surface.IsVoidZ(elevation))
				{
					vertex.Z = elevation;
					newPoints.UpdatePoint(i, vertex);
				}
			}

			return (IGeometry) newPoints;
		}

		#endregion Non-public members
	}
}
