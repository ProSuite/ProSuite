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
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
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

		protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		{
			IEnumerable<FeatureClass> featureClasses =
				selectedFeatures.Select(f => f.GetTable()).Distinct();

			return featureClasses.Any(fc =>
			{
				GeometryType geometryType = fc.GetDefinition().GetShapeType();

				return CanSelectGeometryType(geometryType);
			});
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
			selectedFeatures = GetApplicableSelectedFeatures(selectedFeatures).ToList();

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
			bool insertVerticesInTarget = false;
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

			foreach (var resultPerFeature in result.ResultsByFeature)
			{
				updates.Add(resultPerFeature.OriginalFeature, resultPerFeature.UpdatedGeometry);

				if (resultPerFeature.InsertGeometries.Count > 0)
				{
					inserts.Add(resultPerFeature.OriginalFeature,
					            resultPerFeature.InsertGeometries);
				}
			}

			bool saved = GdbPersistenceUtils.SaveInOperation("Remove overlaps", updates, inserts);

			var currentSelection = SelectionUtils.GetSelectedFeatures(MapView.Active).ToList();

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
				var selectedFeatures = MapUtils.GetFeatures(e.Selection).ToList();

				if (CanUseSelection(selectedFeatures))
				{
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
			Overlaps overlaps = null;

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

		private static Overlaps SelectOverlaps(Overlaps overlaps, Geometry sketch)
		{
			if (overlaps == null)
			{
				return new Overlaps();
			}

			int selectionTolerancePixels = 3;

			bool singlePick = ToolUtils.IsSingleClickSketch(sketch);

			if (singlePick)
			{
				sketch = ToolUtils.GetSinglePickSelectionArea(sketch, selectionTolerancePixels);
			}

			Overlaps result = overlaps.SelectNewOverlaps(
				o => o.GeometryType == GeometryType.Polyline &&
				     IsOverlapSelected(sketch, o, singlePick));

			// in case of single pick the line has priority
			if (! result.HasOverlaps() || ! singlePick)
			{
				result.AddGeometries(overlaps,
				                     g => g.GeometryType == GeometryType.Polygon &&
				                          IsOverlapSelected(sketch, g, singlePick));
			}

			return result;
		}

		private static bool IsOverlapSelected(Geometry sketch, Geometry overlapGeometry,
		                                      bool singlePick)
		{
			if (GeometryUtils.Disjoint(sketch, overlapGeometry))
			{
				return false;
			}

			if (singlePick)
			{
				// Any intersection is enough:
				return true;
			}

			return GeometryUtils.Contains(sketch, overlapGeometry);
		}

		#region Search target features

		[NotNull]
		private IList<Feature> GetOverlappingFeatures(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Dictionary<MapMember, List<long>> selection = ActiveMapView.Map.GetSelection();

			Envelope inExtent = ActiveMapView.Extent;

			TargetFeatureSelection targetFeatureSelection = TargetFeatureSelection.VisibleFeatures;

			IEnumerable<KeyValuePair<FeatureClass, List<Feature>>> foundOidsByClass =
				MapUtils.FindFeatures(ActiveMapView, selection, targetFeatureSelection,
				                      CanOverlapLayer, inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    ! cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (var keyValuePair in foundOidsByClass)
			{
				foundFeatures.AddRange(keyValuePair.Value);
			}

			// Remove the selected features from the set of overlapping features.
			// This is also important to make sure the geometries don't get mixed up / reset 
			// by inserting target vertices
			foundFeatures.RemoveAll(selectedFeatures.Contains);

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