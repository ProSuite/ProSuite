using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.Commons.AGP;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA.VerificationProgress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyFullExtentCmdBase : ButtonCommandBase
	{
		protected VerifyFullExtentCmdBase()
		{
			// Instead of wiring each single button and tool and calling SessionContext.CanVerifyQuality
			// for each one, the singleton event aggregator updates all at once:
			Register();
		}
		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IMapBasedSessionContext SessionContext { get; }

		protected abstract IProSuiteFacade ProSuiteImpl { get; }
		protected override Task<bool> OnClickCore()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
								"Verify Full Extent", MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			IQualityVerificationEnvironment qaEnvironment =
				Assert.NotNull(SessionContext.VerificationEnvironment);

			IQualitySpecificationReference qualitySpecification =
				qaEnvironment.CurrentQualitySpecificationReference;

			if (qualitySpecification == null)
			{
				MessageBox.Show("No Quality Specification is selected", "Verify Full Extent",
								MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			var progressTracker = new QualityVerificationProgressTracker
			{
				CancellationTokenSource = new CancellationTokenSource()
			};

			Envelope fullExtent = null;

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			SpatialReference spatialRef = SessionContext.ProjectWorkspace?.ModelSpatialReference;

			var appController =
				new AgpBackgroundVerificationController(ProSuiteImpl, MapView.Active, fullExtent,
														spatialRef);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Full Extent";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(window, qualitySpecification,
										   qaEnvironment.BackendDisplayName, actionTitle);

			return Task.FromResult(true);
		}

		private async Task<ServiceCallStatus> Verify(
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
							null, progressTracker, "full extent", resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}
	}
}
