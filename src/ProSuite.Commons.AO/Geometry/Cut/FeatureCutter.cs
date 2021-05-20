using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.SpatialIndex;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.Cut
{
	/// <summary>
	/// Wraps a cut operation for polycurves and ring-based multipatches, includes 3D cutting
	/// for multipatches.
	/// </summary>
	public class FeatureCutter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IList<IFeature> _featuresToCut;

		[CanBeNull] private readonly INetworkFeatureCutter _networkFeatureCutter;

		private readonly ISpatialSearcher<IFeature> _spatialIndex;

		private IDictionary<IFeature, IGeometry> _targetsToUpdate;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureCutter"/> class.
		/// </summary>
		/// <param name="featuresToCut">The features that shall be cut.</param>
		/// <param name="networkFeatureCutter">An optional legacy geometric network edge cutter
		/// implementation.</param>
		public FeatureCutter(IList<IFeature> featuresToCut,
		                     [CanBeNull] INetworkFeatureCutter networkFeatureCutter = null)
		{
			Assert.ArgumentNotNull(featuresToCut, nameof(featuresToCut));

			_featuresToCut = featuresToCut;
			_networkFeatureCutter = networkFeatureCutter;

			if (featuresToCut.Count > 100)
			{
				_spatialIndex =
					SpatialHashSearcher<IFeature>.CreateSpatialSearcher(
						_featuresToCut, GetEnvelopeXY);
			}

			RefreshArea = new EnvelopeClass();
			ResultGeometriesByFeature = new Dictionary<IFeature, IList<IGeometry>>();
			ResultFeatures = new List<IFeature>();
			InsertedFeaturesByOriginal =
				new List<KeyValuePair<IFeature, IList<IFeature>>>(featuresToCut.Count);
		}

		public IFlexibleSettingProvider<ChangeAlongZSource> ZSourceProvider { get; set; }

		public bool ContinueOnFailure { get; set; }

		public DegenerateMultipatchFootprintAction DegenerateMultipatchFootprintAction { get; set; }
			= DegenerateMultipatchFootprintAction.Throw;

		[NotNull]
		public IList<ToolEditOperationObserver> EditOperationObservers { get; set; } =
			new List<ToolEditOperationObserver>(0);

		public IList<KeyValuePair<int, string>> FailedCutOperations { get; } =
			new List<KeyValuePair<int, string>>(0);

		/// <summary>
		/// The resulting cut geometries per original feature. Available once Cut has been called.
		/// </summary>
		public IDictionary<IFeature, IList<IGeometry>> ResultGeometriesByFeature { get; }

		/// <summary>
		/// The final result features. They are available once Save has been called.
		/// </summary>
		public IList<IFeature> ResultFeatures { get; }

		public IList<CutPolyline> ProcessedCutLines { get; set; }

		public ICollection<KeyValuePair<IFeature, IList<IFeature>>>
			InsertedFeaturesByOriginal { get; }

		public ICollection<IFeature> TargetFeatures { private get; set; }

		public Func<IGeometry, IGeometry, bool> FeatureToCutPredicate { get; set; }

		public IEnvelope RefreshArea { get; }

		public void Cut([NotNull] IList<CutSubcurve> cutSubcurves,
		                [CanBeNull] ITrackCancel trackCancel = null)
		{
			Assert.ArgumentNotNull(cutSubcurves, nameof(cutSubcurves));
			Assert.ArgumentCondition(_featuresToCut.Count > 0, "No polygon to cut");

			if (cutSubcurves.Count == 0)
			{
				return;
			}

			// simplify the curves to connect several yellow cut subcurves into a proper path
			IGeometry geometryPrototype = _featuresToCut[0].Shape;

			IGeometryCollection simplifiedCurves =
				ReshapeUtils.GetSimplifiedReshapeCurves(
					cutSubcurves, geometryPrototype.SpatialReference);

			if (simplifiedCurves.GeometryCount == 0)
			{
				return;
			}

			Cut((IPolyline) simplifiedCurves, trackCancel);

			if (TargetFeatures != null && ResultGeometriesByFeature.Count > 0)
			{
				InsertIntersectingVerticesInTargets(TargetFeatures,
				                                    (IGeometry) simplifiedCurves);
			}
		}

		public void Cut([NotNull] IPolyline cutPolyline,
		                [CanBeNull] ITrackCancel trackCancel = null)
		{
			foreach (IFeature feature in GetFeaturesToCut(cutPolyline))
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				try
				{
					CutFeature(feature, cutPolyline);
				}
				catch (Exception e)
				{
					if (! ContinueOnFailure)
					{
						throw;
					}

					_msg.Warn($"Feature {feature.OID}: {e.Message}", e);

					FailedCutOperations.Add(
						new KeyValuePair<int, string>(feature.OID, e.Message));
				}
			}

			AddToRefreshArea(cutPolyline);
		}

		public void Cut3D([NotNull] IMultiPatch cutSurfaces,
		                  [CanBeNull] ITrackCancel trackCancel = null)
		{
			foreach (IFeature feature in GetFeaturesToCut(cutSurfaces))
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				try
				{
					CutFeature3D(feature, cutSurfaces);
				}
				catch (Exception e)
				{
					if (! ContinueOnFailure)
					{
						throw;
					}

					FailedCutOperations.Add(
						new KeyValuePair<int, string>(feature.OID, e.Message));
				}
			}
		}

		/// <summary>
		/// Saves the result of a cut and returns all stored features.
		/// </summary>
		/// <param name="transaction">GDB transaction for executing a procedure in an edit operation</param>
		/// <param name="editWorkspace">The workspace in which the operation is executed</param>
		/// <param name="undoMessage">The description for the edit operation</param>
		/// <param name="onResultsSaved">Any action that happens inside a GDB transaction after the results have been saved (optional)</param>
		/// <param name="trackCancel">The cancel tracker, allowing the procedure to abort 
		/// the transaction (optional)</param>
		[NotNull]
		public IEnumerable<IFeature> SaveResults(
			[NotNull] IGdbTransaction transaction,
			[NotNull] IWorkspace editWorkspace,
			[NotNull] string undoMessage,
			[CanBeNull] Action<IEnumerable<KeyValuePair<IFeature, IList<IFeature>>>>
				onResultsSaved = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			var resultFeatures = new List<IFeature>();

			transaction.Execute(
				editWorkspace,
				delegate { StoreResultFeatures(resultFeatures, onResultsSaved); },
				undoMessage,
				trackCancel);

			return resultFeatures;
		}

		public void StoreResultFeatures(
			[CanBeNull] List<IFeature> allStoredFeatures,
			[CanBeNull] Action<IEnumerable<KeyValuePair<IFeature, IList<IFeature>>>>
				onResultsSaved = null)
		{
			foreach (var observer in EditOperationObservers)
			{
				observer.StartedOperation();
			}

			foreach (KeyValuePair<IFeature, IList<IGeometry>> keyValuePair in
				ResultGeometriesByFeature)
			{
				IFeature originalFeature = keyValuePair.Key;
				IList<IGeometry> newGeometries = keyValuePair.Value;

				IList<IFeature> cutResultFeatures;
				if (_networkFeatureCutter != null &&
				    _networkFeatureCutter.IsNetworkEdge(originalFeature))
				{
					// Allow legacy geometric network support:
					cutResultFeatures = SplitNetworkEdge(originalFeature, newGeometries);
				}
				else
				{
					IGeometry largestGeo =
						GeometryUtils.GetLargestGeometry(newGeometries);

					cutResultFeatures = StoreGeometries(newGeometries,
					                                    originalFeature,
					                                    Assert.NotNull(largestGeo),
					                                    null);
				}

				allStoredFeatures?.AddRange(cutResultFeatures);

				NotifySplitObservers(originalFeature, cutResultFeatures);
			}

			if (_targetsToUpdate != null)
			{
				allStoredFeatures?.AddRange(StoreUpdatedTargets());
			}

			onResultsSaved?.Invoke(InsertedFeaturesByOriginal);

			foreach (var observer in EditOperationObservers)
			{
				observer.CompletingOperation();
			}
		}

		public void LogSuccessfulCut()
		{
			if (ResultGeometriesByFeature.Count == 1)
			{
				_msg.InfoFormat("The selected feature was cut into {0} features",
				                GetResultPolygonCount(ResultGeometriesByFeature));
			}
			else
			{
				_msg.InfoFormat("{0} features were cut into {1} features",
				                ResultGeometriesByFeature.Count,
				                GetResultPolygonCount(ResultGeometriesByFeature));
			}
		}

		private void CutFeature3D(IFeature feature, IMultiPatch cutSurfaces)
		{
			IGeometry selectedGeometry = feature.Shape;

			if (GeometryUtils.Disjoint(cutSurfaces, selectedGeometry))
			{
				return;
			}

			if (GeometryUtils.AreEqual(cutSurfaces, selectedGeometry))
			{
				return;
			}

			if (FeatureToCutPredicate != null &&
			    ! FeatureToCutPredicate(selectedGeometry, cutSurfaces))
			{
				return;
			}

			// TODO: Multipatches should be cut if its footprint can be cut. However, if multiple ring groups exist,
			//       that are not connected (touch along a line) in 3D, the rings should be grouped into connected parts.

			List<CutPolyline> cutPolylines =
				IntersectionUtils.GetIntersectionLines3D((IMultiPatch) selectedGeometry,
				                                         cutSurfaces).ToList();

			foreach (CutPolyline cutPoline in cutPolylines)
			{
				cutPoline.ObjectId = feature.OID;

				// Add the calculated lines for reference and traceability, filtering and subsequent cutting
				// with Success field equals null
				ProcessedCutLines?.Add(cutPoline);
			}

			if (cutPolylines.Count > 0)
			{
				var success = false;

				// TODO: Consider filter option for cut lines or even a clever path selection logic to avoid junctions:
				// Only use cut lines that are in the plane to connect disjoint cut line paths that are positive or negative
				// Consider adding an option to delete the negative or the positive side of the rings.

				var cutLineToUse =
					(IPolyline) GeometryUtils.Union(
						cutPolylines.Select(c => c.Polyline).ToList());
				GeometryUtils.Simplify(cutLineToUse, true, false);

				if (! cutLineToUse.IsEmpty)
				{
					try
					{
						success = CutFeature(feature, cutLineToUse,
						                     ChangeAlongZSource.SourcePlane);
					}
					catch (Exception e)
					{
						_msg.WarnFormat("Cutting multipatch feature {0} failed.",
						                GdbObjectUtils.ToString(feature));
						_msg.Debug(e);
					}

					var appliedCutline = new CutPolyline(cutLineToUse);
					appliedCutline.SuccessfulCut = success;

					ProcessedCutLines?.Add(appliedCutline);
				}
			}
		}

		private bool CutFeature([NotNull] IFeature feature,
		                        [NotNull] IPolyline cutPolyline,
		                        ChangeAlongZSource? zSource = null)
		{
			IGeometry selectedGeometry = feature.Shape;

			if (GeometryUtils.Disjoint(cutPolyline, selectedGeometry))
			{
				return false;
			}

			if (FeatureToCutPredicate != null &&
			    ! FeatureToCutPredicate(selectedGeometry, cutPolyline))
			{
				return false;
			}

			_msg.DebugFormat("Cutting feature {0}", GdbObjectUtils.ToString(feature));

			IGeometry geometryToCut = GeometryFactory.Clone(selectedGeometry);

			CutPolyline usedCutLine = null;

			if (ProcessedCutLines != null)
			{
				usedCutLine = new CutPolyline(feature.OID);
				ProcessedCutLines.Add(usedCutLine);
			}

			IList<IGeometry> resultGeometries;

			switch (geometryToCut.GeometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					resultGeometries = CutGeometryUtils.TryCut(
						(IPolygon) geometryToCut, cutPolyline,
						zSource ?? DetermineZSource(feature));
					break;
				case esriGeometryType.esriGeometryPolyline:
					resultGeometries =
						_networkFeatureCutter?.IsNetworkEdge(feature) == true
							? GetNetworkSplitPoints(cutPolyline, geometryToCut)
							: CutGeometryUtils.TryCut((IPolyline) geometryToCut, cutPolyline);
					break;
				case esriGeometryType.esriGeometryMultiPatch:
					resultGeometries =
						CutGeometryUtils.TryCut((IMultiPatch) geometryToCut, cutPolyline,
						                        zSource ?? DetermineZSource(feature),
						                        usedCutLine, DegenerateMultipatchFootprintAction)
						                .Values.Cast<IGeometry>()
						                .ToList();
					break;
				default:
					throw new InvalidOperationException(
						$"Unsupported geometry type: {geometryToCut.GeometryType}");
			}

			if (resultGeometries != null && resultGeometries.Count > 0)
			{
				IList<IGeometry> previousResults;
				if (ResultGeometriesByFeature.TryGetValue(feature, out previousResults))
				{
					foreach (IGeometry resultGeometry in resultGeometries)
					{
						previousResults.Add(resultGeometry);
					}
				}
				else
				{
					ResultGeometriesByFeature.Add(feature, resultGeometries);
				}
			}

			return resultGeometries != null && resultGeometries.Count > 0;
		}

		private static EnvelopeXY GetEnvelopeXY(IFeature feature)
		{
			IEnvelope env = feature.Extent;

			return new EnvelopeXY(env.XMin, env.YMin, env.XMax, env.YMax);
		}

		private ChangeAlongZSource DetermineZSource(IFeature feature)
		{
			string note = null;
			ChangeAlongZSource zSource = ZSourceProvider?.GetValue(feature, out note) ??
			                             ChangeAlongZSource.Target;

			if (note != null)
			{
				_msg.Info(note);
			}

			return zSource;
		}

		private IEnumerable<IFeature> GetFeaturesToCut(IGeometry cutGeometry)
		{
			IEnumerable<IFeature> resultFeatures;

			if (_spatialIndex != null)
			{
				IEnvelope env = cutGeometry.Envelope;

				resultFeatures = _spatialIndex.Search(env.XMin, env.YMin, env.XMax,
				                                      env.YMax,
				                                      GeometryUtils.GetXyTolerance(env));
			}
			else
			{
				resultFeatures = _featuresToCut;
			}

			return resultFeatures;
		}

		private void InsertIntersectingVerticesInTargets(
			[NotNull] IEnumerable<IFeature> targetFeatures,
			[NotNull] IGeometry intersectingGeometry)
		{
			if (_targetsToUpdate == null)
			{
				_targetsToUpdate = new Dictionary<IFeature, IGeometry>();
			}

			ReshapeUtils.InsertIntersectingVerticesInTargets(targetFeatures,
			                                                 intersectingGeometry,
			                                                 _targetsToUpdate);
		}

		[NotNull]
		private IEnumerable<IFeature> StoreUpdatedTargets()
		{
			Assert.NotNull(_targetsToUpdate, "No updated targets");

			GdbObjectUtils.StoreGeometries(_targetsToUpdate);

			return _targetsToUpdate.Keys;
		}

		[NotNull]
		private IList<IFeature> StoreGeometries(
			[NotNull] ICollection<IGeometry> newGeometries,
			[NotNull] IFeature originalFeature,
			[NotNull] IGeometry geometryToStoreInOriginal,
			[CanBeNull] ITrackCancel trackCancel)
		{
			var storedFeatures = new List<IFeature>(newGeometries.Count + 1);

			if (trackCancel != null && ! trackCancel.Continue())
			{
				return storedFeatures;
			}

			// store the update
			IFeature stored = AssignGeometryToFeature(geometryToStoreInOriginal,
			                                          originalFeature,
			                                          false);

			if (stored != null)
			{
				storedFeatures.Add(stored);
			}

			// store other new geometries as inserts
			foreach (IGeometry modifyGeometry in
				newGeometries.Where(polycurve => polycurve != geometryToStoreInOriginal))
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return storedFeatures;
				}

				IFeature newFeature =
					AssignGeometryToFeature(modifyGeometry, originalFeature, true);

				if (newFeature != null)
				{
					storedFeatures.Add(newFeature);
				}
			}

			var inserts = new List<IFeature>(newGeometries.Count);

			foreach (IFeature storedFeature in storedFeatures)
			{
				ResultFeatures.Add(storedFeature);

				if (storedFeature != originalFeature)
				{
					inserts.Add(storedFeature);
				}
			}

			NotifySplittingObservers(originalFeature, inserts);

			foreach (IFeature feature in storedFeatures)
			{
				feature.Store();
			}

			InsertedFeaturesByOriginal.Add(
				new KeyValuePair<IFeature, IList<IFeature>>(originalFeature, inserts));

			return storedFeatures;
		}

		[CanBeNull]
		private static IFeature AssignGeometryToFeature(
			[CanBeNull] IGeometry modifiedGeometry,
			[NotNull] IFeature originalFeature,
			bool duplicateOriginalFeature)
		{
			if (modifiedGeometry == null || modifiedGeometry.IsEmpty)
			{
				_msg.WarnFormat(
					"Skipping result feature {0}. The result would be an empty geometry.",
					GdbObjectUtils.ToString(originalFeature));

				return null;
			}

			if (GeometryUtils.AreEqual(originalFeature.Shape, modifiedGeometry))
			{
				_msg.DebugFormat("Feature {0} was not changed.",
				                 GdbObjectUtils.ToString(originalFeature));

				return null;
			}

			FeatureStorageUtils.MakeGeometryStorable(modifiedGeometry, originalFeature.Shape,
			                                         originalFeature);

			IFeature resultFeature = null;
			if (! modifiedGeometry.IsEmpty)
			{
				resultFeature = duplicateOriginalFeature
					                ? GdbObjectUtils.DuplicateFeature(originalFeature, true)
					                : originalFeature;

				GdbObjectUtils.SetFeatureShape(resultFeature, modifiedGeometry);
			}
			else
			{
				_msg.WarnFormat(
					"Feature {0} was skipped. Simplified new geometry was empty.",
					GdbObjectUtils.ToString(originalFeature));
			}

			return resultFeature;
		}

		private static int GetResultPolygonCount(
			[NotNull] IDictionary<IFeature, IList<IGeometry>> resultGeometriesByFeature)
		{
			var result = 0;

			foreach (IList<IGeometry> resultList in resultGeometriesByFeature.Values)
			{
				result += resultList.Count;
			}

			return result;
		}

		private void NotifySplittingObservers(IFeature originalFeature,
		                                      List<IFeature> inserts)
		{
			foreach (var observer in EditOperationObservers)
			{
				_msg.DebugFormat(
					"Calling edit operation observer {0} for feature {1}",
					observer, GdbObjectUtils.ToString(originalFeature));

				observer.Splitting(originalFeature, inserts);
			}
		}

		private void NotifySplitObservers(IFeature originalFeature,
		                                  IList<IFeature> cutResultFeatures)
		{
			foreach (var observer in EditOperationObservers)
			{
				observer.Split(
					originalFeature,
					cutResultFeatures.Where(f => f != originalFeature));
			}
		}

		private List<IGeometry> GetNetworkSplitPoints(IPolyline cutPolyline,
		                                              IGeometry geometryToCut)
		{
			Assert.NotNull(_networkFeatureCutter);

			return new List<IGeometry>
			       {
				       _networkFeatureCutter.CalculateSplitPoints(
					       geometryToCut, cutPolyline)
			       };
		}

		private IList<IFeature> SplitNetworkEdge(IFeature edgeFeature,
		                                         IList<IGeometry> newGeometries)
		{
			IList<IFeature> cutResultFeatures;
			Assert.AreEqual(1, newGeometries.Count,
			                "Unexpected number of split geometries. Edge features require split point geometries.");

			IGeometry splitPoints = newGeometries[0];

			if (splitPoints.IsEmpty)
			{
				cutResultFeatures = new List<IFeature>(0);
			}
			else
			{
				NotifySplittingObservers(edgeFeature, new List<IFeature>(0));

				IList<IFeature> connectedEdges =
					Assert.NotNull(_networkFeatureCutter)
					      .SplitNetworkEdge(edgeFeature, (IPointCollection) splitPoints);

				InsertedFeaturesByOriginal.Add(
					new KeyValuePair<IFeature, IList<IFeature>>(
						edgeFeature,
						connectedEdges.Where(feature => feature != edgeFeature).ToList()));

				cutResultFeatures = connectedEdges;

				foreach (IFeature resultFeature in cutResultFeatures)
				{
					ResultFeatures.Add(resultFeature);
				}
			}

			return cutResultFeatures;
		}

		private void AddToRefreshArea(IGeometry geometry)
		{
			if (RefreshArea.IsEmpty)
			{
				RefreshArea.SpatialReference = geometry.SpatialReference;
			}

			RefreshArea.Union(geometry.Envelope);
		}
	}
}
