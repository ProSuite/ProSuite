using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.Core.QA.VerificationProgress;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyVisibleExtentCmdBase : ButtonCommandBase
	{
		protected VerifyVisibleExtentCmdBase()
		{
			// Instead of wiring each single button and tool and calling SessionContext.CanVerifyQuality
			// for each one, the singleton event aggregator updates all at once:
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

		/// <summary>
		/// Returns the perimeter to verify, derived from the visible extent, for example
		/// intersected with the current work unit.
		/// </summary>
		/// <param name="visibleExtent">The visible extent of the active map view.</param>
		/// <returns>The perimeter to verify, or null if nothing is to be verified. In that
		/// case the implementation has informed the user.</returns>
		[CanBeNull]
		protected virtual Geometry GetPerimeterToVerify([NotNull] Envelope visibleExtent)
		{
			return visibleExtent;
		}

		protected override Task<bool> OnClickAsyncCore()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                "Verify Extent", MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				MessageBox.Show("No active map.", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Extent",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			Geometry perimeter = GetPerimeterToVerify(mapView.Extent);

			if (perimeter == null)
			{
				return Task.FromResult(false);
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			var projectWorkspace = (ProjectWorkspace) SessionContext.ProjectWorkspace;
			SpatialReference spatialRef = projectWorkspace?.ModelSpatialReference;

			var appController =
				new AgpBackgroundVerificationController(WorkListOpener, mapView, perimeter,
				                                        spatialRef,
				                                        SessionContext.VerificationEnvironment,
				                                        SaveAction);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(perimeter, progressTracker),
					ApplicationController = appController
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Visible Extent";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(
				window, qualitySpecification,
				qaEnvironment.BackendDisplayName ?? "<not connected to service>", actionTitle);

			return Task.FromResult(true);
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] Geometry perimeter,
			[NotNull] QualityVerificationProgressTracker progressTracker)
		{
			Task<ServiceCallStatus> verificationTask =
				await BackgroundTask.Run(
					() =>
					{
						IQualityVerificationEnvironment qaEnvironment =
							SessionContext.VerificationEnvironment;

						Assert.NotNull(qaEnvironment);

						return qaEnvironment.VerifyPerimeter(
							perimeter, progressTracker, "visible extent");
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}
	}
}
