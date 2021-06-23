using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.QA.VerificationProgress;
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

namespace ProSuite.AGP.QA.ProPlugins
{
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
		}

		protected abstract IMapBasedSessionContext SessionContext { get; }

		protected abstract Window CreateProgressWindow(
			VerificationProgressViewModel progressViewModel);

		protected override Task OnToolActivateAsync(bool active)
		{
			return base.OnToolActivateAsync(active);
		}

		protected override Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			GeometryUtils.Simplify(sketchGeometry);

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecification;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification,
			                                                Project.Current.HomeFolderPath);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController = new AgpBackgroundVerificationController(
				MapView.Active, sketchGeometry, spatialRef);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(sketchGeometry, progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = "Verify Perimeter";

			Window window = CreateProgressWindow(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
			                               qaEnvironment.BackendDisplayName, actionTitle);

			return Task.FromResult(true);
		}

		protected override bool HandleEscape()
		{
			return true;
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
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							SessionContext.VerificationEnvironment;

						Assert.NotNull(qaEnvironment);

						return qaEnvironment.VerifyPerimeter(
							perimeter, progressTracker, resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			if (result == ServiceCallStatus.Finished)
			{
				_msg.InfoFormat(
					"Successfully finished extent verification. The results have been saved in {0}",
					resultsPath);
			}
			else
			{
				_msg.WarnFormat("Extent verification was not finished. Status: {0}", result);
			}

			return result;
		}
	}
}
