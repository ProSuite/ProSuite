﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.RemoveOverlaps
{
	[CLSCompliant(false)]
	public static class RemoveOverlapsUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Calculation of selectable overlaps

		[NotNull]
		public static Overlaps GetSelectableOverlaps(
			[NotNull] IEnumerable<IFeature> sourceFeatures,
			[NotNull] IList<IFeature> overlappingFeatures,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			var result = new Overlaps(new List<IGeometry>());

			Stopwatch watch = Stopwatch.StartNew();

			var sourceCount = 0;

			foreach (IFeature sourceFeature in sourceFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					_msg.Debug("Overlaps calculation was cancelled.");
					return result;
				}

				result.AddGeometries(GetSelectableOverlaps(
					                     sourceFeature, overlappingFeatures,
					                     result.Notifications, trackCancel));

				_msg.DebugFormat(
					"Calculated overlaps for source feature {0}. Current overlaps count: {1}",
					GdbObjectUtils.ToString(sourceFeature), result.OverlapCount);

				sourceCount++;
			}

			_msg.DebugStopTiming(watch,
			                     "Calculated {0} overlaps between {1} source and {2} target features.",
			                     result.OverlapCount, sourceCount,
			                     overlappingFeatures.Count);

			return result;
		}

		public static IEnumerable<IGeometry> GetSelectableOverlaps(
			[NotNull] IFeature sourceFeature,
			[NotNull] IEnumerable<IFeature> overlappingFeatures,
			[CanBeNull] NotificationCollection notifications = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			IGeometry sourceGeometry = sourceFeature.Shape;
			var topOpSource = (ITopologicalOperator) sourceGeometry;

			foreach (IFeature feature in overlappingFeatures)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					yield break;
				}

				_msg.VerboseDebugFormat("Calculating overlap from {0}",
				                        GdbObjectUtils.ToString(feature));

				IGeometry targetGeometry = feature.Shape;

				if (GeometryUtils.Disjoint(targetGeometry, sourceGeometry))
				{
					continue;
				}

				if (GeometryUtils.Contains(targetGeometry, sourceGeometry))
				{
					// Idea for the future: Optionally allow also deleting features (probably using a black display feedback)
					NotificationUtils.Add(notifications,
					                      "Source feature {0} is completely within target {1} and would become empty if the overlap was removed. The overlap is supressed.",
					                      RowFormat.Format(sourceFeature),
					                      RowFormat.Format(feature));
					continue;
				}

				_msg.VerboseDebug("Intersecting...");

				if (sourceGeometry.GeometryType ==
				    esriGeometryType.esriGeometryMultiPatch)
				{
					sourceGeometry = GeometryFactory.CreatePolygon(sourceGeometry);
				}

				esriGeometryDimension dimension = sourceGeometry is IPolygon
					                                  ? esriGeometryDimension
						                                  .esriGeometry2Dimension
					                                  : esriGeometryDimension
						                                  .esriGeometry1Dimension;

				if (targetGeometry.GeometryType ==
				    esriGeometryType.esriGeometryMultiPatch)
				{
					targetGeometry = GeometryFactory.CreatePolygon(targetGeometry);
				}

				IGeometry intersection = topOpSource.Intersect(targetGeometry, dimension);

				Marshal.ReleaseComObject(targetGeometry);

				_msg.VerboseDebug("Simplifying...");

				GeometryUtils.Simplify(intersection);

				if (! intersection.IsEmpty)
				{
					yield return intersection;
				}
			}
		}

		#endregion

		#region Select overlaps

		public static void SelectOverlapsToRemove(
			[CanBeNull] IList<IGeometry> selectableOverlaps,
			[CanBeNull] IGeometry selectionArea,
			bool singlePick,
			ITrackCancel trackCancel,
			out IPolycurve removePolyline,
			out IPolycurve removePolygon)
		{
			removePolygon = null;
			removePolyline = SelectOverlapsToRemove(
				selectableOverlaps, selectionArea, singlePick,
				esriGeometryType.esriGeometryPolyline, trackCancel);

			// in case of single pick the line has priority
			if (removePolyline == null || ! singlePick)
			{
				removePolygon =
					SelectOverlapsToRemove(selectableOverlaps, selectionArea, singlePick,
					                       esriGeometryType.esriGeometryPolygon,
					                       trackCancel);
			}
		}

		[CanBeNull]
		private static IPolycurve SelectOverlapsToRemove(
			[CanBeNull] ICollection<IGeometry> overlaps,
			[CanBeNull] IGeometry selectionArea,
			bool singlePick,
			esriGeometryType resultGeometryType,
			[CanBeNull] ITrackCancel trackCancel)
		{
			IPolycurve result = null;

			if (overlaps == null)
			{
				return null;
			}

			var allSelectedComponents = new List<IGeometry>(overlaps.Count);

			foreach (IGeometry overlap in overlaps)

			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return null;
				}

				if (overlap.GeometryType != resultGeometryType ||
				    (selectionArea != null &&
				     GeometryUtils.Disjoint(selectionArea, overlap)))
				{
					continue;
				}

				var selectedParts =
					(IGeometryCollection) GetSelectedGeometryParts(
						selectionArea, ! singlePick,
						(IGeometryCollection) overlap);

				if (selectedParts != null)
				{
					allSelectedComponents.Add((IGeometry) selectedParts);
				}
			}

			if (allSelectedComponents.Count > 1)
			{
				if (singlePick)
				{
					IGeometry smallest =
						GeometryUtils.GetSmallestGeometry(allSelectedComponents);

					if (smallest != null)
					{
						result = (IPolycurve) GeometryUtils
							.GetHighLevelGeometry(smallest);
					}
					else
					{
						throw new AssertionException(
							"Unable to determine smallest geometry");
					}
				}
				else
				{
					result = (IPolycurve) GeometryUtils.Union(allSelectedComponents);
				}
			}
			else if (allSelectedComponents.Count == 1)
			{
				result = (IPolycurve) allSelectedComponents[0];
			}

			return result;
		}

		[CanBeNull]
		private static IGeometry GetSelectedGeometryParts(
			[CanBeNull] IGeometry selectionArea,
			bool selectionAreaMustContain,
			[NotNull] IGeometryCollection fromGeometryCollection)
		{
			IGeometry result = null;

			foreach (
				IGeometry highLevelPart in GetSelectableHighLevelParts(
					fromGeometryCollection))
			{
				if (selectionArea == null ||
				    (! selectionAreaMustContain &&
				     GeometryUtils.Intersects(selectionArea, highLevelPart)) ||
				    GeometryUtils.Contains(selectionArea, highLevelPart))
				{
					if (result == null)
					{
						result = GeometryFactory.Clone(highLevelPart);
					}
					else
					{
						((IGeometryCollection) result).AddGeometryCollection(
							(IGeometryCollection) highLevelPart);
					}
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetSelectableHighLevelParts(
			[NotNull] IGeometryCollection fromGeometryCollection)
		{
			var fromPolygon = fromGeometryCollection as IPolygon;

			if (fromPolygon != null)
			{
				foreach (
					IPolygon part in
					GeometryUtils.GetConnectedComponents(
						(IPolygon) fromGeometryCollection))
				{
					yield return part;
				}
			}
			else
			{
				foreach (IGeometry part in GeometryUtils.GetParts(fromGeometryCollection))
				{
					yield return GeometryUtils.GetHighLevelGeometry(part);
				}
			}
		}

		#endregion

		#region Store results

		public static bool TryPrepareGeometryForStore([CanBeNull] IGeometry modifiedGeometry,
		                                              [NotNull] IFeature originalFeature,
		                                              [CanBeNull] out string failMessage)
		{
			failMessage = null;

			if (modifiedGeometry == null || modifiedGeometry.IsEmpty)
			{
				failMessage =
					$"Skipping overlap with feature {GdbObjectUtils.ToString(originalFeature)}. " +
					"The result would be an empty geometry.";

				return false;
			}

			if (GeometryUtils.AreEqual(originalFeature.Shape, modifiedGeometry))
			{
				_msg.DebugFormat("Feature {0} was not changed.",
				                 GdbObjectUtils.ToString(originalFeature));

				return false;
			}

			if (! GeometryUtils.TrySimplifyZ(modifiedGeometry))
			{
				// Find cases where this returns false. 
				_msg.DebugFormat(
					"Not enough Z values to calculate Z values. The store might fail!");
			}

			// sometimes they have NaN z values:
			GeometryUtils.Simplify(modifiedGeometry);

			if (modifiedGeometry.IsEmpty)
			{
				failMessage =
					$"Feature {GdbObjectUtils.ToString(originalFeature)} was skipped. Simplified new geometry was empty.";

				return false;
			}

			return true;
		}

		#endregion
	}
}