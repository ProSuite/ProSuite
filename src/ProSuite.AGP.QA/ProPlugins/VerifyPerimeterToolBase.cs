using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.QA.VerificationProgress;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.AGP.QA.ProPlugins
{
	// todo daro: extract common base class for VerifyPerimeterToolBase, VerifySelectionCmdBase, VerifyVisibleExtentCmdBase
	// TODO: Move OneClickToolBase to ProSuite.AGP as a shared project instead of using AGP.Editing
	public abstract class VerifyPerimeterToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected VerifyPerimeterToolBase()
		{
			IsSketchTool = true;
			CompleteSketchOnMouseUp = true;
			RequiresSelection = false;

			//SelectionCursor = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursor);
			//SelectionCursorShift = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursorShift);

			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		protected override Task OnToolActivateAsync(bool active)
		{
			return base.OnToolActivateAsync(active);
		}

		protected override bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			SetupSketch();

			SketchType = SketchGeometryType.Rectangle;

			return base.OnToolActivatedCore(hasMapViewChanged);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			GeometryUtils.Simplify(sketchGeometry);

			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                "Verify Extent", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				MessageBox.Show("No active map.", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			bool isSingleClickSketch = false;
			await QueuedTask.Run(() =>
			{
				isSingleClickSketch =
					ToolUtils.IsSingleClickSketch(sketchGeometry);
			});

			if (isSingleClickSketch)
			{
				MessageBox.Show(
					"Invalid perimeter. Please draw a box to define the extent to be verified",
					"Verify Extent",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Perimeter",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			var projectWorkspace = (ProjectWorkspace) SessionContext.ProjectWorkspace;
			SpatialReference spatialRef = projectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(
				WorkListOpener, mapView, sketchGeometry, spatialRef, SaveAction);

			string perimeterName = "Perimeter";

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(sketchGeometry, progressTracker, resultsPath),
					ApplicationController = appController,
					KeepPreviousIssuesDisabled = true
				};

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			string actionTitle = $"{qualitySpecification.Name}: Verify {perimeterName}";

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               Assert.NotNull(qaEnvironment.BackendDisplayName),
			                               actionTitle);

			return true;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override Task HandleEscapeAsync()
		{
			return Task.CompletedTask;
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info("Draw a box or press P and draw a polygon");
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info("Draw a box or press P and draw a polygon");
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progressTracker,
			string resultsPath)
		{
			// NOTE: If the background task is not Run( async () => ... but only Run(() => ...
			// The tool's OnSketchCompleteAsync will be called twice! 
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					async () =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							SessionContext.VerificationEnvironment;

						Assert.NotNull(qaEnvironment);

						return await qaEnvironment.VerifyPerimeter(
							       perimeter, progressTracker, "perimeter", resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}
	}
}
