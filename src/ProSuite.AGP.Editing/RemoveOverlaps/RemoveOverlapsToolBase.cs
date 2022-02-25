using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public abstract class RemoveOverlapsToolBase : TwoPhaseEditToolBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private Overlaps _overlaps;
		private RemoveOverlapsFeedback _feedback;
		private IList<Feature> _overlappingFeatures;

		protected RemoveOverlapsToolBase()
		{
			GeomIsSimpleAsFeature = false;

			SelectionCursor = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursorShift);
			SecondPhaseCursor = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursorProcess);
		}

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override void OnUpdate()
		{
			Enabled = MicroserviceClient != null;

			Tooltip = Enabled
				          ? "Remove a part of a feature that overlaps with other polygon features"
				          : "Microservice not found / not started. Please make sure the latest ProSuite Extension is installed.";
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new RemoveOverlapsFeedback();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.RemoveOverlapsTool_LogPromptForSelection);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Multipatch;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			IList<Feature> overlappingFeatures =
				GetOverlappingFeatures(selectedFeatures, progressor);

			if (progressor != null && ! progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of removable overlaps was cancelled.");
				return;
			}

			_overlaps = CalculateOverlaps(selectedFeatures, overlappingFeatures, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of removable overlaps was cancelled.");
				return;
			}

			// TODO: Options
			bool insertVerticesInTarget = true;
			_overlappingFeatures = insertVerticesInTarget
				                       ? overlappingFeatures
				                       : null;

			_feedback.Update(_overlaps);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _overlaps != null && _overlaps.HasOverlaps();
		}

		protected override bool SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_overlaps);

			Overlaps overlapsToRemove = SelectOverlaps(_overlaps, sketch);

			if (! overlapsToRemove.HasOverlaps())
			{
				return false;
			}

			IEnumerable<Feature> selectedFeatures = MapUtils.GetFeatures(selection);

			RemoveOverlapsResult result =
				MicroserviceClient.RemoveOverlaps(
					selectedFeatures, overlapsToRemove, _overlappingFeatures,
					progressor?.CancellationToken ?? new CancellationTokenSource().Token);

			var updates = new Dictionary<Feature, Geometry>();
			var inserts = new Dictionary<Feature, IList<Geometry>>();

			HashSet<long> editableClassHandles =
				MapUtils.GetLayers<BasicFeatureLayer>(bfl => bfl.IsEditable)
				        .Select(l => l.GetTable().Handle.ToInt64()).ToHashSet();

			foreach (OverlapResultGeometries resultPerFeature in result.ResultsByFeature)
			{
				Feature originalFeature = resultPerFeature.OriginalFeature;
				Geometry updatedGeometry = resultPerFeature.UpdatedGeometry;

				if (! IsStoreRequired(originalFeature, updatedGeometry, editableClassHandles))
				{
					continue;
				}

				updates.Add(originalFeature, updatedGeometry);

				if (resultPerFeature.InsertGeometries.Count > 0)
				{
					inserts.Add(originalFeature,
					            resultPerFeature.InsertGeometries);
				}
			}

			if (result.TargetFeaturesToUpdate != null)
			{
				var updatedTargets = new List<Feature>();
				foreach (KeyValuePair<Feature, Geometry> kvp in result.TargetFeaturesToUpdate)
				{
					if (! IsStoreRequired(kvp.Key, kvp.Value, editableClassHandles))
					{
						continue;
					}

					updatedTargets.Add(kvp.Key);
					updates.Add(kvp.Key, kvp.Value);
				}

				if (updatedTargets.Count > 0)
				{
					_msg.InfoFormat("Target features with potential vertex insertions: {0}",
				                 StringUtils.Concatenate(updatedTargets,
				                                         GdbObjectUtils.GetDisplayValue, ", "));
				}
			}

			IEnumerable<Dataset> datasets =
				GdbPersistenceUtils.GetDatasets(updates.Keys, inserts.Keys);

			var newFeatures = new List<Feature>();

			bool saved = GdbPersistenceUtils.ExecuteInTransaction(
				editContext =>
				{
					_msg.DebugFormat("Saving {0} updates and {1} inserts...", updates.Count,
					                 inserts.Count);

					GdbPersistenceUtils.UpdateTx(editContext, updates);

					newFeatures.AddRange(GdbPersistenceUtils.InsertTx(editContext, inserts));

					return true;
				},
				"Remove overlaps", datasets);

			ToolUtils.SelectNewFeatures(newFeatures, MapView.Active);

			var currentSelection = GetApplicableSelectedFeatures(MapView.Active).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_overlaps = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			if (_overlaps != null && _overlaps.Notifications.Count > 0)
			{
				_msg.Info(_overlaps.Notifications.Concatenate(Environment.NewLine));

				if (! _overlaps.HasOverlaps())
				{
					_msg.InfoFormat("Select one or more different features.");
				}
			}
			else if (_overlaps == null || ! _overlaps.HasOverlaps())
			{
				_msg.Info(
					"No overlap of other polygons with current selection found. Select one or more different features.");
			}

			if (_overlaps != null && _overlaps.HasOverlaps())
			{
				string msg = _overlaps.OverlapGeometries.Count == 1
					             ? "Select the overlap to subtract from the selection"
					             : "Select one or more overlaps to subtract from the selection. Draw a box to select overlaps completely within the box.";

				_msg.InfoFormat(LocalizableStrings.RemoveOverlapsTool_AfterSelection, msg);
			}
		}

		protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs e)
		{
			// NOTE: This method is not called when the selection is cleared by another command (e.g. by 'Clear Selection')
			//       Is there another way to get the global selection changed event? What if we need the selection changed in a button?

			//if (_shiftIsPressed) // always false -> toolkeyup is first. This method is apparently scheduled to run after key up
			//{
			//	return Task.FromResult(true);
			//}

			CancelableProgressor progressor = GetOverlapsCalculationProgressor();

			if (IsInSelectionPhase())
			{
				if (CanUseSelection(e.Selection))
				{
					var selectedFeatures = GetApplicableSelectedFeatures(e.Selection).ToList();

					AfterSelection(selectedFeatures, progressor);

					var sketch = GetCurrentSketchAsync().Result;

					SelectAndProcessDerivedGeometry(e.Selection, sketch, progressor);
				}
			}

			return Task.FromResult(true);
		}

		protected CancelableProgressor GetOverlapsCalculationProgressor()
		{
			var overlapsCalculationProgressorSource = new CancelableProgressorSource(
				"Calculating overlaps...", "cancelled", true);

			CancelableProgressor selectionProgressor =
				overlapsCalculationProgressorSource.Progressor;

			return selectionProgressor;
		}

		private Overlaps CalculateOverlaps(IList<Feature> selectedFeatures,
		                                   IList<Feature> overlappingFeatures,
		                                   CancelableProgressor progressor)
		{
			Overlaps overlaps;

			CancellationToken cancellationToken;

			if (progressor != null)
			{
				cancellationToken = progressor.CancellationToken;
			}
			else
			{
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationToken = cancellationTokenSource.Token;
			}

			if (MicroserviceClient != null)
			{
				overlaps =
					MicroserviceClient.CalculateOverlaps(selectedFeatures, overlappingFeatures,
					                                     cancellationToken);
			}
			else
			{
				throw new InvalidConfigurationException("Microservice has not been started.");
			}

			return overlaps;
		}

		private Overlaps SelectOverlaps(Overlaps overlaps, Geometry sketch)
		{
			if (overlaps == null)
			{
				return new Overlaps();
			}

			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
			                                          out bool singlePick);

			// in case of single pick the line has priority...
			Overlaps result = overlaps.SelectNewOverlaps(
				o => o.GeometryType == GeometryType.Polyline &&
				     ToolUtils.IsSelected(sketch, o, singlePick));

			// ... over the polygon
			if (! result.HasOverlaps() || ! singlePick)
			{
				result.AddGeometries(overlaps,
				                     g => g.GeometryType == GeometryType.Polygon &&
				                          ToolUtils.IsSelected(sketch, g, singlePick));
			}

			if (singlePick)
			{
				// Filter to the smallest overlap
				foreach (var overlap in result.OverlapGeometries)
				{
					IList<Geometry> geometries = overlap.Value;

					if (geometries.Count > 1)
					{
						Geometry smallest = GetSmallestGeometry(geometries);

						geometries.Clear();
						geometries.Add(smallest);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Returns a reference to the smallest (area for IArea objects, length for ICurve objects) 
		/// geometry of the given geometries. If several  geometries have the largest size, the first 
		/// in the list will be returned.
		/// </summary>
		/// <param name="geometries">The geometries which must all be of the same geometry type.</param>
		/// <returns></returns>
		public static T GetSmallestGeometry<T>([NotNull] IEnumerable<T> geometries)
			where T : Geometry
		{
			Geometry smallestPart = null;
			double smallestSize = double.PositiveInfinity;

			foreach (T candidate in geometries)
			{
				double candidateSize = GetGeometrySize(candidate);

				if (candidateSize < smallestSize)
				{
					smallestPart = candidate;
					smallestSize = candidateSize;
				}
			}

			return (T) smallestPart;
		}

		/// <summary>
		/// Returns a value that indicates the size of the specified geometry:
		/// - Multipatch, Polygon, Ring: 2D area
		/// - Polyline, Path, Segment: 2D length
		/// - Multipoint: Point count
		/// - Point: 0
		/// </summary>
		/// <param name="geometry"></param>
		/// <returns></returns>
		public static double GetGeometrySize([NotNull] Geometry geometry)
		{
			var multipart = geometry as Multipart;

			if (multipart != null && multipart.Area > 0)
			{
				return Math.Abs(multipart.Area);
			}

			if (multipart != null)
			{
				return multipart.Length;
			}

			return 0;
		}

		private static bool IsStoreRequired(Feature originalFeature, Geometry updatedGeometry,
		                                    HashSet<long> editableClassHandles)
		{
			if (! GdbPersistenceUtils.CanChange(originalFeature,
			                                    editableClassHandles, out string warning))
			{
				_msg.DebugFormat("{0}: {1}",
				                 GdbObjectUtils.ToString(originalFeature),
				                 warning);
				return false;
			}

			Geometry originalGeometry = originalFeature.GetShape();

			if (originalGeometry != null &&
			    originalGeometry.IsEqual(updatedGeometry))
			{
				_msg.DebugFormat("The geometry of feature {0} is unchanged. It will not be stored",
				                 GdbObjectUtils.ToString(originalFeature));

				return false;
			}

			return true;
		}

		#region Search target features

		[NotNull]
		private IList<Feature> GetOverlappingFeatures(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Dictionary<MapMember, List<long>> selection = ActiveMapView.Map.GetSelection();

			Envelope inExtent = ActiveMapView.Extent;

			// todo daro To tool Options? See ChangeGeometryAlongToolBase.SelectTargetsAsync() as well.
			const TargetFeatureSelection targetFeatureSelection =
				TargetFeatureSelection.VisibleSelectableFeatures;

			IEnumerable<FeatureClassSelection> featureClassSelections =
				MapUtils.FindFeatures(ActiveMapView, selection, targetFeatureSelection,
				                      CanOverlapLayer, inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    ! cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (var classSelection in featureClassSelections)
			{
				foundFeatures.AddRange(classSelection.Features);
			}

			// Remove the selected features from the set of overlapping features.
			// This is also important to make sure the geometries don't get mixed up / reset 
			// by inserting target vertices
			foundFeatures.RemoveAll(
				f => selectedFeatures.Any(s => GdbObjectUtils.IsSameFeature(f, s)));

			return foundFeatures;
		}

		private bool CanOverlapLayer(Layer layer)
		{
			var featureLayer = layer as FeatureLayer;

			List<string>
				ignoredClasses = new List<string>(); // RemoveOverlapsOptions.IgnoreFeatureClasses;

			return CanOverlapGeometryType(featureLayer) &&
			       (ignoredClasses == null || ! IgnoreLayer(layer, ignoredClasses));
		}

		private static bool CanOverlapGeometryType([CanBeNull] FeatureLayer featureLayer)
		{
			if (featureLayer?.GetFeatureClass() == null)
			{
				return false;
			}

			esriGeometryType shapeType = featureLayer.ShapeType;

			return shapeType == esriGeometryType.esriGeometryPolygon ||
			       shapeType == esriGeometryType.esriGeometryMultiPatch;
		}

		private static bool IgnoreLayer(Layer layer, IEnumerable<string> ignoredClasses)
		{
			FeatureClass featureClass = (layer as FeatureLayer)?.GetTable() as FeatureClass;

			if (featureClass == null)
			{
				return true;
			}

			string className = featureClass.GetName();

			foreach (string ignoredClass in ignoredClasses)
			{
				if (className.EndsWith(ignoredClass, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}
