using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.FillHole;

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

			SelectionCursor = ToolUtils.GetCursor(Resources.RemoveHoleToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.RemoveHoleToolCursorShift);
			SecondPhaseCursor = ToolUtils.GetCursor(Resources.RemoveHoleToolCursorProcess);
		}

		protected FillHoleOptions RemoveHoleOptions { get; } = new FillHoleOptions();

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override void OnUpdate()
		{
			Enabled = MicroserviceClient != null;

			Tooltip = Enabled
				          ? "Remove holes or boundary loops from polygon features"
				          : "Microservice not found / not started.";
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new HoleFeedback();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(
				"Select one or more polygon features which contain the hole(s) to be removed.");
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

		protected override bool SelectAndProcessDerivedGeometry(
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
				selection, activeMapView.Map.SpatialReference).ToList();

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

			bool saved = GdbPersistenceUtils.ExecuteInTransaction(
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
			                                 ProtobufConversionUtils.GetUniqueClassId(f) ==
			                                 classId);
		}

		#endregion

		protected abstract bool CalculateHoles(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor,
		                                       CancellationToken cancellationToken);

		protected abstract IList<Holes> SelectHoles([CanBeNull] IList<Holes> holes,
		                                            [NotNull] Geometry sketch);

		protected abstract CancelableProgressor GetHoleCalculationProgressor();
	}
}
