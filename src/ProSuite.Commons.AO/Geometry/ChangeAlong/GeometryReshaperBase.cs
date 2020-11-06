using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	[CLSCompliant(false)]
	public abstract class GeometryReshaperBase : IDisposable
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private ICollection<IFeature> _targetFeatures;

		private List<IPoint> _potentialTargetInsertPoints;

		private Dictionary<IFeature, double> _originalFeatureSize;

		protected IList<ToolEditOperationObserver> EditOperationObservers { get; set; }

		protected GeometryReshaperBase([NotNull] ICollection<IFeature> featuresToReshape, IList<ToolEditOperationObserver> editOperationObservers)
		{
			Assert.ArgumentNotNull(featuresToReshape, nameof(featuresToReshape));

			RefreshArea = new EnvelopeClass();

			ReshapeGeometryCloneByFeature =
				new Dictionary<IFeature, IGeometry>(featuresToReshape.Count);

			foreach (IFeature feature in featuresToReshape)
			{
				ReshapeGeometryCloneByFeature.Add(feature, feature.ShapeCopy);

				if (XyTolerance == null)
				{
					XyTolerance = GeometryUtils.GetXyTolerance(feature.Shape);
				}
				else if (! MathUtils.AreEqual(XyTolerance.Value,
				                              GeometryUtils.GetXyTolerance(feature.Shape)))
				{
					_msg.Debug(
						"Reshape multiple geometries: Not all features have the same spatial reference (xy tolerance).");
				}
			}
		}

		public Dictionary<IFeature, IGeometry> ReshapeGeometryCloneByFeature { get; protected set; }

		/// <summary>
		/// Target features that should also receive additional vertices introduced by reshape.
		/// For performance resons the target features that are not editable can be left out. 
		/// Non-editable target features will be filtered out.
		/// </summary>
		[CanBeNull]
		public ICollection<IFeature> TargetFeatures
		{
			get { return _targetFeatures; }
			set
			{
				_targetFeatures = value;

				if (_targetFeatures != null)
				{
					_potentialTargetInsertPoints = new List<IPoint>();
					UpdatedTargets =
						new Dictionary<IFeature, IGeometry>(_targetFeatures.Count);
				}
			}
		}

		/// <summary>
		/// Updated target geometry (with inserted points for topological correctness) by target feature. 
		/// This dictionary is populated if the TargetFeatures are set before reshaping.
		/// </summary>
		[CanBeNull]
		[CLSCompliant(false)]
		public IDictionary<IFeature, IGeometry> UpdatedTargets { get; private set; }

		public ReshapeResultFilter ResultFilter { get; set; }

		public bool AllowSimplifiedReshapeSideDetermination { protected get; set; }

		[CanBeNull]
		public ILinearNetworkFeatureFinder NetworkFeatureFinder { get; set; }

		[CanBeNull]
		public LinearNetworkNodeUpdater NetworkFeatureUpdater { get; set; }

		public bool RemoveClosedReshapePathAreas { protected get; set; }

		[NotNull]
		protected List<IGeometry> GeometriesToReshape
			=> new List<IGeometry>(ReshapeGeometryCloneByFeature.Values);

		public IEnvelope RefreshArea { get; }

		/// <summary>
		/// The map's XY tolerance which is relevant for the simplify operation.
		/// </summary>
		protected double? XyTolerance { get; set; }

		public bool UseMinimumTolerance { get; set; }

		public bool OpenJawReshapeOcurred { get; protected set; }

		public bool MoveLineEndJunction { protected get; set; }

		public int OpenJawIntersectionPointCount { get; protected set; }

		protected bool NotificationIsWarning { get; set; }
		public abstract bool AddRefreshAreaPadding { get; }

		#region Implementation of IDisposable

		/// <summary>
		/// Releases the Com objects to allow the GC to clean up.
		/// Before calling dispose, make sure no references to properties such as
		/// ReshapeGeometryCloneByFeature and UpdatedTargets of this class are held anywhere.
		/// </summary>
		public void Dispose()
		{
			using (_msg.IncrementIndentation())
			{
				foreach (IGeometry geometryClone in ReshapeGeometryCloneByFeature.Values)
				{
					int remainingRefs = Marshal.ReleaseComObject(geometryClone);

					_msg.DebugFormat("Remaining references to reshape clone: {0}",
					                 remainingRefs);
				}

				if (UpdatedTargets != null)
				{
					foreach (IGeometry geometryClone in UpdatedTargets.Values)
					{
						int remainingRefs = Marshal.ReleaseComObject(geometryClone);

						_msg.DebugFormat(
							"Remaining references to target geometry clone: {0}",
							remainingRefs);
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// Rehshapes all the provided geometries individually and calculates the reshape side
		/// regardless of the other geometries in the list. No Target features are updated as
		/// the provided reshape path is typically the edit sketch geometry.
		/// </summary>
		/// <param name="reshapePath"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		[NotNull]
		public abstract IDictionary<IGeometry, NotificationCollection> Reshape(
			[NotNull] IPath reshapePath,
			bool tryReshapeRingNonDefaultSide,
			[CanBeNull] NotificationCollection notifications);

		/// <summary>
		/// Reshapes the provided geometries with the provided reshape paths. Multiple
		/// paths are currently only supported for special multi-polygon reshapes
		/// </summary>
		/// <param name="reshapePaths"></param>
		/// <param name="tryReshapeRingNonDefaultSide"></param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		public abstract IDictionary<IGeometry, NotificationCollection> Reshape(
			[NotNull] IList<IPath> reshapePaths,
			bool tryReshapeRingNonDefaultSide,
			[CanBeNull] NotificationCollection notifications);

		public abstract IDictionary<IGeometry, NotificationCollection>
			EnsureResultsNotOverlapping(
				IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
				IList<IPath> reshapePaths);

		[NotNull]
		public Dictionary<IGeometry, NotificationCollection> Reshape(
			[NotNull] IReshapeAlongCurves reshapeAlongCurves,
			[CanBeNull] Predicate<IPath> canReshapePredicate,
			[CanBeNull] NotificationCollection notifications,
			bool useNonDefaultReshapeSide)
		{
			Dictionary<IGeometry, NotificationCollection> reshapedGeometries =
				ReshapeCore(reshapeAlongCurves, canReshapePredicate,
				            useNonDefaultReshapeSide,
				            notifications);

			return reshapedGeometries;
		}

		[NotNull]
		public Dictionary<IGeometry, NotificationCollection> Reshape(
			[NotNull] IList<CutSubcurve> reshapeCurves,
			[CanBeNull] NotificationCollection notifications,
			bool useNonDefaultReshapeSide)
		{
			Dictionary<IGeometry, NotificationCollection> reshapedGeometries =
				ReshapeCore(reshapeCurves, useNonDefaultReshapeSide, notifications);

			return reshapedGeometries;
		}

		/// <summary>
		/// Saves the result of a reshape within the provided transaction and returns the updated features 
		/// (including updated targets and adjacent network edges).
		/// </summary>
		/// <param name="transaction"></param>
		/// <param name="editWorkspace"></param>
		/// <param name="undoMessage"></param>
		/// <param name="reshapedGeometries"></param>
		public IList<IFeature> SaveResult(
			[NotNull] IGdbTransaction transaction,
			[NotNull] IWorkspace editWorkspace,
			[NotNull] string undoMessage,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries)
		{
			// for showing size difference after store:
			if (_originalFeatureSize == null)
			{
				CacheOriginalFeatureSize();
			}

			var result = new List<IFeature>();

			transaction.Execute(
				editWorkspace,
				() => result = Save(reshapedGeometries), undoMessage);

			return result;
		}

		/// <summary>
		/// Saves the result of a reshape and returns the updated features (including updated targets and adjacent network edges).
		/// The transaction must have been started previously.
		/// </summary>
		/// <param name="reshapedGeometries"></param>
		/// <returns></returns>
		public List<IFeature> Save(
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries)
		{
			var result = new List<IFeature>();

			foreach (
				KeyValuePair<IFeature, IGeometry> keyValuePair in
				GetReshapedFeatures(reshapedGeometries.Keys))
			{
				IFeature feature = keyValuePair.Key;
				IGeometry geometryClone = keyValuePair.Value;

				StoreReshapedGeometry(geometryClone, feature,
				                      reshapedGeometries[geometryClone],
				                      NotificationIsWarning);

				result.Add(feature);
			}

			// Only insert the target vertices after the source is simplified to ensure source-target vertex consistency
			if (TargetFeatures != null)
			{
				UpdateTargets();
			}

			if (UpdatedTargets != null)
			{
				GdbObjectUtils.StoreGeometries(UpdatedTargets);
				result.AddRange(UpdatedTargets.Keys);
			}

			return result;
		}

		public void LogSuccessfulReshape(
			[NotNull] ICollection<IGeometry> reshapedGeometries,
			esriUnits mapUnits, esriUnits distanceUnits)
		{
			LogSuccessfulReshape(null, reshapedGeometries, mapUnits, distanceUnits);
		}

		public void LogSuccessfulReshape(
			[CanBeNull] string titleMessage,
			[NotNull] ICollection<IGeometry> reshapedGeometries,
			esriUnits mapUnits, esriUnits distanceUnits)
		{
			IEnumerable<string> messages = GetReshapedFeaturesMessages(
				reshapedGeometries, mapUnits, distanceUnits);

			string msg = string.Empty;
			if (! string.IsNullOrEmpty(titleMessage))
			{
				msg += titleMessage;
				// string.Format("{0}{1}", titleMessage, Environment.NewLine);
			}

			foreach (string message in messages)
			{
				if (! string.IsNullOrEmpty(msg))
				{
					msg += Environment.NewLine;
				}

				msg += message;
			}

			_msg.Info(msg);
		}

		public bool ResultWithinOtherResultButNotInOriginal(
			[NotNull] ICollection<IGeometry> reshapedGeometries,
			out IPolygon withinPolygon)
		{
			// Circumcision-reshape:

			// If the smaller result geometry is contained in a larger result geometry
			// but the smaller original is not contained in the larger original (?) 
			// and neither is the smaller result contained in the larger original
			bool reshapedGeometriesContained =
				AnyPolygonWithinOther(reshapedGeometries, out withinPolygon);

			if (! reshapedGeometriesContained)
			{
				return false;
			}

			IPolygon originalWithinPoly;
			bool originalGeometriesContained =
				AnyPolygonWithinOther(
					GdbObjectUtils.GetGeometries(ReshapeGeometryCloneByFeature.Keys),
					out originalWithinPoly);

			return ! originalGeometriesContained;
		}

		public bool HasSizeChangePercentageAboveThreshold(
			[NotNull] ICollection<IGeometry> reshapedGeometries,
			esriUnits mapUnits, esriUnits distanceUnits, double threshold,
			[CanBeNull] NotificationCollection notifications)
		{
			var result = false;

			if (_originalFeatureSize == null)
			{
				CacheOriginalFeatureSize();
			}

			Dictionary<IFeature, double> originalFeatureSize =
				Assert.NotNull(_originalFeatureSize);

			foreach (
				KeyValuePair<IFeature, IGeometry> keyValuePair in
				GetReshapedFeatures(reshapedGeometries))
			{
				IFeature feature = keyValuePair.Key;
				IGeometry updatedGeometry = keyValuePair.Value;

				double sizeChangeInMapUnits =
					ReshapeUtils.GetAreaOrLength(updatedGeometry) -
					originalFeatureSize[feature];

				double percentSizeChangeInMapUnits = Math.Abs(
					                                     sizeChangeInMapUnits /
					                                     originalFeatureSize[feature]) *
				                                     100;

				if (percentSizeChangeInMapUnits > threshold)
				{
					result = true;

					string sizeChangeText = GetSizeChangeText(
						sizeChangeInMapUnits, distanceUnits,
						mapUnits, updatedGeometry);

					NotificationUtils.Add(notifications,
					                      "{0} is {1} which is a {2:0}% change",
					                      RowFormat.Format(feature, true), sizeChangeText,
					                      percentSizeChangeInMapUnits);
				}
			}

			return result;
		}

		[NotNull]
		protected abstract Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			[NotNull] IReshapeAlongCurves reshapeAlongCurves,
			[CanBeNull] Predicate<IPath> canReshapePredicate,
			bool useNonDefaultReshapeSide,
			[CanBeNull] NotificationCollection notifications);

		[NotNull]
		protected abstract Dictionary<IGeometry, NotificationCollection> ReshapeCore(
			[NotNull] IList<CutSubcurve> reshapeCurves,
			bool useNonDefaultReshapeSide,
			[CanBeNull] NotificationCollection notifications);

		protected void AddPotentialTargetInsertPoints(
			[NotNull] IEnumerable<CutSubcurve> reshapeCurves)
		{
			if (TargetFeatures == null)
			{
				return;
			}

			Assert.NotNull(_potentialTargetInsertPoints,
			               "_potentialTargetInsertPoints not initialized.");

			IPointCollection potentialTargetPoints = GetPotentialTargetInsertPoints(
				reshapeCurves);

			AddPotentialTargetInsertPoints(potentialTargetPoints);
		}

		protected void AddPotentialTargetInsertPoints(
			[NotNull] CutSubcurve cutSubcurve)
		{
			Assert.ArgumentNotNull(cutSubcurve, nameof(cutSubcurve));

			if (TargetFeatures == null)
			{
				return;
			}

			foreach (IPoint point in cutSubcurve.GetPotentialTargetInsertPoints())
			{
				_potentialTargetInsertPoints.Add(point);
			}
		}

		protected void UpdateTargets()
		{
			ICollection<IFeature> targetFeatures = Assert.NotNull(TargetFeatures,
			                                                      "Target features not set.");
			Assert.NotNull(UpdatedTargets, "Updated target features not set.");

			// Remove the non-source points and update the points that are almost at a source point 
			// (within the distance the simplify operation could have moved the source point)
			double tolerance = Assert.NotNull(XyTolerance, "XyTolerance not set.").Value
			                   * 2 * Math.Sqrt(2);

			List<IPoint> pointsToRemove =
				_potentialTargetInsertPoints.Where(
					potentialTargetInsertPoint =>
						! EnsurePointIsInGeometryToReshape(
							potentialTargetInsertPoint,
							tolerance)).ToList();

			foreach (IPoint pointToRemove in pointsToRemove)
			{
				_potentialTargetInsertPoints.Remove(pointToRemove);
			}

			foreach (IFeature targetFeature in targetFeatures)
			{
				if (DatasetUtils.IsBeingEdited(targetFeature.Class))
				{
					AddUpdatedTargetGeometry(targetFeature);
				}
			}
		}

		/// <summary>
		/// Collects all reshapeCurves in a polyline geometry, simplifies it and then reshapes using
		/// each path of the simplified polyline.
		/// </summary>
		/// <param name="geometryToReshape"></param>
		/// <param name="reshapeCurves"></param>
		/// <param name="useNonDefaultReshapeSide"></param>
		/// <param name="notifications"></param>
		/// <param name="reshapeInfos"></param>
		/// <returns></returns>
		protected IGeometry ReshapeWithSimplifiedCurves(
			[NotNull] IGeometry geometryToReshape,
			[NotNull] IEnumerable<CutSubcurve> reshapeCurves,
			bool useNonDefaultReshapeSide,
			[CanBeNull] NotificationCollection notifications,
			out ICollection<ReshapeInfo> reshapeInfos)
		{
			IGeometryCollection simplifiedCurves =
				ReshapeUtils.GetSimplifiedReshapeCurves(reshapeCurves,
				                                        geometryToReshape
					                                        .SpatialReference,
				                                        UseMinimumTolerance);

			var paths = new List<IPath>();
			var closedPaths = new List<IPath>();

			IPolygon polygonToRemove = null;
			if (RemoveClosedReshapePathAreas)
			{
				// separate closed paths (used for remove-polygon) from normal paths
				foreach (IPath path in GeometryUtils.GetPaths(
					(IGeometry) simplifiedCurves))
				{
					if (path.IsClosed)
					{
						closedPaths.Add(path);
					}
					else
					{
						paths.Add(path);
					}
				}

				if (closedPaths.Count > 0)
				{
					polygonToRemove = GeometryFactory.CreatePolygon(closedPaths);
				}
			}
			else
			{
				paths = new List<IPath>(
					GeometryUtils.GetPaths((IGeometry) simplifiedCurves));
			}

			ReshapeResultFilter resultFilter = GetResultFilter(useNonDefaultReshapeSide);
			bool reshaped = ReshapeUtils.ReshapeGeometry(geometryToReshape, paths,
			                                             resultFilter,
			                                             notifications, out reshapeInfos);

			if (reshaped)
			{
				reshaped = AreAllReshapesAllowed(reshapeInfos, notifications);
			}

			IGeometry result = reshaped
				                   ? geometryToReshape
				                   : null;

			var polygonToReshape = geometryToReshape as IPolygon;

			if (polygonToRemove != null && polygonToReshape != null)
			{
				// TODO: specific result object that every method can modify
				IDictionary<IGeometry, NotificationCollection> results =
					new Dictionary<IGeometry, NotificationCollection>(1);
				RemoveArea(polygonToRemove, polygonToReshape, results, notifications);

				result = results.Keys.First();

				foreach (IPath closedPath in closedPaths)
				{
					reshapeInfos.Add(
						new ReshapeInfo(result, closedPath,
						                new NotificationCollection()));
				}
			}

			return result;
		}

		private IEnumerable<string> GetReshapedFeaturesMessages(
			[NotNull] ICollection<IGeometry> reshapedGeometries,
			esriUnits mapUnits, esriUnits distanceUnits)
		{
			IList<string> result = new List<string>();

			foreach (
				KeyValuePair<IFeature, IGeometry> keyValuePair in
				GetReshapedFeatures(reshapedGeometries))
			{
				IFeature feature = keyValuePair.Key;
				IGeometry updatedGeometry = keyValuePair.Value;

				double sizeChangeMapUnits =
					ReshapeUtils.GetAreaOrLength(updatedGeometry) -
					_originalFeatureSize[feature];

				string sizeChangeText = GetSizeChangeText(
					sizeChangeMapUnits, distanceUnits,
					mapUnits,
					updatedGeometry);

				result.Add(string.Format("{0} was reshaped and is now {1}",
				                         RowFormat.Format(feature, true),
				                         sizeChangeText));
			}

			return result;
		}

		protected void AddToRefreshArea(IEnumerable<ReshapeInfo> reshapeInfos)
		{
			foreach (ReshapeInfo reshapeInfo in reshapeInfos)
			{
				AddToRefreshArea(reshapeInfo);
			}
		}

		protected void AddToRefreshArea(ReshapeInfo reshapeInfo)
		{
			RefreshArea.Union(reshapeInfo.ReshapePath.Envelope);

			// using replaced segments is an important optimization, especially for large polygons
			RefreshArea.Union(reshapeInfo.ReplacedSegments?.Envelope ??
			                  reshapeInfo.GeometryToReshape.Envelope);
		}

		public void AddToRefreshArea(IGeometry geometry)
		{
			if (geometry == null || geometry.IsEmpty)
			{
				return;
			}

			RefreshArea.Union(geometry.Envelope);
		}

		protected static void ReleaseReshapeInfos(
			[NotNull] IEnumerable<ReshapeInfo> reshapeInfos)
		{
			foreach (ReshapeInfo reshapeInfo in reshapeInfos)
			{
				reshapeInfo.Dispose();
			}
		}

		private void CacheOriginalFeatureSize()
		{
			_originalFeatureSize =
				new Dictionary<IFeature, double>(ReshapeGeometryCloneByFeature.Count);

			foreach (IFeature feature in ReshapeGeometryCloneByFeature.Keys)
			{
				// use original shape:
				IGeometry originalShape = feature.Shape;

				_originalFeatureSize.Add(
					feature, ReshapeUtils.GetAreaOrLength(originalShape));

				Marshal.ReleaseComObject(originalShape);
			}
		}

		private static string GetSizeChangeText(double sizeChangeMapUnits,
		                                        esriUnits distanceUnits,
		                                        esriUnits mapUnits,
		                                        [NotNull] IGeometry updatedGeometry)
		{
			bool is2D = updatedGeometry.Dimension ==
			            esriGeometryDimension.esriGeometry2Dimension;

			double sizeChangeDistanceUnits;

			if (is2D)
			{
				sizeChangeDistanceUnits = GeometryUtils.ConvertArea(
					sizeChangeMapUnits, mapUnits,
					distanceUnits);
			}
			else
			{
				sizeChangeDistanceUnits = GeometryUtils.ConvertDistance(
					sizeChangeMapUnits,
					mapUnits, distanceUnits);
			}

			string sizeChangeText = GetSizeChangeText(sizeChangeDistanceUnits, is2D,
			                                          distanceUnits);
			return sizeChangeText;
		}

		private static string GetSizeChangeText(double sizeDifference,
		                                        bool isArea,
		                                        esriUnits displayUnits)
		{
			string unitDisplay = string.Format("{0}{1}",
			                                   SpatialReferenceUtils.GetAbbreviation(
				                                   displayUnits),
			                                   isArea
				                                   ? "²"
				                                   : string.Empty);

			bool more = sizeDifference > 0;

			string sizeAdjective = GetSizeAdjective(more, isArea);

			const int significantDigits = 3;

			double displayNumber =
				Math.Abs(
					MathUtils.RoundToSignificantDigits(
						sizeDifference, significantDigits));

			// fix 72000000 mm where it would actually be 721234567
			if (Math.Abs(Math.Truncate(displayNumber) - displayNumber) < double.Epsilon)
			{
				displayNumber = Math.Abs(Math.Truncate(sizeDifference));
			}

			// fix 2.1E-05 nm²
			string formatNonScientific = StringUtils.FormatNonScientific(
				displayNumber, CultureInfo.CurrentCulture);

			string lengthText = string.Format(
				"{0} {1} {2}",
				formatNonScientific,
				unitDisplay, sizeAdjective);

			return lengthText;
		}

		private static string GetSizeAdjective(bool isMore, bool isArea)
		{
			string sizeAdjective;
			if (isMore)
			{
				if (isArea)
				{
					sizeAdjective = "larger";
				}
				else
				{
					sizeAdjective = "longer";
				}
			}
			else
			{
				if (isArea)
				{
					sizeAdjective = "smaller";
				}
				else
				{
					sizeAdjective = "shorter";
				}
			}

			return sizeAdjective;
		}

		private void AddPotentialTargetInsertPoints(
			[NotNull] IPointCollection potentialTargetPoints)
		{
			for (var i = 0; i < potentialTargetPoints.PointCount; i++)
			{
				IPoint point = potentialTargetPoints.get_Point(i);

				_potentialTargetInsertPoints.Add(point);
			}
		}

		private void AddUpdatedTargetGeometry([NotNull] IFeature targetFeature)
		{
			IDictionary<IFeature, IGeometry> updatedTargets =
				Assert.NotNull(UpdatedTargets,
				               "Updated target dictionary not initialized.");

			IGeometry targetGeometry = updatedTargets.ContainsKey(targetFeature)
				                           ? updatedTargets[targetFeature]
				                           : targetFeature.ShapeCopy;

			// in case the map is in a different SR
			GeometryUtils.EnsureSpatialReference(targetGeometry, targetFeature);

			var xyTolerance = Assert.NotNull(XyTolerance).Value;

			if (ReshapeUtils.EnsurePointsExistInTarget(
				    targetGeometry, _potentialTargetInsertPoints, xyTolerance) &&
			    ! updatedTargets.ContainsKey(targetFeature))
			{
				updatedTargets.Add(targetFeature, targetGeometry);
			}
		}

		[NotNull]
		private static IPointCollection GetPotentialTargetInsertPoints(
			[NotNull] IEnumerable<CutSubcurve> originalReshapeCurves)
		{
			Assert.ArgumentNotNull(originalReshapeCurves, nameof(originalReshapeCurves));

			ICollection<CutSubcurve> originalReshapeCurvesCollection =
				CollectionUtils.GetCollection(originalReshapeCurves);

			// NOTE: Use original reshape curves rather than the reshape path from reshapeInfo
			//		 to ensure we don't miss the ends of the yellow lines (which are joined 
			//		 before assigned as reshapeInfo's reshape path)
			var result = (IPointCollection) GeometryFactory.CreateMultipoint(
				GetTargetInsertPoints(originalReshapeCurvesCollection));

			GeometryUtils.Simplify((IGeometry) result);

			return result;
		}

		private bool EnsurePointIsInGeometryToReshape([NotNull] IPoint point,
		                                              double xyTolerance)
		{
			IPoint vertex = new PointClass();

			foreach (IGeometry geometryToReshape in ReshapeGeometryCloneByFeature.Values)
			{
				var sourcePoints = (IPointCollection) geometryToReshape;

				// TODO: use sqrt(2) of tolerance: hit test checks different to IRelationalOperator
				int globalIndex = GeometryUtils.QueryVertex(
					sourcePoints, point, xyTolerance, vertex);

				if (globalIndex >= 0)
				{
					// Ensure the exact same coordinates as in source because the actual intersection could be slightly
					// off the target vertex which is the original source-target intersection point.
					IPoint sourcePoint = sourcePoints.get_Point(globalIndex);
					point.X = sourcePoint.X;
					point.Y = sourcePoint.Y;
					point.Z = sourcePoint.Z;

					return true;
				}
			}

			return false;
		}

		[NotNull]
		private static IEnumerable<IPoint> GetTargetInsertPoints(
			[NotNull] IEnumerable<CutSubcurve> reshapeCurves)
		{
			foreach (CutSubcurve subcurve in reshapeCurves)
			{
				// NOTE: CutSubcurve can be null 
				if (subcurve == null)
				{
					continue;
				}

				foreach (IPoint point in subcurve.GetPotentialTargetInsertPoints())
				{
					yield return point;
				}
			}
		}

		[NotNull]
		private IEnumerable<KeyValuePair<IFeature, IGeometry>> GetReshapedFeatures(
			[NotNull] ICollection<IGeometry> reshapedGeometries)
		{
			foreach (
				KeyValuePair<IFeature, IGeometry> keyValuePair in
				ReshapeGeometryCloneByFeature)
			{
				IGeometry geometryClone = keyValuePair.Value;

				if (! reshapedGeometries.Contains(geometryClone))
				{
					continue;
				}

				yield return keyValuePair;
			}
		}

		private void StoreReshapedGeometry([NotNull] IGeometry newGeometry,
		                                   [NotNull] IFeature feature,
		                                   [CanBeNull] NotificationCollection notifications,
		                                   bool warn)
		{
			StoreReshapedGeometry(newGeometry, feature, notifications);

			string message = notifications?.Concatenate(". ");

			if (! string.IsNullOrEmpty(message))
			{
				message =
					string.Format("{0} <oid> {1}: {2}", feature.Class.AliasName,
					              feature.OID,
					              message);

				if (warn)
				{
					_msg.Warn(message);
				}
				else
				{
					_msg.Info(message);
				}
			}
		}

		private void StoreReshapedGeometry([NotNull] IGeometry geometry,
		                                   [NotNull] IFeature feature,
		                                   [CanBeNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeometry originalGeometry = feature.Shape;

			// TODO: Add NetworkFeatureFinder.IsEdgeFeature(IFeature) or expose underlying network def.
			bool isLinearNetworkEdgeClass = NetworkFeatureFinder != null;

			// TOP-5212: Generally allow loops in line features. However, if the original was multipart
			// (disjoint paths) it should be possible to create a single path by connecting them using a
			// Y-reshape prolongation.
			// -> Use the hard-core simplify for non-network lines to union disjoint paths that were
			// connected using a Y-reshape-prolongation. SimplifyNetwork does not merge 
			// touching paths into one. However, in the allow loops in line features that were 
			bool allowSplitAndUnsplit =
				geometry.GeometryType == esriGeometryType.esriGeometryPolyline &&
				! isLinearNetworkEdgeClass && GeometryUtils.GetPartCount(originalGeometry) > 1;

			FeatureStorageUtils.MakeGeometryStorable(geometry, originalGeometry, feature,
			                                         allowSplitAndUnsplit);

			Marshal.ReleaseComObject(originalGeometry);

			Stopwatch watch = _msg.DebugStartTiming("Storing {0}...",
			                                        GdbObjectUtils.ToString(feature));

			Assert.False(geometry.IsEmpty, "Reshape result is empty.");

			foreach (var observer in EditOperationObservers)
			{
				observer.Updating(feature);
			}

			StoreReshapedGeometryCore(feature, geometry, notifications);

			_msg.DebugStopTiming(watch, "Stored geometry in {0}",
			                     GdbObjectUtils.ToString(feature));
		}

		protected virtual void StoreReshapedGeometryCore(
			[NotNull] IFeature feature,
			[NotNull] IGeometry newGeometry,
			[CanBeNull] NotificationCollection notifications)
		{
			GdbObjectUtils.SetFeatureShape(feature, newGeometry);
			
			feature.Store();
		}

		private static bool AnyPolygonWithinOther(IEnumerable<IGeometry> geometries,
		                                          [CanBeNull] out IPolygon withinPolygon)
		{
			var result = false;
			withinPolygon = null;
			foreach (
				KeyValuePair<IGeometry, IGeometry> keyValuePair in
				CollectionUtils.GetAllTuples(geometries))
			{
				if (keyValuePair.Key.GeometryType !=
				    esriGeometryType.esriGeometryPolygon ||
				    keyValuePair.Value.GeometryType !=
				    esriGeometryType.esriGeometryPolygon)
				{
					continue;
				}

				if (GeometryUtils.Contains(keyValuePair.Key, keyValuePair.Value))
				{
					result = true;

					AssignPolygon((IPolygon) keyValuePair.Value, ref withinPolygon);
				}
				else if (GeometryUtils.Contains(keyValuePair.Value, keyValuePair.Key))
				{
					result = true;

					AssignPolygon((IPolygon) keyValuePair.Key, ref withinPolygon);
				}
			}

			return result;
		}

		private static void AssignPolygon(IPolygon polygon, ref IPolygon toResult)
		{
			if (toResult == null)
			{
				toResult = GeometryFactory.Clone(polygon);
			}
			else
			{
				((IGeometryCollection) toResult).AddGeometryCollection(
					(IGeometryCollection) polygon);
			}
		}

		protected void RemoveArea(
			[NotNull] IPolygon areaToRemove,
			[NotNull] IPolygon fromGeometry,
			[NotNull] IDictionary<IGeometry, NotificationCollection> reshapedGeometries,
			[CanBeNull] NotificationCollection notifications)
		{
			if (GeometryUtils.Disjoint(areaToRemove, fromGeometry))
			{
				return;
			}

			IGeometry reducedGeometry =
				((ITopologicalOperator) fromGeometry).Difference(areaToRemove);

			KeyValuePair<IFeature, IGeometry> featureGeometry =
				ReshapeGeometryCloneByFeature.First(pair => (pair.Value == fromGeometry));

			int originalPartCount = GeometryUtils.GetExteriorRingCount(fromGeometry);
			int newPartCount =
				GeometryUtils.GetExteriorRingCount(((IPolygon) reducedGeometry));

			if (originalPartCount < newPartCount)
			{
				NotificationUtils.Add(notifications,
				                      "Removing the sketch-area from {0} results would result in multiple parts. The area was not removed.",
				                      GdbObjectUtils.ToString(featureGeometry.Key));
				NotificationIsWarning = true;
			}
			else if (originalPartCount > newPartCount)
			{
				NotificationUtils.Add(notifications,
				                      "Removing the sketch-area from {0} results would result in entire parts being deleted. The area was not removed.",
				                      GdbObjectUtils.ToString(featureGeometry.Key));
				NotificationIsWarning = true;
			}
			else if (AreaRemovalProducedBoundaryLoop(fromGeometry, areaToRemove))
			{
				// if the ring to remove touches the polygon in one point only -> boundary loop
				NotificationUtils.Add(notifications,
				                      "Removing the sketch-area from {0} results would result in a boundary loop. The area was not removed.",
				                      GdbObjectUtils.ToString(featureGeometry.Key));
				NotificationIsWarning = true;
			}
			else
			{
				ReshapeGeometryCloneByFeature[featureGeometry.Key] = reducedGeometry;

				NotificationCollection existingNotifications;
				if (reshapedGeometries.TryGetValue(fromGeometry,
				                                   out existingNotifications))
				{
					reshapedGeometries.Remove(fromGeometry);
				}

				reshapedGeometries.Add(reducedGeometry, existingNotifications);

				RefreshArea.Union(areaToRemove.Envelope);
			}
		}

		protected ReshapeResultFilter GetResultFilter(bool useNonDefaultReshapeSide)
		{
			ReshapeResultFilter resultFilter = ResultFilter ??
			                                   new ReshapeResultFilter(
				                                   useNonDefaultReshapeSide);

			resultFilter.UseNonDefaultReshapeSide = useNonDefaultReshapeSide;

			return resultFilter;
		}

		protected bool IsReshapeAllowed([NotNull] ReshapeInfo reshapeInfo,
		                                [CanBeNull] NotificationCollection notifications)
		{
			if (ResultFilter == null)
			{
				return true;
			}

			return ResultFilter.IsResultAllowed(reshapeInfo, notifications);
		}

		protected bool AreAllReshapesAllowed(
			[NotNull] IEnumerable<ReshapeInfo> reshapeInfos,
			[CanBeNull] NotificationCollection notifications)
		{
			if (ResultFilter == null)
			{
				return true;
			}

			var allowed = true;
			foreach (ReshapeInfo info in reshapeInfos)
			{
				if (! ResultFilter.IsResultAllowed(info, notifications))
				{
					allowed = false;
				}
			}

			return allowed;
		}

		private static bool AreaRemovalProducedBoundaryLoop(IPolygon originalPolygon,
		                                                    IPolygon areaToRemove)
		{
			var boundary = (IPolyline) GeometryUtils.GetBoundary(originalPolygon);

			if (GeometryUtils.Touches(areaToRemove, boundary))
			{
				// It must touch in one point only
				var topoOp = (ITopologicalOperator) boundary;
				IGeometry intersection = topoOp.Intersect(
					areaToRemove, esriGeometryDimension.esriGeometry1Dimension);

				if (intersection.IsEmpty)
				{
					return true;
				}
			}

			return false;
		}
	}
}
