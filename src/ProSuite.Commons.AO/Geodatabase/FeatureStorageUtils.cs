using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class FeatureStorageUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool MakeGeometryStorable(
			[NotNull] IGeometry geometry,
			[CanBeNull] NotificationCollection notifications = null)
		{
			if (GeometryUtils.HasUndefinedZValues(geometry))
			{
				((IZ) geometry).CalculateNonSimpleZs();
			}

			GeometryUtils.Simplify(geometry);

			if (geometry.IsEmpty)
			{
				NotificationUtils.Add(notifications,
				                      "The resulting geometry part becomes empty after simplification. It was not be stored.");
				return false;
			}

			return true;
		}

		public static void MakeGeometryStorable([NotNull] IGeometry reshapedGeometry,
		                                        [NotNull] IGeometry originalGeometry,
		                                        [NotNull] IFeature feature,
		                                        bool allowPolylineSplitAndUnsplit = false)
		{
			Assert.ArgumentCondition(originalGeometry != reshapedGeometry,
			                         "Original and reshaped geometry are the same instance.");

			Stopwatch watch = _msg.DebugStartTiming(
				"Simplify/EnsureZs of geometry to store in {0}...",
				GdbObjectUtils.ToString(feature));

			// NOTE: Simplify fails (in some situations) with undefined Z values (e.g. from non-Z-aware targets) - EnsureZsAreNonNan first
			// NOTE: Simplify can result in undefined Z values - EnsureZsAreNonNan again afterwards
			// NOTE: Simplify in the smaller tolerance, otherwise untouched vertices can get changed!

			EnsuresZsAreNonNan(reshapedGeometry, originalGeometry, feature);

			// Simplify is also needed even if all segment orientation is ok
			// because a reshape with more than one CutSubcurve can result in paths 
			// leading to the inside of the ring -> hard to detect.

			const bool allowReorder = false;

			GeometryUtils.Simplify(reshapedGeometry, allowReorder, allowPolylineSplitAndUnsplit);

			EnsuresZsAreNonNan(reshapedGeometry, originalGeometry, feature);

			Assert.False(GeometryUtils.HasUndefinedZValues(reshapedGeometry),
			             "Geometry has undefined Zs.");

			_msg.DebugStopTiming(watch, "Simplified and ensured Z values");
		}

		/// <summary>
		/// Stores the result of a part extraction and returns the list of newly created features,
		/// or null if the process failed.
		/// </summary>
		/// <param name="originalFeature"></param>
		/// <param name="remainingGeometry"></param>
		/// <param name="newGeometries"></param>
		/// <param name="warnings"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IList<IFeature> StoreResult(
			[NotNull] IFeature originalFeature,
			[NotNull] IGeometry remainingGeometry,
			[NotNull] ICollection<IGeometry> newGeometries,
			[CanBeNull] NotificationCollection warnings)
		{
			// Only store the result if all components can be stored:
			var notifications = new NotificationCollection();
			foreach (IGeometry newGeometry in newGeometries)
			{
				if (! MakeGeometryStorable(newGeometry, notifications))
				{
					NotificationUtils.Add(warnings,
					                      "Unable to store new result geometry for feature <oid> {0}: {1}",
					                      originalFeature.OID,
					                      notifications.Concatenate(", "));

					return null;
				}
			}

			if (! MakeGeometryStorable(remainingGeometry, notifications))
			{
				NotificationUtils.Add(warnings,
				                      "Unable to store largest result geometry for feature <oid> {0}: {1}",
				                      originalFeature.OID,
				                      notifications.Concatenate(", "));

				return null;
			}

			// Presumably everything can be stored:
			var newFeatures = new List<IFeature>();
			foreach (IGeometry newGeometry in newGeometries)
			{
				IFeature newFeature =
					GdbObjectUtils.DuplicateFeature(originalFeature, true);

				GdbObjectUtils.SetFeatureShape(newFeature, newGeometry);

				newFeature.Store();

				newFeatures.Add(newFeature);
			}

			GdbObjectUtils.SetFeatureShape(originalFeature, remainingGeometry);

			originalFeature.Store();

			return newFeatures;
		}

		public static bool TryStore(
			[NotNull] IFeature feature,
			[NotNull] IGeometry geometry,
			[CanBeNull] NotificationCollection notifications = null)
		{
			if (! MakeGeometryStorable(geometry, notifications))
			{
				return false;
			}

			// If this fails, fail with exception to abort transaction or let the caller decide to
			// potentially save an inconsistent state:
			GdbObjectUtils.SetFeatureShape(feature, geometry);

			feature.Store();

			return true;
		}

		private static void EnsuresZsAreNonNan([NotNull] IGeometry inGeometry,
		                                       [NotNull] IGeometry originalGeometry,
		                                       [NotNull] IFeature feature)
		{
			bool zSimple = GeometryUtils.TrySimplifyZ(inGeometry);

			if (zSimple)
			{
				return;
			}

			_msg.Debug(
				"Geometry has undefined Zs, even after calculating non-simple Zs.");

			// Remaining NaNs cannot be interpolated -> e.g. because there is only 1 Z value in a part

			// make sure each part has at least some Z values, otherwise CalculateNonSimpleZs fails:
			int geometryCount = ((IGeometryCollection) inGeometry).GeometryCount;

			for (var i = 0; i < geometryCount; i++)
			{
				IGeometry part = ((IGeometryCollection) inGeometry).Geometry[i];

				if (! GeometryUtils.HasUndefinedZValues(part))
				{
					continue;
				}

				_msg.DebugFormat("Geometry part <index> {0} has no Zs. Extrapolating...",
				                 i);

				IGeometry originalGeoInDataSpatialRef;

				// NOTE: same SR needed because ExtrapolateZs / QueryPointAndDistance does not honor different spatial refs
				if (GeometryUtils.EnsureSpatialReference(originalGeometry, feature,
				                                         out originalGeoInDataSpatialRef))
				{
					_msg.DebugFormat(
						"The original shape of {0} was projected from map coordinates back to the feature class' spatial reference.",
						GdbObjectUtils.ToString(feature));
				}

				IGeometry extrapolatedPart = GeometryUtils.ExtrapolateZ(
					part, (IPolycurve) originalGeoInDataSpatialRef);

				GeometryUtils.ReplaceGeometryPart(inGeometry, i, extrapolatedPart);

				if (originalGeoInDataSpatialRef != originalGeometry)
				{
					Marshal.ReleaseComObject(originalGeoInDataSpatialRef);
				}

				_msg.InfoFormat(
					"Z values for {0} were extrapolated from source geometry.",
					GdbObjectUtils.ToString(feature));
			}
		}
	}
}
