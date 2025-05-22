using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.FillHole
{
	// TODO: Merge base class for both HoleTools -> HoleToolBase
	// TODO: Extent clipping support
	// TODO: Try understand duplicate execution (but only sometimes) of queued tasks
	public abstract class FillHoleToolBase : TwoPhaseEditToolBase
	{
		protected static readonly IMsg _msg = Msg.ForCurrentClass();

		protected HoleToolOptions _fillHoleToolOptions;

		private OverridableSettingsProvider<PartialHoleToolOptions> _settingsProvider;

		protected IList<Holes> _holes;

		private HoleFeedback _feedback;

		protected Envelope _calculationExtent;

		protected FillHoleToolBase()
		{
			GeomIsSimpleAsFeature = false;
		}

		protected abstract ICalculateHolesService MicroserviceClient { get; }

		protected virtual string OptionsFileName => "FillHoleToolOptions.xml";

		[CanBeNull]
		protected virtual string OptionsDockPaneID => null;

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		/// <summary>
		/// By default, the local configuration directory shall be in
		/// %APPDATA%\Roaming\<organization>\<product>\ToolDefaults.
		/// </summary>
		protected virtual string LocalConfigDir
			=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.FillHoleOverlay);
		}

		protected override SelectionCursors GetSecondPhaseCursors()
		{
			return SelectionCursors.CreateCrossCursors(Resources.FillHoleOverlay);
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_fillHoleToolOptions = InitializeOptions();

			_feedback = new HoleFeedback(_fillHoleToolOptions);

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

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_fillHoleToolOptions.LocalOptions);
			_feedback?.DisposeOverlays();
			_feedback = null;

			HideOptionsPane();
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.FillHoleTool_LogPromptForSelection);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			// TODO: Multipatches
			return geometryType == GeometryType.Polygon;
		}

		public HoleToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			// For the time being, we always reload the options because they could have been updated in ArcMap
			_settingsProvider =
				new OverridableSettingsProvider<PartialHoleToolOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialHoleToolOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result =
				new HoleToolOptions(centralConfiguration, localConfiguration);

			result.PropertyChanged -= _fillHoleToolOptions_PropertyChanged;
			result.PropertyChanged += _fillHoleToolOptions_PropertyChanged;

			_msg.DebugStopTiming(watch, "Fill Hole Tool Options validated / initialized");

			string optionsMessage = result.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private void _fillHoleToolOptions_PropertyChanged(object sender,
		                                                  PropertyChangedEventArgs args)
		{
			try
			{
				QueuedTaskUtils.Run(() =>
				{
					var selectedFeatures =
						GetApplicableSelectedFeatures(ActiveMapView).ToList();

					using var source = GetProgressorSource();
					var progressor = source?.Progressor;

					CalculateDerivedGeometries(selectedFeatures, progressor);
					LogDerivedGeometriesCalculated(progressor);
				});
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating fillable holes : {e.Message}", e);
			}
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			_calculationExtent = ActiveMapView.Extent;

			_msg.DebugFormat("Calculating fillable holes for {0} selected features",
			                 selectedFeatures.Count);

			CancellationToken cancellationToken;

			if (progressor != null)
			{
				cancellationToken = progressor.CancellationToken;
			}
			else
			{
				// TODO Why not CancellationToken.None?
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationToken = cancellationTokenSource.Token;
			}

			if (CalculateHoles(selectedFeatures, progressor, cancellationToken))
			{
				return;
			}

			////Note: This is a intermediate solution to get the right amount of calculated holes,
			////when changing the options, but sometimes it leads to double logging of the calculated holes
			//LogDerivedGeometriesCalculated(progressor);

			_feedback.Update(_holes);

			_feedback.UpdateExtent(_calculationExtent);
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

			IList<Polygon> holesToFill = SelectHoles(_holes, sketch);

			_msg.DebugFormat("Selected {0} out of {1} holes to fill",
			                 holesToFill.Count, _holes.Count);

			if (holesToFill.Count == 0)
			{
				return false;
			}

			MapView activeMapView = MapView.Active;

			FeatureClass currentTargetClass = GetCurrentTargetClass(out Subtype targetSubtype);

			var datasets = new List<Dataset> { currentTargetClass };

			IList<Feature> newFeatures = new List<Feature>();

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             _msg.DebugFormat("Inserting {0} new features...",
					                              holesToFill.Count);

					             newFeatures = GdbPersistenceUtils.InsertTx(
						             editContext, currentTargetClass, targetSubtype,
						             holesToFill, GetFieldValue);

					             _msg.InfoFormat("Successfully created {0} new {1} feature(s).",
					                             newFeatures.Count, currentTargetClass.GetName());

					             return true;
				             },
				             "Fill hole(s)", datasets);

			foreach (IDisplayTable displayTable in MapUtils
				         .GetFeatureLayersForSelection<FeatureLayer>(
					         MapView.Active.Map, currentTargetClass))
			{
				if (displayTable is FeatureLayer featureLayer)
				{
					var objectIds = newFeatures.Select(f => f.GetObjectID()).ToList();

					SelectionUtils.SelectRows(featureLayer, SelectionCombinationMethod.Add,
					                          objectIds);
				}
			}

			var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected virtual FeatureClass GetCurrentTargetClass(out Subtype subtype)
		{
			return ToolUtils.GetCurrentTargetFeatureClass(true, out subtype);
		}

		protected virtual object GetFieldValue([NotNull] Field field,
		                                       [NotNull] FeatureClassDefinition featureClassDef,
		                                       [CanBeNull] Subtype subtype)
		{
			// If there is an active template, use it:
			if (GdbPersistenceUtils.TryGetFieldValueFromTemplate(
				    field.Name, EditingTemplate.Current, out object result))
			{
				return result;
			}

			// Otherwise: Geodatabase default value:
			return field.GetDefaultValue(subtype);
		}

		protected override void ResetDerivedGeometries()
		{
			_holes = null;

			_calculationExtent = null;

			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			int holeCount = _holes?.Sum(h => h.HoleCount) ?? 0;

			if (holeCount == 0)
			{
				_msg.InfoFormat(
					"The current selection does not contain a hole or gap. Select one or more different features.");
			}
			else
			{
				string holeCountMsg =
					holeCount == 1
						? "Found one hole{0}. "
						: $"Found {holeCount} holes{{0}}. ";

				holeCountMsg = string.Format(holeCountMsg,
				                             _fillHoleToolOptions.LimitPreviewToExtent
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

		#region Tool Options Dockpane

		[CanBeNull]
		private DockPaneFillHoleViewModelBase GetFillHoleViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneFillHoleViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetFillHoleViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _fillHoleToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetFillHoleViewModel();

			viewModel?.Hide();
		}

		#endregion
	}
}
