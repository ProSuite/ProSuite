using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public abstract class MergeFeaturesToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private MergeToolOptions _mergeToolOptions;
		private OverridableSettingsProvider<PartialMergeOptions> _settingsProvider;

		protected MergeFeaturesToolBase()
		{
			IsSketchTool = true;

			GeomIsSimpleAsFeature = false;
		}

		protected virtual string OptionsFileName => "MergeToolOptions.xml";

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

		//private readonly Cursor _step1Cursor;
		//private readonly Cursor _step2Cursor;

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.MergeFeaturesOverlay);
		}

		// First selected feature
		private Feature _firstFeature;

		private const Keys _immediateMergeKey = Keys.Enter;

		//#region Constructors

		///// <summary>
		///// Initializes a new instance of the <see cref="MergeFeaturesToolBase"/> class.
		///// </summary>
		///// <param name="name">The command name.</param>
		///// <param name="category">The category.</param>
		//protected MergeFeaturesToolBase(string name, string category)
		//	: base(name, category)
		//{
		//	Caption = LocalizableStrings.MergeFeaturesTool_Caption;

		//	TooltipBody =
		//		"Merge two features with the same geometry type.<LineBreak/>" +
		//		"<LineBreak/><Bold>1.</Bold> Select the first feature" +
		//		"<LineBreak/><Bold>2.</Bold> Select the second feature" +
		//		"<LineBreak/>" +
		//		"<LineBreak/><Bold>ESC:</Bold> Clear selection" +
		//		"<LineBreak/><Bold>O:  </Bold> Additional options";

		//	SetBitmap(Resources.MergeFeaturesTool);

		//	_step1Cursor = GetCursor(Resources.MergeFeaturesToolCursor_Step1);
		//	_step2Cursor = GetCursor(Resources.MergeFeaturesToolCursor_Step2);
		//}

		//#endregion

		#region ToolBase and OneClickToolBase overrides

		/// <summary>
		/// An optional merge condition evaluator that currently only results in warnings
		/// if some condition is violated.
		/// </summary>
		public IMergeConditionEvaluator MergeConditionEvaluator { get; protected set; }

		//protected override bool RequiresEditSession => true;

		protected bool AllowMultiSelection =>
			_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject;

		protected bool AllowSelectByPolygon =>
			_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject;

		//protected override bool SelectOnlyEditFeatures => true;

		protected bool IgnoreSelectionOutsideVisibleExtents => true;

		//protected override int CursorCore
		//{
		//	get
		//	{
		//		if (_envelopeDrawer != null)
		//		{
		//			return _envelopeDrawer.CursorHandle;
		//		}

		//		return _firstFeature == null ? Step1CursorCore : Step2CursorCore;
		//	}
		//}

		//protected override void OnCreateCore()
		//{
		//	base.OnCreateCore();

		//	_editEvents = (IEditEvents_Event)Editor;
		//	_editEvents2 = (IEditEvents2_Event)Editor;
		//}

		protected void AddControlledKeysCore(IList<Keys> controlledKeys)
		{
			//controlledKeys.Add(_optionsFormKey);
			controlledKeys.Add(_immediateMergeKey);
		}

		//TODO: Add when MergeOverlay is defined/implemented in the resources
		//protected override SelectionCursors GetSelectionCursors()
		//{
		//	return SelectionCursors.CreateArrowCursors(Resources.MergeOverlay);
		//}

		protected override async Task HandleEscapeAsync()
		{
			//_envelopeDrawer = null;

			_firstFeature = null;

			// also unselect the feature to communicate the current state
			Task task = QueuedTask.Run(async () =>
			{
				ClearSelection();

				LogPromptForSelection();
			});
			await ViewUtils.TryAsync(task, _msg);
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_mergeToolOptions = InitializeOptions();

			//_feedback = new GeneralizeFeedback();

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_mergeToolOptions.LocalOptions);

			_firstFeature = null;

			HideOptionsPane();
		}

		protected override async Task AfterSelectionAsync([NotNull] IList<Feature> selectedFeatures,
		                                                  [CanBeNull]
		                                                  CancelableProgressor progressor)
		{
			_firstFeature = selectedFeatures[0];
			//TODO: brauchts das?
			//return base.AfterSelectionAsync();
		}

		protected override Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			bool isInSelectionPhase = _firstFeature == null || shiftDown;

			return Task.FromResult(isInSelectionPhase);
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info(LocalizableStrings.MergeFeaturesTool_LogUsingCurrentSelection);
		}

		protected override void LogPromptForSelection()
		{
			string message;

			string survivorText =
				_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject
					? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
					: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;

			int selectedCount = ActiveMapView.Map.SelectionCount;

			if (selectedCount > 1)
			{
				message = Environment.NewLine +
				          "Press Enter to merge all selected features. The largest feature will survive after the merge." +
				          Environment.NewLine +
				          $"Alternatively, select a new first feature to merge{survivorText}";
			}
			else
			{
				message = string.Format(
					LocalizableStrings.MergeFeaturesTool_LogPromptForSelection,
					survivorText);
			}

			_msg.Info(message);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry, CancelableProgressor progressor)
		{
			bool isInFirstPhase = await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown());

			Assert.False(isInFirstPhase, "Unexpected tool phase");

			//if (isInFirstPhase)
			//{
			//	 return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			//}
			//else if (!await CanStillUseSelection(sketchGeometry, progressor))
			//{
			//	_msg.InfoFormat(
			//		"The current selection cannot be used. Re-selecting the first feature...");
			//	return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			//}
			//else
			{
				await PickSecondFeatureAndMerge(sketchGeometry, progressor);
			}

			//_envelopeDrawer?.Reset();
			//_envelopeDrawer = null;

			return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
		}

		protected override async Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChangedCoreAsync");

			if (ActiveMapView == null)
			{
				return false;
			}

			if (! CanUseSelection(ActiveMapView))
			{
				_firstFeature = null;
				// TODO: Test if this is necessary
				//await StartSelectionPhaseAsync();
			}
			else
			{
				Dictionary<MapMember, List<long>> selectionByLayer =
					SelectionUtils.GetSelection(ActiveMapView.Map);

				List<Feature> selection =
					GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection)
						.ToList();

				_firstFeature = selection[0];
				//await StartSketchPhaseAsync();
			}

			// TODO: virtual RefreshFeedbackCoreAsync(), override in AdvancedReshape

			return true;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Multipatch ||
			       geometryType == GeometryType.Multipoint;
		}

		protected bool CanSelectFeatureType(esriFeatureType featureType)
		{
			switch (featureType)
			{
				case esriFeatureType.esriFTSimple:
				case esriFeatureType.esriFTSimpleEdge:
				case esriFeatureType.esriFTComplexEdge:
					return true;

				case esriFeatureType.esriFTSimpleJunction:
				case esriFeatureType.esriFTComplexJunction:
				case esriFeatureType.esriFTAnnotation:
				case esriFeatureType.esriFTCoverageAnnotation:
				case esriFeatureType.esriFTDimension:
				case esriFeatureType.esriFTRasterCatalogItem:
					return false;

				default:
					_msg.DebugFormat("Unknown feature type: {0}", featureType);
					return false;
			}
		}

		//protected override void KeyReleasedCore(Keys key)
		//{
		//	//if (key == _optionsFormKey)
		//	//{
		//	//	_msg.Info(_settingsProvider.GetXmlLocationLogMessage());

		//	//	var formOptions = (MergeToolOptions)MergeOptions.Clone();

		//	//	using (var form = new MergeToolOptionsForm(formOptions, Caption))
		//	//	{
		//	//		if (DialogResult.OK == UIEnvironment.ShowDialog(form))
		//	//		{
		//	//			MergeOptions = formOptions;

		//	//			_settingsProvider.StoreLocalConfiguration(MergeOptions.LocalOptions);
		//	//		}
		//	//	}
		//	//}
		//	//ToDo muss noch migriert

		//	if (key == _immediateMergeKey)
		//	{
		//		IList<Feature> selectedFeatures = GetSelectedEditFeatures();

		//		MergerBase merger = GetMerger();

		//		if (!merger.CanMerge(selectedFeatures))
		//		{
		//			return;
		//		}

		//		Geometry largestGeometry =
		//			GeometryUtils.GetLargestGeometry(selectedFeatures.Select(f => f.Shape));

		//		Feature largestFeature =
		//			selectedFeatures.FirstOrDefault(f => f.Shape == largestGeometry);

		//		Assert.NotNull(largestFeature, "No largest feature identified.");

		//		Feature survivingFeature = merger.MergeFeatures(selectedFeatures, largestFeature);

		//		if (survivingFeature != null)
		//		{
		//			SelectResultAndLogNextStep(survivingFeature);
		//		}
		//	}
		//}

		//protected override void OnKeyDownCore(int code, int shift)
		//{
		//	if (_envelopeDrawer != null)
		//	{
		//		_envelopeDrawer.OnKeyDown(code, shift);
		//	}

		//	base.OnKeyDownCore(code, shift);
		//}

		//protected override bool DeactivateCore()
		//{
		//	UnwireEvents();

		//	bool deactivated = base.DeactivateCore();

		//	if (deactivated)
		//	{
		//		_firstFeature = null;
		//	}

		//	return deactivated;
		//}

		//protected override void OnMouseMoveCore(int button, int shift, int x, int y)
		//{
		//	if (_firstFeature == null || shift == 1)
		//	{
		//		base.OnMouseMoveCore(button, shift, x, y);
		//	}
		//	else
		//	{
		//		_envelopeDrawer?.OnMouseMove(button, shift, x, y);
		//	}
		//}

		//protected override void OnMouseDownCore(int button, int shift, int x, int y)
		//{
		//	if (_firstFeature == null || shift == 1)
		//	{
		//		base.OnMouseDownCore(button, shift, x, y);
		//	}
		//	else if (_envelopeDrawer == null)
		//	{
		//		_envelopeDrawer = new EnvelopeDrawer(MxApplication);
		//		_envelopeDrawer.ShowMessages = false;
		//		_envelopeDrawer.OnMouseDown(button, shift, x, y);
		//	}
		//}

		//protected override void OnMouseUpCore(int button, int shift, int x, int y)
		//{
		//	if (_firstFeature == null || shift == 1)
		//	{
		//		base.OnMouseUpCore(button, shift, x, y);
		//	}
		//	else if (!CanStillUseSelection(_firstFeature))
		//	{
		//		_msg.InfoFormat(
		//			"The current selection cannot be used. Re-selecting the first feature...");
		//		base.OnMouseUpCore(button, shift, x, y);
		//	}
		//	else
		//	{
		//		using (new WaitCursor())
		//		{
		//			PickSecondFeatureAndMerge(button, shift, x, y);
		//		}
		//	}

		//	_envelopeDrawer?.Reset();
		//	_envelopeDrawer = null;
		//}

		//protected override bool OnContextMenuCore(int x, int y)
		//{
		//	ICommandBar bar = CreateContextMenu(Assert.NotNull(Application));

		//	Point mousePosition = Control.MousePosition;

		//	bar.Popup(mousePosition.X, mousePosition.Y);

		//	return true;
		//}

		#endregion

		#region Non-public members

		[NotNull]
		protected abstract MergerBase GetMerger();

		[CanBeNull]
		protected virtual Type MergeWithSelectedAsPrimaryCmd()
		{
			return null;
		}

		[CanBeNull]
		protected virtual Type MergeWithLargestAsPrimaryCmd()
		{
			return null;
		}

		//[NotNull]
		//private ICommandBar CreateContextMenu([NotNull] IApplication application)
		//{
		//	Assert.ArgumentNotNull(application, nameof(application));

		//	ICommandBars bars = application.Document.CommandBars;
		//	ICommandBar bar = bars.Create("MergeFeaturesContextMenu",
		//								  esriCmdBarType.esriCmdBarTypeShortcutMenu);

		//	object optional = Type.Missing;

		//	Type largestPartCmdType = MergeWithLargestAsPrimaryCmd();

		//	if (largestPartCmdType != null)
		//	{
		//		bar.Add(UIDUtils.CreateUID(largestPartCmdType), ref optional);
		//	}

		//	Type selectPartCmdType = MergeWithSelectedAsPrimaryCmd();

		//	if (selectPartCmdType != null)
		//	{
		//		bar.Add(UIDUtils.CreateUID(selectPartCmdType), ref optional);
		//	}

		//	ICommandItem item = bar.Add(
		//		UIDUtils.CreateUID(KnownMxCommands.ZoomToSelected),
		//		ref optional);
		//	item.Group = true;

		//	bar.Add(UIDUtils.CreateUID(KnownMxCommands.ZoomToPreviousExtent),
		//			ref optional);
		//	bar.Add(UIDUtils.CreateUID(KnownMxCommands.ZoomToNextExtent), ref optional);

		//	bar.Add(UIDUtils.CreateUID(KnownMxCommands.ZoomInFixed), ref optional);
		//	bar.Add(UIDUtils.CreateUID(KnownMxCommands.ZoomOutFixed), ref optional);
		//	bar.Add(UIDUtils.CreateUID(KnownMxCommands.ClearSelection), ref optional);

		//	return bar;
		//}

		//protected virtual int Step1CursorCore => _step1Cursor.Handle.ToInt32();

		//protected virtual int Step2CursorCore => _step2Cursor.Handle.ToInt32();

		protected MergeToolOptions MergeOptions => _mergeToolOptions;

		//protected string LocalConfigDir =>
		//	EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
		//		AppDataFolder.Roaming);

		//[CanBeNull]
		//protected virtual string CentralConfigDir => null;

		//protected virtual string OptionsFileName => "MergeToolOptions.xml";

		protected virtual MergeOperationSurvivor MergeOperationSurvivor =>
			_mergeToolOptions.MergeSurvivor;

		//protected virtual bool AssumeStoredGeometryIsSimple =>
		//	WorkspaceUtils.IsSDEGeodatabase(Editor.EditWorkspace);

		private MergeToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			_settingsProvider =
				new OverridableSettingsProvider<PartialMergeOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialMergeOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result =
				new MergeToolOptions(centralConfiguration, localConfiguration);

			result.PropertyChanged -= OptionsPropertyChanged;
			result.PropertyChanged += OptionsPropertyChanged;

			_msg.DebugStopTiming(watch, "Merge Features Options validated / initialized");

			string optionsMessage = result.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			try
			{
				QueuedTaskUtils.Run(() => ProcessSelectionAsync());
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating merge features : {e.Message}", e);
			}
		}

		#region Tool Options DockPane

		[CanBeNull]
		private DockPaneMergeFeaturesViewModelBase GetDockPaneMergeFeaturesViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneMergeFeaturesViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetDockPaneMergeFeaturesViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _mergeToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetDockPaneMergeFeaturesViewModel();
			viewModel?.Hide();
		}

		#endregion

		private async Task<bool> CanStillUseSelection(Geometry sketchGeometry,
		                                              CancelableProgressor progressor)
		{
			return await QueuedTask.Run(() =>
			{
				return true;
				//List<Feature> selectedFeatures =
				//	GetApplicableSelectedFeatures(ActiveMapView).ToList();
				////TODO: Kratzt mich diese Unterwellelung?
				//if (selectedFeatures == null || selectedFeatures.Count != 1)
				//{
				//	return false;
				//}

				//Feature selectedFeature = selectedFeatures[0];
				//return selectedFeature.GetObjectID() == sketchGeometry.GetObjectID() &&
				//	   selectedFeature.GetTable().GetID() == sketchGeometry.GetTable().GetID();
			});

			//IList<Feature> selectedFeatures = EditorUtils.GetSelectedEditFeatures(Editor);

			//// Consider using method CanUseSelection(out applicableSelection, notifications)
			//return CanUseCurrentSelectionCore() &&
			//	   selectedFeatures.Count == 1 && selectedFeatures[0] == feature;
		}

		///// <summary>
		///// Check if the given feature belongs to the originclass of the given
		///// relationship class.
		///// If the class of the feature and the originfeatureclass do not share
		///// the same name, then false is returned, there is no check, if the class
		///// of the given features does belong the the relationshipClass
		///// </summary>
		///// <param name="feature">Feature to check</param>
		///// <param name="relationshipClass">RelationshipClass used to get the originClass</param>
		///// <returns>TRUE if the feature is from the originClass, FALSE otherwise</returns>
		//private static bool IsFeatureFromOriginClass(
		//	[NotNull] Feature feature,
		//	[NotNull] RelationshipClass relationshipClass)
		//{
		//	string featureClassName = ((IDataset)feature.Class).Name;
		//	string originClassName = ((IDataset)relationshipClass.OriginClass).Name;

		//	return featureClassName.Equals(originClassName);
		//}

		///// <summary>
		///// Gets the list of relationships where the given object is one part of.
		///// </summary>
		///// <param name="gdbObject">Feature that must belong to the returned relationship</param>
		///// <param name="relationshipClass">RelationshipClass that holds the information
		///// about the relationships with the given object</param>
		///// <returns>List with IRelationship instances, could be empty</returns>
		//[NotNull]
		//private static IList<Relationship> GetRelationships(
		//	[NotNull] Object gdbObject,
		//	[NotNull] RelationshipClass relationshipClass)
		//{
		//	var result = new List<Relationship>();

		//	//IEnumRelationship relations = relationshipClass.GetRelationshipsForObject(gdbObject);

		//	if (relations != null)
		//	{
		//		relations.Reset();
		//		Relationship relationship;
		//		while ((relationship = relations.Next()) != null)
		//		{
		//			result.Add(relationship);
		//		}
		//	}

		//	return result;
		//}

		private async Task<bool> PickSecondFeatureAndMerge(Geometry sketchGeometry,
		                                                   CancelableProgressor progressor)
		{
			Feature secondFeature =
				await QueuedTask.Run(() => PickSecondFeature(sketchGeometry, progressor));

			if (secondFeature == null)
			{
				return false;
			}

			MergerBase merger = await QueuedTask.Run(() => GetMerger());

			IList<Feature> features = new List<Feature> { _firstFeature, secondFeature };
			bool canMerge = await QueuedTask.Run(() => merger.CanMerge(features));
			if (! canMerge)
			{
				return false;
			}

			// TODO: Remember the layer of the current selection to prioritize this layer when selecting the result feature!

			bool flipFeatures =
				await QueuedTask.Run(() => DetermineSecondFeatureIsUpdate(
					                     _firstFeature, secondFeature));

			Feature updateFeature = flipFeatures
				                        ? secondFeature
				                        : _firstFeature;
			Feature survivingFeature =
				await QueuedTask.Run(() => merger.MergeFeatures(
					                     new List<Feature> { _firstFeature, secondFeature },
					                     updateFeature));

			if (survivingFeature != null)
			{
				await QueuedTask.Run(() => SelectResultAndLogNextStep(survivingFeature));
			}

			return true;
		}

		private void SelectResultAndLogNextStep(Feature survivingFeature)
		{
			SelectionUtils.SelectFeature(ActiveMapView.Map, survivingFeature);

			if (_mergeToolOptions.UseMergeResultForNextMerge)
			{
				_firstFeature = survivingFeature;
				LogUsingCurrentSelection();
			}
			else
			{
				_firstFeature = null;
				LogPromptForSelection();
			}
		}

		protected virtual bool IsPickableTargetFeature([NotNull] Feature feature)
		{
			// assumes that editability is already checked at layer level

			if (_firstFeature == null)
			{
				return true;
			}

			Geometry firstFeatureShape = _firstFeature.GetShape();
			Geometry testFeatureShape = feature.GetShape();

			bool unEqualShapeTypes = firstFeatureShape.GeometryType !=
			                         testFeatureShape.GeometryType;

			if (unEqualShapeTypes)
			{
				return false;
			}

			return _firstFeature.GetObjectID() != feature.GetObjectID() ||
			       _firstFeature.GetTable().GetID() != feature.GetTable().GetID();
		}

		[CanBeNull]
		private Feature PickSecondFeature(Geometry sketchGeometry,
		                                  CancelableProgressor cancellabelProgressor)
		{
			var featureFinder = new FeatureFinder(ActiveMapView,
			                                      TargetFeatureSelection
				                                      .VisibleSelectableEditableFeatures)
			                    {
				                    //FeatureClassPredicate = GetTargetFeatureClassPredicate()
			                    };

			// They might be stored (insert target vertices):
			featureFinder.ReturnUnJoinedFeatures = true;

			Predicate<Layer> layerPredicate = null; // CanOverlapLayer;
			//IEnumerable<FeatureSelectionBase> featureClassSelections =
			//	featureFinder.FindFeaturesByLayer(sketchGeometry, layerPredicate);

			var selectionByClass =
				featureFinder.FindFeaturesByFeatureClass(sketchGeometry, layerPredicate);

			if (cancellabelProgressor?.CancellationToken.IsCancellationRequested == true)
			{
				return null;
			}

			var foundFeatures = new List<Feature>();

			//foreach (var classSelection in selectionByClass)
			//{
			//	foundFeatures.AddRange(classSelection.GetFeatures());
			//}

			foreach (var classSelection in selectionByClass)
			{
				foundFeatures.AddRange(classSelection.GetFeatures().Where(IsPickableTargetFeature));
			}

			// Filter out the first feature to avoid selecting it again
			foundFeatures.RemoveAll(f => GdbObjectUtils.IsSameFeature(f, _firstFeature));

			// Return the first valid feature (or implement custom logic to pick one)
			Feature selectedFeature = foundFeatures.FirstOrDefault();

			if (selectedFeature == null)
			{
				_msg.Info("No valid second feature found.");
			}

			return selectedFeature;

			//OLD version:
			// Remove the selected features from the set of overlapping features.
			// This is also important to make sure the geometries don't get mixed up / reset 
			// by inserting target vertices
			//foundFeatures.RemoveAll(f =>
			//	                        selectedFeatures.Any(s => GdbObjectUtils
			//		                                             .IsSameFeature(f, s)));

			//return foundFeatures;

			//IEnvelope envelope = null;
			//if (_envelopeDrawer != null)
			//{
			//	_envelopeDrawer.OnMouseUp(button, shift, x, y);

			//	envelope = _envelopeDrawer.EnvelopeValid
			//		           ? GeometryFactory.Clone(_envelopeDrawer.Envelope)
			//		           : null;

			//	// NOTE: envelope drawer will be disposed by the caller
			//}

			//PickerService picker = GetPickerService();

			//return envelope != null
			//	       ? picker.PickFeature(envelope, PickerReducingMode.None,
			//	                            IsPickableTargetLayer,
			//	                            PickerOptions.OnlySelectableLayers,
			//	                            IsPickableTargetFeature)
			//	       : picker.PickFeature(x, y, PickerReducingMode.None,
			//	                            IsPickableTargetLayer,
			//	                            PickerOptions.OnlySelectableLayers,
			//	                            IsPickableTargetFeature);
		}

		//private bool IsPickableTargetLayer([NotNull] ILayer layer)
		//{
		//	var featureLayer = layer as IFeatureLayer;

		//	return featureLayer != null &&
		//		   ArcMapUtils.IsEditable(MxApplication, featureLayer) &&
		//		   featureLayer.FeatureClass != null &&
		//		   CanSelectGeometryType(featureLayer.FeatureClass.ShapeType);
		//}

		/// <summary>
		/// Determines whether the second feature is the update (and the first shall be deleted) or not.
		/// </summary>
		/// <param name="firstFeature"></param>
		/// <param name="secondFeature"></param>
		/// <returns></returns>
		private bool DetermineSecondFeatureIsUpdate(Feature firstFeature,
		                                            Feature secondFeature)
		{
			bool result;

			// TODO: Use MergeOperationSurvivor to allow subclasses to modify using modifier key...
			if (_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject)
			{
				result = false;
			}
			else
			{
				Assert.True(_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject,
				            "Unsupported MergeOperationSurvivor.");

				Geometry firstShape = firstFeature.GetShape();
				Geometry secondShape = secondFeature.GetShape();

				Geometry larger =
					GeometryUtils.GetLargestGeometry(new List<Geometry>
					                                 {
						                                 firstShape,
						                                 secondShape
					                                 });

				bool firstFeatureIsLarger = firstShape == larger;

				result = ! firstFeatureIsLarger;
			}

			return result;
		}

		#endregion
	}
}
