using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.CreateFootprint
{
	public static class CreateFootprintUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IList<IFeature> GetFootprintableFeatures(
			[NotNull] IEnumerable<IFeature> forExistingFeatures,
			[NotNull] IEnumerable<IFeatureClass> targetClasses,
			[NotNull] FootprintOptions footprintOptions,
			[NotNull] out IList<string> nonFootprintableReasons)
		{
			IList<IFeature> result = new List<IFeature>();

			nonFootprintableReasons = new List<string>();

			const bool isFeatureKnownInserted = false;

			ICollection<IFeatureClass> targetClassCollection =
				CollectionUtils.GetCollection(targetClasses);

			foreach (IFeature feature in forExistingFeatures)
			{
				FootprintSourceTargetMapping mapping =
					GetMapping(footprintOptions.SourceTargetMappings,
					           feature);

				IFeatureClass targetClass = GetTargetClass(mapping, targetClassCollection, out _);

				double? bufferDistance = GetBufferDistance(mapping, feature);

				bool updateZIfNoBufferDefined = footprintOptions.UpdateZIfNoBuffer;
				bool relateExistingTargets = footprintOptions.RelateExistingTargets;

				string reason;

				if (CanCreateFootprint(feature, isFeatureKnownInserted, mapping, targetClass,
				                       bufferDistance,
				                       updateZIfNoBufferDefined, relateExistingTargets, out reason))
				{
					result.Add(feature);
				}
				else
				{
					nonFootprintableReasons.Add(reason);
				}
			}

			return result;
		}

		public static bool InvalidatesFootprint([NotNull] IFeature changedFeature,
		                                        [NotNull] FootprintOptions options)
		{
			FootprintSourceTargetMapping mapping = GetMapping(options.SourceTargetMappings,
			                                                  changedFeature);

			if (mapping == null)
			{
				return false;
			}

			int bufferDistanceFieldIdx = GetBufferDistanceFieldIdx(changedFeature, mapping);
			int? zOffsetFieldIdx = TryGetZOffsetFieldIdx(changedFeature, mapping);

			int subtypeFieldIdx = DatasetUtils.GetSubtypeFieldIndex(changedFeature.Class);

			var rowChanges = (IRowChanges) changedFeature;
			var featureChanges = (IFeatureChanges) changedFeature;

			bool bufferDistanceChanged = rowChanges.ValueChanged[bufferDistanceFieldIdx];
			bool zOffsetChanged = zOffsetFieldIdx != null &&
			                      rowChanges.ValueChanged[zOffsetFieldIdx.Value];

			bool geometryChanged = featureChanges.ShapeChanged;

			// also subtype: might change the mapping -> becomes a target
			bool subtypeChanged = subtypeFieldIdx >= 0 &&
			                      rowChanges.get_ValueChanged(subtypeFieldIdx);

			return geometryChanged || bufferDistanceChanged || zOffsetChanged || subtypeChanged;
		}

		[CanBeNull]
		public static IFeature TryCreateOrUpdateFootprint(
			[NotNull] IFeature feature,
			[NotNull] IEnumerable<IFeatureClass> targetClasses,
			[NotNull] FootprintOptions options,
			[NotNull] IZSettingsModel zSettingsModel,
			bool isFeatureKnownInserted)
		{
			FootprintSourceTargetMapping mapping = GetMapping(options.SourceTargetMappings,
			                                                  feature);

			int? targetSubtype;
			IFeatureClass targetClass = GetTargetClass(mapping, targetClasses,
			                                           out targetSubtype);

			double? bufferDistance = GetBufferDistance(mapping, feature);

			bool updateZIfNoBuffer = options.UpdateZIfNoBuffer;
			bool tryRelateIntersectingTargets = options.RelateExistingTargets;

			string reason;

			if (! CanCreateFootprint(
				    feature, isFeatureKnownInserted, mapping, targetClass, bufferDistance,
				    updateZIfNoBuffer, tryRelateIntersectingTargets, out reason))
			{
				_msg.InfoFormat("No footprint calculated for feature {0}. {1}",
				                RowFormat.Format(feature), reason);

				return null;
			}

			// buffer to the inside
			bufferDistance *= -1;

			// offset downwards
			double zOffset = -options.ZOffset;

			var zCalculator = new FootprintZCalculator(
				Assert.NotNull(mapping), options.UpdateZIfNoBuffer,
				zSettingsModel, zOffset);

			Assert.NotNull(targetClass, "Target not defined");

			IFeature footprintFeature = TryCreateOrUpdateFootprint(
				targetClass, targetSubtype, feature, bufferDistance, zCalculator,
				tryRelateIntersectingTargets,
				isFeatureKnownInserted);

			return footprintFeature;
		}

		public static void DeleteFootprintFeature(
			[NotNull] IFeature ofFeature,
			[NotNull] IEnumerable<IFeatureClass> targetClasses,
			[NotNull] FootprintOptions footprintOptions)
		{
			Assert.ArgumentNotNull(ofFeature, nameof(ofFeature));
			Assert.ArgumentNotNull(targetClasses, nameof(targetClasses));
			Assert.ArgumentNotNull(footprintOptions, nameof(footprintOptions));

			_msg.DebugFormat("Deleting footprint of {0}", GdbObjectUtils.ToString(ofFeature));

			var featureClass = (IFeatureClass) ofFeature.Class;

			FootprintSourceTargetMapping mapping =
				GetMapping(footprintOptions.SourceTargetMappings,
				           ofFeature);

			if (mapping == null)
			{
				_msg.InfoFormat(
					"No target class defined for {0}, subtype: {1}. No footprint deleted for deleted feature {2}.",
					ofFeature.Class.AliasName, GdbObjectUtils.GetSubtypeCode(ofFeature),
					RowFormat.Format(ofFeature));

				return;
			}

			IFeatureClass targetClass = GetTargetClass(mapping, targetClasses, out _);

			if (targetClass == null)
			{
				_msg.InfoFormat(
					"Target class {0} does not exist in map or is not editable. No footprint deleted for deleted feature {1}",
					mapping.Target.FeatureClassName, RowFormat.Format(ofFeature));

				return;
			}

			IRelationshipClass relationshipClass =
				DatasetUtils.FindUniqueRelationshipClass(featureClass, targetClass);

			if (relationshipClass == null)
			{
				_msg.DebugFormat(
					"No relationship class with target class. No footprint deleted for deleted feature {0}",
					GdbObjectUtils.ToString(ofFeature));

				return;
			}

			IFeature footprintFeature =
				FindSingleRelatedBaseplateFeature(ofFeature, relationshipClass);

			if (footprintFeature != null)
			{
				_msg.InfoFormat("Deleting connected footprint feature {0}",
				                RowFormat.Format(footprintFeature));

				footprintFeature.Delete();
			}
		}

		[CanBeNull]
		public static IPolygon CreateOffsetBufferedFootprint(
			[NotNull] IMultiPatch multiPatch, double bufferDistance,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));
			Assert.False(multiPatch.IsEmpty,
			             "The source geometry is empty. Unable to calculate footprint.");

			IPolygon footprint = GetFootprintAO(multiPatch);

			IPolygon result = Math.Abs(bufferDistance) < double.Epsilon
				                  ? footprint
				                  : GeometryUtils.ConstructOffsetBuffer(footprint, bufferDistance);

			if (result == null || result.IsEmpty)
			{
				NotificationUtils.Add(notifications,
				                      "Unable to calculate a footprint with buffer distance {0}",
				                      bufferDistance);

				_msg.DebugFormat("Footprint is null or empty.");

				return null;
			}

			GeometryUtils.Simplify(result);

			if (result.IsEmpty)
			{
				NotificationUtils.Add(notifications,
				                      "Unable to calculate a valid footprint with buffer distance {0}",
				                      bufferDistance);

				_msg.DebugFormat("Empty footprint result after simplify.");

				return null;
			}

			return result;
		}

		public static IPolygon GetFootprint([NotNull] IMultiPatch multipatch)
		{
			IPolygon result = null;
			if (IntersectionUtils.UseCustomIntersect)
			{
				result = TryGetGeomFootprint(multipatch, out _);
			}

			if (result == null)
			{
				result = GetFootprintAO(multipatch);
			}

			return result;
		}

		private static double GetXyTolerance(IGeometry geometry)
		{
			double xyTolerance = GeometryUtils.GetXyTolerance(geometry);

			// Prevent bogus tolerance (everything smaller the resolution) that
			// increases probability of 'Intersection seen twice error'
			// But take 2 * the resolution to be consistent with AO
			double minimumTolerance = 2 * GeometryUtils.GetXyResolution(geometry);

			return Math.Max(xyTolerance, minimumTolerance);
		}

		public static IPolygon GetFootprintAO([NotNull] IMultiPatch multiPatch)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			// NOTE: IRing.IsInterior is mostly incorrect -> don't use to determine inner rings
			//		 if it must be determined whether it's an interior ring use multiPatch.GetRingType()
			// NOTE: IMultiPatch.get_BeginningRingCount(esriMultiPatchBeginningRingMask) cannot
			//		 be used either because all rings are both OuterRings and RingMasks
			IPolygon footprint = GeometryFactory.CreatePolygon(multiPatch);

			return footprint;
		}

		[CanBeNull]
		public static IPolygon TryGetGeomFootprint([NotNull] IMultiPatch multiPatch,
		                                           [CanBeNull] out IPolyline verticalRings)
		{
			Assert.ArgumentNotNull(multiPatch, nameof(multiPatch));

			double xyTolerance = GetXyTolerance(multiPatch);

			verticalRings = null;
			try
			{
				IPolygon footprintPoly =
					GetFootprintGeom(multiPatch, xyTolerance, out verticalRings);

				return footprintPoly;
			}
			catch (Exception e)
			{
				_msg.Warn(
					$"Error calculating footprint at {GeometryUtils.ToString(multiPatch.Envelope, withoutSpatialReference:true)}. " +
					"Using AO-fallback.");
				_msg.Debug(
					$"Error calculating footprint for {GeometryUtils.ToString(multiPatch)}. " +
					"Using AO-fallback.", e);

				return null;
			}
		}

		private static IPolygon GetFootprintGeom([NotNull] IMultiPatch multiPatch,
		                                         double xyTolerance,
		                                         out IPolyline tooSmallRings)
		{
			tooSmallRings = null;
			Polyhedron polyhedron =
				GeometryConversionUtils.CreatePolyhedron(multiPatch, false, true);

			MultiLinestring footprint = null;
			List<Linestring> verticalRings = null;
			try
			{
				footprint = polyhedron.GetXYFootprint(xyTolerance, out verticalRings);
			}
			catch (Exception)
			{
				//// TODO: This is slow and only works in 50% because it is not enough
				//// (specifically, snap intersection points too but most likely a full cracking
				//// would probably be appropriate.
				//// Same as AO:
				//double enlargedTolerance = 2 * Math.Sqrt(2) * xyTolerance;
				//IList<KeyValuePair<IPnt, List<Pnt3D>>> clusters =
				//	GeomTopoOpUtils.Cluster(polyhedron.GetPoints().ToList(), p => p,
				//	                        enlargedTolerance);

				//var actualClusters = clusters
				//                     .Where(
				//	                     c => c.Value.Any(p => ! p.EqualsXY(c.Key, double.Epsilon)))
				//                     .ToList();

				//foreach (KeyValuePair<IPnt, List<Pnt3D>> cluster in actualClusters)
				//{
				//	foreach (Pnt3D pnt3D in cluster.Value)
				//	{
				//		pnt3D.X = cluster.Key.X;
				//		pnt3D.Y = cluster.Key.Y;
				//	}
				//}

				//// try again:
				//footprint = polyhedron.GetXYFootprint(xyTolerance, out verticalRings);

				throw;
			}

			IPolygon footprintPoly =
				GeometryConversionUtils.CreatePolygon(multiPatch, footprint.GetLinestrings());

			if (verticalRings != null && verticalRings.Count > 0)
			{
				tooSmallRings =
					GeometryConversionUtils.CreatePolyline(verticalRings,
					                                       multiPatch.SpatialReference);
			}

			GeometryUtils.Simplify(footprintPoly);

			return footprintPoly;
		}

		[NotNull]
		public static IFeature CreateNewFeature([NotNull] IFeatureClass targetClass,
		                                        [CanBeNull] Subtype subtype)
		{
			int subtypeCode = subtype?.Code ?? -1;

			IFeature newFeature =
				GdbObjectUtils.CreateFeature(targetClass, subtypeCode);

			return newFeature;
		}

		public static void SetFeatureShape([NotNull] IFeature feature,
		                                   [NotNull] IGeometry geometry)
		{
			IGeometryDef outputGeometryDef = DatasetUtils.GetGeometryDef(feature);

			IGeometry awareGeometry;
			GeometryUtils.EnsureSchemaZM(geometry, outputGeometryDef,
			                             out awareGeometry);

			GdbObjectUtils.SetFeatureShape(feature, awareGeometry);
		}

		public static double? GetNumericFieldValue(IFeature feature, int fieldIndex,
		                                           bool useCodedDomainName)
		{
			Assert.ArgumentCondition(fieldIndex >= 0, "Invalid field index.");

			object valueObj = feature.get_Value(fieldIndex);

			if (Convert.IsDBNull(valueObj))
			{
				return null;
			}

			if (valueObj is double)
			{
				return (double) valueObj;
			}

			int? intValue = GdbObjectUtils.ReadRowValue<int>(feature, fieldIndex);

			IField field = feature.Fields.get_Field(fieldIndex);

			var domain = field.Domain as ICodedValueDomain;

			return domain != null && useCodedDomainName
				       ? GetCodedValue(domain, feature, fieldIndex)
				       : intValue;
		}

		public static void UpdateFootprintFeature(
			[NotNull] IFeature footprintFeature,
			[NotNull] IGeometry footprintGeometry,
			[NotNull] IFeature sourceFeature,
			[CanBeNull] IRelationshipClass relationshipClassForNewRelationship,
			[CanBeNull] NotificationCollection notifications = null)
		{
			SetFeatureShape(footprintFeature, footprintGeometry);

			footprintFeature.Store();

			if (relationshipClassForNewRelationship != null)
			{
				TryAddRelationship(sourceFeature, footprintFeature,
				                   relationshipClassForNewRelationship, notifications);
			}
		}

		public static IRelationship TryAddRelationship(
			[NotNull] IFeature feature1,
			[NotNull] IFeature feature2,
			[CanBeNull] IRelationshipClass relationshipClass,
			[CanBeNull] NotificationCollection notifications = null)
		{
			if (relationshipClass == null)
			{
				return null;
			}

			const bool addMissingPrimaryKey = true;

			// important during explode situations;
			const bool overwriteExistingForeignKeys = true;

			IRelationship newRelationship = RelationshipClassUtils.TryCreateRelationship(
				feature1, feature2, relationshipClass, addMissingPrimaryKey,
				overwriteExistingForeignKeys, notifications);

			if (newRelationship == null)
			{
				_msg.DebugFormat("Cannot create relationship between features: {0}, {1}",
				                 GdbObjectUtils.ToString(feature1),
				                 GdbObjectUtils.ToString(feature2));
			}
			else
			{
				feature1.Store();
				feature2.Store();
			}

			return newRelationship;
		}

		private static bool CanCreateFootprint(
			IFeature forFeature,
			bool isFeatureKnownInserted,
			FootprintSourceTargetMapping sourceTargetMapping,
			IFeatureClass targetFeatureClass,
			double? bufferDistance,
			bool updateZIfNoBuffer,
			bool tryRelateIntersectingTargets,
			out string reason)
		{
			if (sourceTargetMapping == null)
			{
				reason = string.Format(
					"No target class defined for {0}, subtype: {1}.",
					forFeature.Class.AliasName, GdbObjectUtils.GetSubtypeCode(forFeature));

				return false;
			}

			if (targetFeatureClass == null)
			{
				reason = string.Format(
					"Target class or subtype '{0}' does not exist in map or is not editable. No footprint calculated for feature {1}",
					sourceTargetMapping.Target, RowFormat.Format(forFeature));

				return false;
			}

			var needsNewRelationship = false;
			if (! isFeatureKnownInserted && tryRelateIntersectingTargets)
			{
				// for just inserted features we probably don't want to support relating existing targets
				// to eliminate the probability to relate with the wrong target plus the performance is more critical
				// if necessary relating existing targets could be optimised!
				var notifications = new NotificationCollection();
				IFeature targetFeature = FindExistingTargetFeature(
					forFeature, true, targetFeatureClass, notifications, out needsNewRelationship);

				if (targetFeature == null && notifications.Count > 0)
				{
					reason = notifications.Concatenate(" ");
					_msg.DebugFormat(reason);
				}
			}

			if (! updateZIfNoBuffer && bufferDistance == null)
			{
				reason = string.Format(
					"No roof overhang value defined in feature {0}. No footprint was calculated.",
					RowFormat.Format(forFeature));

				// even if no footprint can be calculated, it should still be processed.
				return needsNewRelationship;
			}

			if (updateZIfNoBuffer && bufferDistance == null)
			{
				IRelationshipClass relationshipClass =
					DatasetUtils.FindUniqueRelationshipClass(forFeature.Class, targetFeatureClass);

				if (relationshipClass == null ||
				    FindSingleRelatedBaseplateFeature(forFeature, relationshipClass) == null)
				{
					reason = string.Format(
						"No roof overhang value defined in feature {0} and no related footprint found to update Z values. No footprint was calculated.",
						RowFormat.Format(forFeature));

					return needsNewRelationship;
				}
			}

			reason = null;

			return true;
		}

		[CanBeNull]
		private static IFeature TryCreateOrUpdateFootprint(
			[NotNull] IFeatureClass targetFeatureClass,
			[CanBeNull] int? subtypeCode,
			[NotNull] IFeature sourceFeature,
			double? bufferDistance,
			FootprintZCalculator zCalculator,
			bool tryRelateIntersectingTargets,
			bool isFeatureKnownInserted)
		{
			IRelationshipClass relationshipClass =
				DatasetUtils.FindUniqueRelationshipClass(sourceFeature.Class, targetFeatureClass);

			var createNewRelationship = false;
			IFeature existingFootprint = null;

			// NOTE: if the feature was inserted and it already has a related footprint this means
			// that it is a copy of an existing feature -> do not re-relate the existing to a new
			// roof! Instead create a new footprint in every case.
			// NOTE: currently the rule engine deletes these 
			if (! isFeatureKnownInserted)
			{
				existingFootprint = FindExistingTargetFeature(
					sourceFeature, tryRelateIntersectingTargets,
					targetFeatureClass, null,
					out createNewRelationship);
			}

			if (bufferDistance == null)
			{
				// Geometry cannot be calculated. Try Z-update, relate to existing...
				return UpdateNoBufferFootprint(sourceFeature, existingFootprint, relationshipClass,
				                               zCalculator, createNewRelationship,
				                               zCalculator.UpdateZIfNoBuffer);
			}

			var notifications = new NotificationCollection();
			IGeometry footprintGeometry =
				TryCalculateFootprint(sourceFeature, zCalculator, (double) bufferDistance,
				                      notifications);

			if (footprintGeometry == null)
			{
				// Geometry could not be calculated.
				IRelationship createdRelationship = TryCreateRelationship(
					sourceFeature, existingFootprint,
					createNewRelationship ? relationshipClass : null, notifications);

				if (createdRelationship == null)
				{
					if (isFeatureKnownInserted &&
					    relationshipClass?.Cardinality ==
					    esriRelCardinality.esriRelCardinalityOneToOne)
					{
						// TOP-5425:
						// Remove any existing 1:1 relationship because the update is still related
						relationshipClass.DeleteRelationshipsForObject(sourceFeature);
					}

					// No action can be performed at all. Decide higher up whether to throw or to warn...
					throw new InvalidOperationException(
						$"Cannot create footprint for {RowFormat.Format(sourceFeature)} because: {notifications.Concatenate(" ")}");
				}

				_msg.Info(notifications.Concatenate(" "));
				return existingFootprint;
			}

			IFeature footprintFeature = existingFootprint;

			if (footprintFeature == null)
			{
				footprintFeature =
					GdbObjectUtils.CreateFeature(targetFeatureClass, subtypeCode ?? -1);

				_msg.InfoFormat("Creating new footprint feature {0} for {1}",
				                RowFormat.Format(footprintFeature),
				                RowFormat.Format(sourceFeature));

				createNewRelationship = true;
			}
			else
			{
				_msg.InfoFormat("Updating existing footprint feature {0} for {1}...",
				                RowFormat.Format(footprintFeature),
				                RowFormat.Format(sourceFeature));
			}

			UpdateFootprintFeature(footprintFeature, footprintGeometry, sourceFeature,
			                       createNewRelationship ? relationshipClass : null);

			return footprintFeature;
		}

		[CanBeNull]
		private static IFeature FindExistingTargetFeature(
			[NotNull] IFeature sourceFeature,
			bool tryFindTargetSpatially,
			[NotNull] IFeatureClass targetFeatureClass,
			[CanBeNull] NotificationCollection notifications,
			out bool foundSpatially)
		{
			foundSpatially = false;

			IRelationshipClass relationshipClass =
				DatasetUtils.FindUniqueRelationshipClass(sourceFeature.Class, targetFeatureClass);

			// try to find footprint by relationship
			IFeature existingFootprint = FindSingleRelatedBaseplateFeature(sourceFeature,
				relationshipClass);

			if (existingFootprint == null && relationshipClass != null &&
			    tryFindTargetSpatially)
			{
				if (TrySpatiallyFindExistingFootprint(sourceFeature, targetFeatureClass,
				                                      relationshipClass, notifications,
				                                      out existingFootprint))
				{
					foundSpatially = true;
				}
			}

			return existingFootprint;
		}

		private static IFeature UpdateNoBufferFootprint(
			[NotNull] IFeature sourceFeature,
			[CanBeNull] IFeature existingFootprint,
			[CanBeNull] IRelationshipClass relationshipClass,
			FootprintZCalculator zCalculator,
			bool createNewRelationship, bool updateZ)
		{
			var notifications = new NotificationCollection();
			notifications.Add("No roof overhang value defined in {0}.",
			                  RowFormat.Format(sourceFeature));

			IGeometry updatedFootprintGeometry = null;

			if (existingFootprint != null && updateZ)
			{
				string reason;
				if (! zCalculator.CanUpdateFeatureZs(sourceFeature, out reason))
				{
					notifications.Add("Z values cannot be updated either because: {0}.", reason);
				}
				else
				{
					notifications.Add(
						"Only updating Z values of {0}.", RowFormat.Format(existingFootprint));

					// no buffer distance is set (normally 'Manuell erfasst'): assign Z
					updatedFootprintGeometry =
						zCalculator.CalculateZs(existingFootprint.Shape, sourceFeature);

					SetFeatureShape(existingFootprint, updatedFootprintGeometry);
					existingFootprint.Store();
				}
			}

			IRelationship createdRelationship = TryCreateRelationship(
				sourceFeature, existingFootprint, createNewRelationship ? relationshipClass : null,
				notifications);

			if (updatedFootprintGeometry == null && createdRelationship == null)
			{
				_msg.Warn(notifications.Concatenate(" "));
			}
			else
			{
				_msg.Info(notifications.Concatenate(" "));
			}

			return existingFootprint;
		}

		private static IRelationship TryCreateRelationship(
			[NotNull] IFeature sourceFeature,
			[CanBeNull] IFeature existingFootprint,
			[CanBeNull] IRelationshipClass relationshipClass,
			[CanBeNull] NotificationCollection notifications)
		{
			if (existingFootprint == null)
			{
				return null;
			}

			if (relationshipClass == null)
			{
				return null;
			}

			Assert.NotNull(existingFootprint,
			               "No existing footprint to create relationship with.");

			IRelationship createdRelationship = TryAddRelationship(sourceFeature,
				existingFootprint,
				relationshipClass);

			NotificationUtils.Add(notifications,
			                      createdRelationship != null
				                      ? "Created a new relationship with feature {0}."
				                      : "Could not create a new relationship with existing feature {0}.",
			                      RowFormat.Format(existingFootprint));

			return createdRelationship;
		}

		private static bool TrySpatiallyFindExistingFootprint(
			[NotNull] IFeature sourceFeature,
			[NotNull] IFeatureClass targetFeatureClass,
			IRelationshipClass relationshipClass,
			[CanBeNull] NotificationCollection notifications,
			out IFeature resultFeature)
		{
			resultFeature = null;
			bool result;

			Stopwatch watch = _msg.DebugStartTiming();

			// TODO: filter also for target subtype. So far only one exists in footprints.
			IQueryFilter filter = GdbQueryUtils.CreateSpatialFilter(targetFeatureClass,
				sourceFeature.Shape);

			IList<IFeature> foundFootprints =
				new List<IFeature>(GdbQueryUtils.GetFeatures(targetFeatureClass, filter, false));

			IList<IFeature> usableFootprints =
				Filter(foundFootprints, sourceFeature, relationshipClass, notifications).ToList();

			// NOTE: even if existing footprints are found but cannot be used (not inside, several footprints found)
			//		 a new footprint can be created. It is not generally incorrect if two roofs overlap or even one
			//		 contains another!
			if (usableFootprints.Count == 0)
			{
				result = false;
			}
			else if (usableFootprints.Count == 1)
			{
				IFeature foundFootprint = usableFootprints[0];

				_msg.DebugFormat("Found existing single footprint {0}",
				                 GdbObjectUtils.ToString(foundFootprint));

				result = true;
				resultFeature = foundFootprint;
			}
			else
			{
				NotificationUtils.Add(notifications,
				                      "Several footprints found intersecting source feature {0}. No relationships will be created.",
				                      RowFormat.Format(sourceFeature));

				result = false;
			}

			_msg.DebugStopTiming(watch, "Searched for existing footprints");

			return result;
		}

		private static IEnumerable<IFeature> Filter(
			IEnumerable<IFeature> footprintCandidates,
			IFeature sourceFeature, IRelationshipClass relationshipClass,
			[CanBeNull] NotificationCollection notifications)
		{
			IGeometry sourceShape = sourceFeature.Shape;

			foreach (IFeature candiate in footprintCandidates)
			{
				if (! GeometryUtils.Contains(sourceShape, candiate.Shape))
				{
					NotificationUtils.Add(notifications,
					                      "No relationship is created between {0} and existing footprint {1} because the footprint is not completely inside the source feature.",
					                      RowFormat.Format(sourceFeature),
					                      RowFormat.Format(candiate));
					continue;
				}

				var relatedOtherRoofs =
					new List<IObject>(RelationshipClassUtils.GetRelatedObjects(candiate,
						                  relationshipClass));
				if (relatedOtherRoofs.Count != 0)
				{
					NotificationUtils.Add(notifications,
					                      "{0}: No relationship with existing footprint {1} created because it is already related to another multipatch",
					                      RowFormat.Format(sourceFeature),
					                      RowFormat.Format(candiate));

					continue;
				}

				// create relationship class
				yield return candiate;
			}
		}

		[CanBeNull]
		private static FootprintSourceTargetMapping GetMapping(
			[NotNull] IEnumerable<FootprintSourceTargetMapping> sourceTargetMappings,
			[NotNull] IFeature forFeature)
		{
			Assert.ArgumentNotNull(sourceTargetMappings, nameof(sourceTargetMappings));

			int? subtypeCode = GdbObjectUtils.GetSubtypeCode(forFeature);

			foreach (FootprintSourceTargetMapping mapping in sourceTargetMappings)
			{
				if (mapping.Source.References(forFeature.Class) &&
				    mapping.Source.Subtype == subtypeCode)
				{
					return mapping;
				}
			}

			_msg.DebugFormat("No mapping found for source feature {0}",
			                 GdbObjectUtils.ToString(forFeature));

			return null;
		}

		[CanBeNull]
		private static IFeatureClass GetTargetClass(
			[CanBeNull] FootprintSourceTargetMapping mapping,
			[NotNull] IEnumerable<IFeatureClass> targetClasses,
			out int? subtypeCode)
		{
			subtypeCode = null;

			if (mapping == null)
			{
				return null;
			}

			foreach (IFeatureClass targetClass in targetClasses)
			{
				if (! mapping.Target.References(targetClass))
				{
					continue;
				}

				IList<Subtype> subtypes = DatasetUtils.GetSubtypes(targetClass);

				if (subtypes.Count > 0)
				{
					foreach (Subtype subtype in subtypes)
					{
						if (mapping.Target.Subtype == subtype.Code && IsEditable(targetClass))
						{
							subtypeCode = subtype.Code;
							return targetClass;
						}
					}
				}
				else if (mapping.Target.Subtype == null && IsEditable(targetClass))
				{
					return targetClass;
				}
			}

			return null;
		}

		private static bool IsEditable([NotNull] IFeatureClass featureClass)
		{
			var workspace = (IWorkspaceEdit) DatasetUtils.GetWorkspace(featureClass);

			return workspace.IsBeingEdited();
		}

		private static double? GetBufferDistance(
			[CanBeNull] FootprintSourceTargetMapping mapping,
			[NotNull] IFeature feature)
		{
			if (mapping == null)
			{
				return null;
			}

			int bufferDistanceFieldIdx = GetBufferDistanceFieldIdx(feature, mapping);

			const bool useCodedDomainName = true;

			return GetNumericFieldValue(feature, bufferDistanceFieldIdx,
			                            useCodedDomainName);
		}

		private static int GetBufferDistanceFieldIdx(
			[NotNull] IFeature feature,
			[NotNull] FootprintSourceTargetMapping mapping)
		{
			int bufferDistanceFieldIdx =
				feature.Fields.FindField(mapping.BufferDistanceFieldName);

			if (bufferDistanceFieldIdx < 0)
			{
				throw new InvalidConfigurationException(
					string.Format(
						"Roof overhang distance field {0} not found in feature class {1}",
						mapping.BufferDistanceFieldName, feature.Class.AliasName));
			}

			return bufferDistanceFieldIdx;
		}

		private static int? TryGetZOffsetFieldIdx(
			[NotNull] IFeature feature,
			[NotNull] FootprintSourceTargetMapping mapping)
		{
			if (string.IsNullOrEmpty(mapping.ZOffsetFieldName))
			{
				return null;
			}

			int result = feature.Fields.FindField(mapping.ZOffsetFieldName);

			if (result < 0)
			{
				throw new InvalidConfigurationException(
					string.Format(
						"Roof z-offset field {0} not found in feature class {1}",
						mapping.ZOffsetFieldName, feature.Class.AliasName));
			}

			return result;
		}

		[CanBeNull]
		private static IGeometry TryCalculateFootprint(
			[NotNull] IFeature sourceFeature,
			[NotNull] FootprintZCalculator zCalculator,
			double bufferDistance,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(sourceFeature, nameof(sourceFeature));

			var multiPatch = (IMultiPatch) sourceFeature.Shape;
			Assert.False(multiPatch.IsEmpty,
			             "The source geometry is empty. Unable to calculate footprint.");

			_msg.DebugFormat(
				"Calculating multipatch footprint with buffer distance {0}...",
				bufferDistance);

			IGeometry footprint = CreateOffsetBufferedFootprint(
				multiPatch, bufferDistance, notifications);

			if (footprint == null || footprint.IsEmpty)
			{
				return null;
			}

			IGeometry result = null;
			string reason;
			if (! zCalculator.CanUpdateFeatureZs(sourceFeature, out reason))
			{
				NotificationUtils.Add(notifications, reason);
			}
			else
			{
				result = zCalculator.CalculateZs(footprint, sourceFeature);
			}

			Marshal.ReleaseComObject(footprint);

			return result;
		}

		[CanBeNull]
		private static IFeature FindSingleRelatedBaseplateFeature(
			[NotNull] IFeature roofFeature, [CanBeNull] IRelationshipClass relationshipClass)
		{
			if (relationshipClass == null)
			{
				return null;
			}

			return RelationshipClassUtils.FindSingleRelatedFeature(roofFeature,
				relationshipClass);
		}

		/// <summary>
		/// Gets the value of a given name in a coded value domain.
		/// </summary>
		/// <param name="domain">The coded value domain.</param>
		/// <param name="obj"></param>
		/// <param name="fieldIndex"></param>
		/// <returns></returns>
		[CanBeNull]
		private static double? GetCodedValue([NotNull] ICodedValueDomain domain,
		                                     [NotNull] IObject obj, int fieldIndex)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			int? intValue = GdbObjectUtils.ReadRowValue<int>(obj, fieldIndex);

			string stringValue =
				Convert.ToString(DomainUtils.GetCodedValueName(domain, intValue));

			// stringValue could be ub / k_W etc.
			double result;
			return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture,
			                       out result)
				       ? (double?) result
				       : null;
		}
	}
}
