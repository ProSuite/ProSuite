using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.Core.QA.VerificationProgress;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyLastCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		//TODO: improve enabling: disable if qaEnvironment.LastVerificationPerimeter is null
		protected VerifyLastCmdBase()
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

		protected override Task<bool> OnClickAsyncCore()
		{
			if (SessionContext?.VerificationEnvironment == null)
			{
				MessageBox.Show("No quality verification environment is configured.",
				                "Verify Last", MessageBoxButton.OK, MessageBoxImage.Warning);
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
				MessageBox.Show("No Quality Specification is selected", "Verify Last",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			Geometry perimeter = qaEnvironment.LastVerificationPerimeter;

			if (perimeter == null || perimeter.IsEmpty)
			{
				MessageBox.Show("No last verification perimeter", "Verify Last",
				                MessageBoxButton.OK, MessageBoxImage.Warning);
				return Task.FromResult(false);
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Alt, exclusive: true))
			{
				ZoomTo(perimeter);

				return Task.FromResult(true);
			}

			var progressTracker = new QualityVerificationProgressTracker
			                      {
				                      CancellationTokenSource = new CancellationTokenSource()
			                      };

			string resultsPath = VerifyUtils.GetResultsPath(qualitySpecification);

			var projectWorkspace = (ProjectWorkspace) SessionContext.ProjectWorkspace;
			SpatialReference spatialRef = projectWorkspace?.ModelSpatialReference;

			var appController =
				new AgpBackgroundVerificationController(WorkListOpener, mapView, perimeter,
				                                        spatialRef, SaveAction);

			var qaProgressViewmodel =
				new VerificationProgressViewModel
				{
					ProgressTracker = progressTracker,
					VerificationAction = () => Verify(perimeter, progressTracker, resultsPath),
					ApplicationController = appController
				};

			string actionTitle = $"{qualitySpecification.Name}: Verify Last Perimeter";

			Window window = VerificationProgressWindow.Create(qaProgressViewmodel);

			VerifyUtils.ShowProgressWindow(
				window, qualitySpecification,
				qaEnvironment.BackendDisplayName ?? "<not connected to service", actionTitle);

			return Task.FromResult(true);
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
							perimeter, progressTracker, "last extent", resultsPath);
					},
					BackgroundProgressor.None);

			ServiceCallStatus result = await verificationTask;

			return result;
		}

		private static void ZoomTo([NotNull] Geometry perimeter)
		{
			QueuedTaskUtils.Run(
				async delegate
				{
					try
					{
						Envelope extent = perimeter.Extent.Expand(1.1, 1.1, true);

						await MapView.Active.ZoomToAsync(extent, TimeSpan.FromSeconds(0.3));

						CIMColor darkGreen = ColorFactory.Instance.CreateRGBColor(0, 128, 0);

						CIMStroke outline =
							SymbolFactory.Instance.ConstructStroke(
								darkGreen, 3, SimpleLineStyle.Solid);

						CIMPolygonSymbol polygonSymbol =
							SymbolFactory.Instance.ConstructPolygonSymbol(
								darkGreen, SimpleFillStyle.Null, outline);

						await MapUtils.FlashGeometryAsync(MapView.Active, perimeter,
						                                  polygonSymbol.MakeSymbolReference(),
						                                  milliseconds: 800);
					}
					catch (Exception e)
					{
						_msg.Warn($"Error zooming to/flashing verified perimeter: {e.Message}", e);
					}
				});
		}
	}
}
