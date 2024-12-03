using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.QA.VerificationProgress
{
	public class AgpBackgroundVerificationController : IApplicationBackgroundVerificationController
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IWorkListOpener _workListOpener;
		private readonly MapView _mapView;
		[CanBeNull] private readonly Geometry _verifiedPerimeter;
		[CanBeNull] private readonly SpatialReference _verificationSpatialReference;

		private bool _issuesSaved;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgpBackgroundVerificationController"/> class.
		/// </summary>
		/// <param name="workListOpener"></param>
		/// <param name="mapView"></param>
		/// <param name="verifiedPerimeter"></param>
		/// <param name="verificationSpatialReference"></param>
		/// <param name="saveAction"></param>
		public AgpBackgroundVerificationController(
			[NotNull] IWorkListOpener workListOpener,
			[NotNull] MapView mapView,
			[CanBeNull] Geometry verifiedPerimeter,
			[CanBeNull] SpatialReference verificationSpatialReference,
			[CanBeNull]
			Action<IQualityVerificationResult, ErrorDeletionInPerimeter, bool> saveAction = null)
		{
			Assert.ArgumentNotNull(workListOpener, nameof(workListOpener));
			Assert.ArgumentNotNull(mapView, nameof(mapView));

			_workListOpener = workListOpener;
			_mapView = mapView;
			_verifiedPerimeter = verifiedPerimeter;
			_verificationSpatialReference = verificationSpatialReference;

			SaveAction = saveAction;
		}

		[CanBeNull]
		private Action<IQualityVerificationResult, ErrorDeletionInPerimeter, bool> SaveAction
		{
			get;
		}

		public void FlashProgress(IList<EnvelopeXY> tiles,
		                          ServiceCallStatus currentProgressStep)
		{
			if (tiles.Count == 0)
			{
				return;
			}

			// Multi-threaded access: Copy the list to be able to enumerate it:
			List<EnvelopeXY> immutableList = tiles.ToList();

			QueuedTaskUtils.Run(
				async delegate
				{
					try
					{
						bool flashed = await FlashProgressAsync(immutableList, currentProgressStep);
						_msg.DebugFormat("Flashed progress: {0}", flashed);
					}
					catch (Exception e)
					{
						_msg.Warn($"Error flashing verification progress: {e.Message}", e);
					}
				}).ConfigureAwait(false).GetAwaiter();
		}

		public bool CanFlashProgress(ServiceCallStatus? currentProgressStep,
		                             IList<EnvelopeXY> tiles,
		                             out string reason)
		{
			if (currentProgressStep == ServiceCallStatus.Undefined)
			{
				reason =
					"Shows the tile verification progress but tile processing has not yet started.";
				return false;
			}

			if (tiles.Count == 0)
			{
				reason =
					"Shows the tile verification progress but tile processing has not yet started";
				return false;
			}

			reason = null;
			return true;
		}

		public void ZoomToVerifiedPerimeter()
		{
			if (_mapView == null)
			{
				return;
			}

			if (_verifiedPerimeter == null)
			{
				return;
			}

			QueuedTaskUtils.Run(
				async delegate
				{
					try
					{
						await _mapView.ZoomToAsync(_verifiedPerimeter.Extent);

						CIMRGBColor green = ColorUtils.CreateRGB(0, 255, 0);

						CIMPolygonSymbol polygonSymbol = SymbolUtils.CreatePolygonSymbol(green);

						Polygon polygon = GeometryFactory.CreatePolygon(_verifiedPerimeter.Extent);

						await MapUtils.FlashGeometryAsync(_mapView, polygon,
						                                  polygonSymbol.MakeSymbolReference());
					}
					catch (Exception e)
					{
						_msg.Warn($"Error zooming to verified perimeter: {e.Message}", e);
					}
				});
		}

		public bool CanZoomToVerifiedPerimeter(out string reason)
		{
			if (_verifiedPerimeter == null)
			{
				reason = "No perimeter";

				return false;
			}

			reason = null;
			return true;
		}

		public async Task OpenWorkList(IQualityVerificationResult verificationResult,
		                               bool replaceExisting)
		{
			if (_workListOpener.CanUseProductionModelIssueSchema())
			{
				Envelope envelope = null;

				_msg.Info("Opening production model issue work list...");

				await _workListOpener.OpenProductionModelIssueWorkEnvironmentAsync(envelope);
			}
			else
			{
				_msg.InfoFormat("Opening issue geodatabase ({0}) work list...",
				                verificationResult.IssuesGdbPath);
				await ViewUtils.TryAsync(
					_workListOpener.OpenFileGdbIssueWorkListAsync(verificationResult.IssuesGdbPath,
						replaceExisting), _msg);
			}
		}

		public bool CanOpenWorkList(ServiceCallStatus? currentProgressStep,
		                            IQualityVerificationResult verificationResult,
		                            out string reason)
		{
			// TODO: Access to error datasets in model context

			if (verificationResult == null)
			{
				reason = "Dialog has not been fully initialized";

				return false;
			}

			if (currentProgressStep == ServiceCallStatus.Running ||
			    currentProgressStep == ServiceCallStatus.Undefined)
			{
				reason = "Opens the work list when the verification is completed";

				return false;
			}

			if (! verificationResult.HasIssues)
			{
				reason = "No issues";
				return false;
			}

			if (_workListOpener.CanUseProductionModelIssueSchema())
			{
				reason =
					"Open Issue Work List from project workspace using traditional error datasets";
				return true;
			}

			// No production model issue schema, use IssueGdb:
			if (string.IsNullOrEmpty(verificationResult.IssuesGdbPath))
			{
				reason = "No issue File Geodatabase has been created";

				return false;
			}

			if (! Directory.Exists(verificationResult.IssuesGdbPath))
			{
				reason =
					$"Issue File Geodatabase at {verificationResult.IssuesGdbPath} does not exist or cannot be accessed";

				return false;
			}

			reason = $"Open Issue Work List using {verificationResult.IssuesGdbPath}";
			return true;
		}

		public void ShowReport(IQualityVerificationResult verificationResult)
		{
			if (verificationResult.HtmlReportPath == null)
			{
				return;
			}

			ProcessUtils.StartProcess(verificationResult.HtmlReportPath);
		}

		public bool CanShowReport(ServiceCallStatus? currentProgressStep,
		                          IQualityVerificationResult verificationResult,
		                          out string reason)
		{
			if (verificationResult == null)
			{
				reason = "Dialog has not been fully initialized";

				return false;
			}

			if (currentProgressStep == ServiceCallStatus.Running ||
			    currentProgressStep == ServiceCallStatus.Undefined)
			{
				reason = "Shows the verification report when the verification is completed";

				return false;
			}

			if (string.IsNullOrEmpty(verificationResult.HtmlReportPath))
			{
				reason = "No HTML report has been created";

				return false;
			}

			if (! File.Exists(verificationResult.HtmlReportPath))
			{
				reason =
					$"HTML report at {verificationResult.HtmlReportPath} does not exist or cannot be accessed";

				return false;
			}

			reason = null;

			return true;
		}

		public void SaveIssues(IQualityVerificationResult verificationResult,
		                       ErrorDeletionInPerimeter errorDeletion,
		                       bool updateLatestTestDate)
		{
			SaveAction?.Invoke(verificationResult, errorDeletion,
			                   updateLatestTestDate);

			_issuesSaved = verificationResult.IssuesSaved >= 0;
		}

		public bool CanSaveIssues(IQualityVerificationResult verificationResult, out string reason)
		{
			if (verificationResult == null)
			{
				reason = "Dialog has not been fully initialized";

				return false;
			}

			if (SaveAction == null)
			{
				reason = "Saving is not supported";
				return false;
			}

			if (_issuesSaved)
			{
				reason = "Issues have already been saved";
				return false;
			}

			bool result = verificationResult.CanSaveIssues;

			reason = result ? null : "No issues have been collected";

			return result;
		}

		private async Task<bool> FlashProgressAsync([NotNull] IList<EnvelopeXY> tiles,
		                                            ServiceCallStatus currentProgressStep)
		{
			if (tiles.Count == 0)
			{
				return false;
			}

			CIMRGBColor green = ColorUtils.CreateRGB(0, 200, 0);

			CIMLineSymbol lineSymbol = SymbolUtils.CreateLineSymbol(green, 2);

			List<Overlay> overlays = new List<Overlay>(2);

			var completedPolylines = tiles.Select(e =>
				                                      GeometryFactory.CreatePolyline(
					                                      e, _verificationSpatialReference))
			                              .ToList();

			Geometry completedLineGeometry = GeometryUtils.Union(completedPolylines);

			Overlay gridOverlay = new Overlay(completedLineGeometry, lineSymbol);

			overlays.Add(gridOverlay);

			var currentPolyOverlay = CreateCurrentPolyOverlay(tiles, currentProgressStep);

			if (currentPolyOverlay != null)
			{
				overlays.Add(currentPolyOverlay);
			}

			await MapUtils.FlashGeometriesAsync(_mapView, overlays, 1000);

			// Keep the current tile a bit longer...
			if (currentPolyOverlay != null)
			{
				return await MapUtils.FlashGeometryAsync(_mapView, currentPolyOverlay);
			}

			return true;
		}

		[CanBeNull]
		private Overlay CreateCurrentPolyOverlay([NotNull] IList<EnvelopeXY> tiles,
		                                         ServiceCallStatus currentProgressStep)
		{
			if (currentProgressStep == ServiceCallStatus.Finished)
			{
				return null;
			}

			CIMRGBColor fillColor;
			switch (currentProgressStep)
			{
				case ServiceCallStatus.Running:
					fillColor = ColorUtils.ParseHexColorARGB(PaleGreen);
					break;
				case ServiceCallStatus.Cancelled:
					fillColor = ColorUtils.ParseHexColorARGB(SandyBrown);
					break;
				case ServiceCallStatus.Failed:
					fillColor = ColorUtils.ParseHexColorARGB(OrangeRed);
					break;
				default:
					throw new ArgumentOutOfRangeException(
						nameof(currentProgressStep), currentProgressStep, @"Unexpected value");
			}

			CIMPolygonSymbol polygonSymbol = SymbolUtils.CreatePolygonSymbol(fillColor);

			Geometry currentPoly =
				GeometryFactory.CreatePolygon(tiles[tiles.Count - 1],
				                              _verificationSpatialReference);

			return new Overlay(currentPoly, polygonSymbol);
		}

		private string PaleGreen { get; } = "#A098FB98";

		private string SandyBrown { get; } = "#A0F4A460";

		private string OrangeRed { get; } = "#A0FF4500";
	}
}
