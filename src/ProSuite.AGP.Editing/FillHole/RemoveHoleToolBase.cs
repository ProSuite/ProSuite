using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.FillHole
{
	public abstract class RemoveHoleToolBase : TwoPhaseEditToolBase
	{
		protected static readonly IMsg _msg = Msg.ForCurrentClass();

		protected IList<Holes> _holes;

		private HoleFeedback _feedback;

		protected Envelope _calculationPerimeter;

		protected RemoveHoleToolBase()
		{
			GeomIsSimpleAsFeature = false;
		}

		protected FillHoleOptions RemoveHoleOptions { get; } = new FillHoleOptions();

		protected abstract ICalculateHolesService MicroserviceClient { get; }

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_feedback = new HoleFeedback();

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override CancelableProgressorSource GetProgressorSource()
		{
			// Disable the progressor because removing holes is typically fast,
			// and the users potentially want to continue working already.
			return null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.RemoveHoleTool_LogPromptForSelection);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			// TODO: Multipatches
			return geometryType == GeometryType.Polygon;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			_msg.DebugFormat("Calculating removable holes for {0} selected features",
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

		protected override async Task<bool> SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_holes);

			IList<Holes> featuresWithHoles = SelectHoles(_holes, sketch);

			_msg.DebugFormat("Selected {0} out of {1} hole features to remove holes",
			                 featuresWithHoles.Count, _holes.Count);

			if (featuresWithHoles.Count == 0)
			{
				return false;
			}

			MapView activeMapView = MapView.Active;

			var selectedFeatures = MapUtils.GetFeatures(
				selection, true, activeMapView.Map.SpatialReference).ToList();

			var updates = new Dictionary<Feature, Geometry>();

			foreach (Holes featuresWithHole in featuresWithHoles)
			{
				GdbObjectReference featureRef =
					Assert.NotNull(featuresWithHole.FeatureReference).Value;

				Feature feature = GetOriginalFeature(featureRef, selectedFeatures);
				//var feature = selectedFeatures.FirstOrDefault(f => featureRef.References(f));

				if (feature != null)
				{
					List<Geometry> shapeAndHoles = new List<Geometry> { feature.GetShape() };
					shapeAndHoles.AddRange(featuresWithHole.HoleGeometries);

					Geometry resultGeometry = GeometryUtils.Union(shapeAndHoles);

					updates.Add(feature, resultGeometry);
				}
			}

			IEnumerable<Dataset> datasets =
				GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys);

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             _msg.DebugFormat("Saving {0} updates...", updates.Count);

					             GdbPersistenceUtils.UpdateTx(editContext, updates);

					             return true;
				             },
				             "Remove hole(s)", datasets);

			if (progressor == null || ! progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.InfoFormat("Successfully removed {0} hole(s) from {1} feature(s).",
				                featuresWithHoles.Sum(h => h.HoleCount), featuresWithHoles.Count);
			}

			CalculateDerivedGeometries(selectedFeatures, progressor);

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
					"The current selection neither contain holes nor boundary loops. Select one or more different features.");
			}
			else
			{
				string holeCountMsg =
					holeCount == 1
						? "Found one hole{0}. "
						: $"Found {holeCount} holes{{0}}. ";

				holeCountMsg = string.Format(holeCountMsg,
				                             RemoveHoleOptions.LimitPreviewToExtent
					                             ? " in current extent (shown in green)"
					                             : string.Empty);

				string clickHoleMsg =
					"Click on a hole to remove. Holes selected by dragging a box must be completely within the area.";

				// TODO: Implement polygon sketch
				//"Holes selected by dragging a box or by drawing a polygon (while holding [P]) must be completely within the area.";

				_msg.InfoFormat("{0}{1}" +
				                Environment.NewLine +
				                "Press [ESC] to select different features.",
				                holeCountMsg, clickHoleMsg);
			}
		}

		#region Code duplicates

		// TODO: Consider upgrading ObjectClassId to long in microservice
		//       and potentially add it to IReadOnly
		//       and somehow add support for Shapefiles (OID service? Hash of full path?)
		private static Feature GetOriginalFeature(GdbObjectReference featureRef,
		                                          List<Feature> updateFeatures)
		{
			// consider using anything unique as an identifier, e.g. a GUID
			long classId = featureRef.ClassId;
			long objectId = featureRef.ObjectId;

			return GetOriginalFeature(objectId, classId, updateFeatures);
		}

		private static Feature GetOriginalFeature(long objectId, long classId,
		                                          List<Feature> updateFeatures)
		{
			return updateFeatures.First(f => f.GetObjectID() == objectId &&
			                                 GeometryProcessingUtils.GetUniqueClassId(f) ==
			                                 classId);
		}

		#endregion

		protected abstract bool CalculateHoles(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor,
		                                       CancellationToken cancellationToken);

		protected abstract IList<Holes> SelectHoles([CanBeNull] IList<Holes> holes,
		                                            [NotNull] Geometry sketch);

		protected override Cursor GetSelectionCursor()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay, null);
		}

		protected override Cursor GetSelectionCursorShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay,
			                              Resources.Lasso);
		}

		protected override Cursor GetSelectionCursorLassoShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay,
			                              Resources.Lasso,
			                              Resources.Shift);
		}

		protected override Cursor GetSelectionCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay,
			                              Resources.Polygon);
		}

		protected override Cursor GetSelectionCursorPolygonShift()
		{
			return ToolUtils.CreateCursor(Resources.Arrow,
			                              Resources.RemoveHoleOverlay,
			                              Resources.Polygon,
			                              Resources.Shift);
		}

		#region second phase cursors

		protected override Cursor GetSecondPhaseCursor()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.RemoveHoleOverlay, 10, 10);
		}

		protected override Cursor GetSecondPhaseCursorLasso()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.RemoveHoleOverlay,
			                              Resources.Lasso, null, 10, 10);
		}

		protected override Cursor GetSecondPhaseCursorPolygon()
		{
			return ToolUtils.CreateCursor(Resources.Cross, Resources.RemoveHoleOverlay,
			                              Resources.Polygon, null, 10, 10);
		}

		#endregion
	}
}
