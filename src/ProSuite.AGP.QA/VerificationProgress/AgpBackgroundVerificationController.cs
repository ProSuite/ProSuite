using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.QA.VerificationProgress
{
	public class AgpBackgroundVerificationController : IApplicationBackgroundVerificationController
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly MapView _mapView;
		[CanBeNull] private readonly Geometry _verifiedPerimeter;
		[CanBeNull] private readonly SpatialReference _verificationSpatialReference;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgpBackgroundVerificationController"/> class.
		/// </summary>
		/// <param name="mapView"></param>
		/// <param name="verifiedPerimeter"></param>
		/// <param name="verificationSpatialReference"></param>
		public AgpBackgroundVerificationController(
			[NotNull] MapView mapView,
			[CanBeNull] Geometry verifiedPerimeter,
			[CanBeNull] SpatialReference verificationSpatialReference)
		{
			_mapView = mapView;
			_verifiedPerimeter = verifiedPerimeter;
			_verificationSpatialReference = verificationSpatialReference;
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

		public void OpenWorkList(IQualityVerificationResult verificationResult)
		{
			throw new NotImplementedException();
		}

		public bool CanOpenWorkList(ServiceCallStatus? currentProgressStep,
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
				reason = "Opens the work list when the verification is completed";

				return false;
			}

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

			// TODO: Implement OpenWorkList

			reason = "Opening the work list from here is not yet supported";
			return false;
		}

		public void ShowReport(IQualityVerificationResult verificationResult)
		{
			if (verificationResult.HtmlReportPath == null)
			{
				return;
			}

			Process.Start(verificationResult.HtmlReportPath);
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
			throw new NotImplementedException();
		}

		public bool CanSaveIssues(IQualityVerificationResult verificationResult, out string reason)
		{
			reason = "Saving issues in production model error datasets is not yet supported";
			return false;
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

			IEnumerable<Polyline> completedPolylines = tiles.Select(e =>
				GeometryFactory.CreatePolyline(
					e, _verificationSpatialReference));

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
