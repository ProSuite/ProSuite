using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.FillHole;

namespace ProSuite.AGP.Editing.FillHole
{
	// TODO: Merge base class for both HoleTools -> HoleToolBase
	// TODO: Extent clipping support
	// TODO: Try understand duplicate execution (but only sometimes) of queued tasks
	public abstract class FillHoleToolBase : TwoPhaseEditToolBase
	{
		protected static readonly IMsg _msg = Msg.ForCurrentClass();

		protected IList<Holes> _holes;

		private HoleFeedback _feedback;

		protected Envelope _calculationPerimeter;

		protected FillHoleToolBase()
		{
			GeomIsSimpleAsFeature = false;

			SelectionCursor = ToolUtils.GetCursor(Resources.FillHoleToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.FillHoleToolCursorShift);
			SecondPhaseCursor = ToolUtils.GetCursor(Resources.FillHoleToolCursorProcess);
		}

		protected FillHoleOptions FillHoleOptions { get; } = new FillHoleOptions();

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override void OnUpdate()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new HoleFeedback();

			if (EditingTemplate.Current == null)
			{
				_msg.Warn(
					"No polygon feature template has been selected. Please select a polygon template " +
					"in the 'Create Feature' Pane. This will determine the type of new features created to fill holes.");
			}
			else if (EditingTemplate.Current.Layer is BasicFeatureLayer fl &&
			         fl.ShapeType != esriGeometryType.esriGeometryPolygon)
			{
				_msg.WarnFormat(
					"The current feature template ({0}) is not a polygon feature type. Please select a polygon " +
					"template in the 'Create Feature' Pane. This will determine the type of new features created to fill holes.",
					EditingTemplate.Current.Name);
			}
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(
				"Select a polygon that contains a hole or several polygons with a gap in between.");
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			// TODO: Multipatches
			return geometryType == GeometryType.Polygon;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			_msg.DebugFormat("Calculating fillable holes for {0} selected features",
			                 selectedFeatures.Count);

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

			if (CalculateHoles(selectedFeatures, progressor, cancellationToken))
			{
				return;
			}

			_feedback.Update(_holes);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _holes?.Any(h => h.HasHoles()) == true;
		}

		protected override bool SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_holes);

			IList<Polygon> holesToFill = SelectHoles(_holes, sketch);

			_msg.DebugFormat("Selected {0} out of {1} holes to fill",
			                 holesToFill.Count, _holes.Count);

			if (holesToFill.Count == 0)
			{
				return false;
			}

			MapView activeMapView = MapView.Active;

			EditingTemplate editTemplate = EditingTemplate.Current;

			FeatureClass currentTargetClass = ToolUtils.GetCurrentTargetFeatureClass(editTemplate);

			if (currentTargetClass == null)
			{
				throw new Exception("No valid template selected");
			}

			IEnumerable<Dataset> datasets = new List<Dataset> { currentTargetClass };

			IList<Feature> newFeatures = null;

			bool saved = GdbPersistenceUtils.ExecuteInTransaction(
				editContext =>
				{
					_msg.DebugFormat("Inserting {0} new features...", holesToFill.Count);

					newFeatures = GdbPersistenceUtils.InsertTx(
						editContext, currentTargetClass, holesToFill.Cast<Geometry>().ToList(),
						editTemplate.Inspector);

					_msg.InfoFormat("Successfully created {0} new {1} feature(s).",
					                newFeatures.Count, editTemplate.Name);

					return true;
				},
				"Fill hole(s)", datasets);

			var targetLayer = (BasicFeatureLayer) editTemplate.Layer;
			SelectionUtils.SelectRows(targetLayer, SelectionCombinationMethod.Add, newFeatures);

			var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_holes = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			int holeCount = _holes?.Sum(h => h.HoleCount) ?? 0;

			if (holeCount == 0)
			{
				_msg.InfoFormat(
					"Select a polygon that contains a hole or several polygons with a gap in between.");
			}
			else
			{
				string holeCountMsg =
					holeCount == 1
						? "Found one hole{0}. "
						: $"Found {holeCount} holes{{0}}. ";

				holeCountMsg = string.Format(holeCountMsg,
				                             FillHoleOptions.LimitPreviewToExtent
					                             ? " in current extent (shown in green)"
					                             : string.Empty);

				EditingTemplate editTemplate = EditingTemplate.Current;

				string templateName = editTemplate?.Name ?? "<no template selected>";

				string clickHoleMsg =
					$"Click on a hole to fill with a new '{templateName}' feature. " +
					"Holes selected by dragging a box must be completely within the area.";

				// TODO: Implement polygon sketch
				//"Holes selected by dragging a box or by drawing a polygon (while holding [P]) must be completely within the area.";

				_msg.InfoFormat("{0}{1}" +
				                Environment.NewLine +
				                "Press [ESC] to select different features.",
				                holeCountMsg, clickHoleMsg);
			}
		}

		protected abstract bool CalculateHoles(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor,
		                                       CancellationToken cancellationToken);

		protected static CancelableProgressor GetHoleCalculationProgressor()
		{
			var overlapsCalculationProgressorSource = new CancelableProgressorSource(
				"Calculating holes...", "cancelled", true);

			CancelableProgressor selectionProgressor =
				overlapsCalculationProgressorSource.Progressor;

			return selectionProgressor;
		}

		protected abstract IList<Polygon> SelectHoles([CanBeNull] IList<Holes> holes,
		                                              [NotNull] Geometry sketch);

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

		private static List<Polygon> GetSourcePolygons(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] Envelope clipEnvelope)
		{
			var selectedShapes = new List<Polygon>(selectedFeatures.Count);
			var shapesToClip = new List<Polygon>(selectedFeatures.Count);

			foreach (Feature selectedFeature in selectedFeatures)
			{
				var selectedPoly = (Polygon) selectedFeature.GetShape();

				if (clipEnvelope == null)
				{
					selectedShapes.Add(selectedPoly);
					continue;
				}

				throw new NotImplementedException();

				if (GeometryUtils.Disjoint(selectedPoly, clipEnvelope))
				{
					continue;
				}

				if (GeometryUtils.Contains(clipEnvelope, selectedPoly))
				{
					selectedShapes.Add(selectedPoly);
				}
				else
				{
					shapesToClip.Add(selectedPoly);
					// TODO:

					// Expand the clip envelope to ensure that the partially visible holes also appear as holes
					//_calculationPerimeter = HoleUtils.ExpandExtentToContainRelevantRings(
					//	clipEnvelope, Assert.NotNull(_calculationPerimeter), selectedPoly);
				}
			}

			foreach (Polygon polygonToClip in shapesToClip)
			{
				selectedShapes.Add(GeometryUtils.GetClippedPolygon(polygonToClip,
					                   Assert.NotNull(clipEnvelope)));
				Marshal.ReleaseComObject(polygonToClip);
			}

			return selectedShapes;
		}
	}
}
