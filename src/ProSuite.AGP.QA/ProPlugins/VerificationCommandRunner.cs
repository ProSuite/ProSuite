using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.UI.Core.QA.VerificationProgress;
using Keys = System.Windows.Forms.Keys;

namespace ProSuite.AGP.QA.ProPlugins
{
	/// <summary>
	/// Shared verification command operations, with application-specific preparation and UI
	/// dependencies supplied by the caller. Create a runner for each command invocation.
	/// </summary>
	public sealed class VerificationCommandRunner
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		[NotNull] private readonly IVerificationCommandContext _context;

		public VerificationCommandRunner([NotNull] IVerificationCommandContext context)
		{
			Assert.ArgumentNotNull(context, nameof(context));
			_context = context;
		}

		public Task<bool> VerifyFullExtent()
		{
			return VerifyPerimeter(null, "Verify Full Extent", "full extent");
		}

		public Task<bool> VerifyPerimeter(
			[CanBeNull] Geometry perimeter, [NotNull] string title,
			[NotNull] string perimeterDisplayName,
			bool saveVerification = false,
			[CanBeNull] Func<QualityVerificationProgressTracker, Task> onCompleted = null)
		{
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return Task.FromResult(false);
			}

			return ShowProgressAsync(environment, mapView, perimeter, title,
			                         p => VerifyPerimeterAsync(
				                         perimeter, p, perimeterDisplayName, saveVerification,
				                         onCompleted));
		}

		public Task<bool> VerifyVisibleExtent(
			[CanBeNull] Func<Envelope, Geometry> getPerimeter = null)
		{
			const string title = "Verify Visible Extent";
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return Task.FromResult(false);
			}

			Geometry perimeter =
				getPerimeter == null ? mapView.Extent : getPerimeter(mapView.Extent);
			return perimeter == null
				       ? Task.FromResult(false)
				       : ShowProgressAsync(environment, mapView, perimeter, title,
				                           p => VerifyPerimeterAsync(
					                           perimeter, p, "visible extent"));
		}

		public Task<bool> VerifyLast()
		{
			const string title = "Verify Last Perimeter";
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView, requireSpecification: false))
			{
				return Task.FromResult(false);
			}

			Geometry perimeter = environment.LastVerificationPerimeter;
			if (perimeter == null || perimeter.IsEmpty)
			{
				ShowWarning("No last verification perimeter.", title);
				return Task.FromResult(false);
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Alt, exclusive: true))
			{
				ZoomTo(mapView, perimeter);
				return Task.FromResult(true);
			}

			return ShowProgressAsync(environment, mapView, perimeter, title,
			                         p => VerifyPerimeterAsync(perimeter, p, "last extent"));
		}

		public async Task<bool> VerifySelectedPolygons(
			[CanBeNull] Func<Polygon, Geometry> getPerimeter = null)
		{
			const string title = "Verify Selection";
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return false;
			}

			if (! MapUtils.HasSelection(mapView.Map))
			{
				ShowWarning("No selected polygons.", title);
				return false;
			}

			Polygon union = await QueuedTask.Run(() => GetSelectedPolygonUnion(mapView));
			Geometry perimeter = getPerimeter == null ? union : getPerimeter(union);
			if (perimeter == null)
			{
				return false;
			}

			return await ShowProgressAsync(environment, mapView, perimeter, title,
			                               p => VerifyPerimeterAsync(
				                               perimeter, p, "selected polygons"));
		}

		public async Task<bool> VerifySelection(
			[NotNull] Func<MapView, Task<IList<Row>>> getRelevantSelection)
		{
			const string title = "Verify Selection";
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return false;
			}

			if (! MapUtils.HasSelection(mapView.Map))
			{
				ShowWarning("No selected features", title);
				return false;
			}

			IList<Row> selection = await QueuedTask.Run(() => getRelevantSelection(mapView));
			return await ShowProgressAsync(environment, mapView, null, title,
			                               p => ExecuteInBackground(() =>
				                               Assert.NotNull(_context.Environment)
				                                     .VerifySelection(selection, null, p)));
		}

		/// <summary>
		/// Verifies an already resolved selection of rows, such as the involved objects of an
		/// issue being retested. Requires an active map, but does not check the map's selection.
		/// </summary>
		public Task<bool> VerifySelection(
			[NotNull] IList<Row> selection, [CanBeNull] Geometry perimeter,
			[NotNull] string title,
			[CanBeNull] Action<VerificationProgressViewModel> configureViewModel = null)
		{
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return Task.FromResult(false);
			}

			return ShowProgressAsync(environment, mapView, perimeter, title,
			                         p => ExecuteInBackground(() =>
				                                                  Assert.NotNull(
						                                                  _context.Environment)
					                                                  .VerifySelection(
						                                                  selection, perimeter, p)),
			                         configureViewModel);
		}

		public async Task<bool> VerifySketch(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] Func<Geometry, Geometry> getPerimeter = null)
		{
			const string title = "Verify Perimeter";
			GeometryUtils.Simplify(sketchGeometry);
			if (! TryGetContext(title, out IQualityVerificationEnvironment environment,
			                    out MapView mapView))
			{
				return false;
			}

			if (await QueuedTask.Run(() => ToolUtils.IsSingleClickSketch(sketchGeometry)))
			{
				ShowWarning(
					"Invalid perimeter. Please draw a box to define the extent to be verified.",
					title);
				return false;
			}

			Geometry perimeter =
				getPerimeter == null ? sketchGeometry : getPerimeter(sketchGeometry);
			if (perimeter == null)
			{
				return false;
			}

			return await ShowProgressAsync(environment, mapView, perimeter, title,
			                               p => VerifyPerimeterAsync(perimeter, p, "perimeter"));
		}

		private static async Task<ServiceCallStatus> ExecuteInBackground(
			[NotNull] Func<Task<ServiceCallStatus>> verifyAction)
		{
			// The async lambda is required to avoid duplicate sketch-completion callbacks.
			Task<ServiceCallStatus> verificationTask = await BackgroundTask.Run(
				                                           async () => await verifyAction(),
				                                           BackgroundProgressor.None);
			return await verificationTask;
		}

		private Task<ServiceCallStatus> VerifyPerimeterAsync(
			Geometry perimeter, QualityVerificationProgressTracker progress,
			string perimeterDisplayName, bool saveVerification = false,
			[CanBeNull] Func<QualityVerificationProgressTracker, Task> onCompleted = null)
		{
			return ExecuteInBackground(async () =>
			{
				ServiceCallStatus result = await Assert.NotNull(_context.Environment)
				                                       .VerifyPerimeter(
					                                       perimeter, progress,
					                                       perimeterDisplayName,
					                                       saveVerification);
				if (onCompleted != null)
				{
					await onCompleted(progress);
				}

				return result;
			});
		}

		private async Task<bool> ShowProgressAsync(
			IQualityVerificationEnvironment environment, MapView mapView, Geometry perimeter,
			string title,
			Func<QualityVerificationProgressTracker, Task<ServiceCallStatus>> verifyAction,
			[CanBeNull] Action<VerificationProgressViewModel> configureViewModel = null)
		{
			IQualitySpecificationReference specification =
				Assert.NotNull(environment.CurrentQualitySpecificationReference);
			if (! await _context.PrepareAsync(title))
			{
				return false;
			}

			var progress = new QualityVerificationProgressTracker
			               {
				               CancellationTokenSource = new CancellationTokenSource()
			               };
			var viewModel = new VerificationProgressViewModel
			                {
				                ProgressTracker = progress,
				                VerificationAction = () => verifyAction(progress),
				                ApplicationController =
					                _context.CreateController(mapView, perimeter)
			                };
			configureViewModel?.Invoke(viewModel);
			Window window = VerificationProgressWindow.Create(viewModel);
			VerifyUtils.ShowProgressWindow(window, specification,
			                               environment.BackendDisplayName ??
			                               "<not connected to service>",
			                               $"{specification.Name}: {title}");
			return true;
		}

		private bool TryGetContext(string title, out IQualityVerificationEnvironment environment,
		                           out MapView mapView, bool requireSpecification = true)
		{
			environment = _context.Environment;
			mapView = MapView.Active;
			if (environment == null)
			{
				ShowWarning("No quality verification environment is configured.", title);
				return false;
			}

			if (mapView == null)
			{
				ShowWarning("No active map.", title);
				return false;
			}

			return ! requireSpecification || HasSpecification(environment, title);
		}

		private static bool HasSpecification(IQualityVerificationEnvironment environment,
		                                     string title)
		{
			if (environment.CurrentQualitySpecificationReference != null)
			{
				return true;
			}

			ShowWarning("No quality specification is selected", title);
			return false;
		}

		private static void ShowWarning(string message, string title)
		{
			Gateway.ShowMessage(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private static Polygon GetSelectedPolygonUnion(MapView mapView)
		{
			var polygons = new List<Polygon>();
			bool anySelected = false;
			foreach (Feature feature in SelectionUtils.GetSelectedFeatures(mapView))
			{
				anySelected = true;
				if (feature.GetShape() is Polygon polygon)
				{
					polygons.Add(polygon);
				}
			}

			if (polygons.Count == 0)
			{
				throw new InvalidOperationException(anySelected
					                                    ? "The selection does not contain a polygon feature. Please select at least one polygon feature."
					                                    : "No feature is selected. Please select at least one polygon feature.");
			}

			if (polygons.Count == 1)
			{
				return polygons[0];
			}

			_msg.InfoFormat("Calculating union of {0} polygons", polygons.Count);
			return (Polygon) GeometryUtils.Union(polygons);
		}

		private static void ZoomTo(MapView mapView, Geometry perimeter)
		{
			QueuedTaskUtils.Run(async delegate
			{
				try
				{
					Envelope extent = perimeter.Extent.Expand(1.1, 1.1, true);
					await mapView.ZoomToAsync(extent, TimeSpan.FromSeconds(0.3));
					CIMColor darkGreen = ColorFactory.Instance.CreateRGBColor(0, 128, 0);
					CIMStroke outline = SymbolFactory.Instance.ConstructStroke(
						darkGreen, 3, SimpleLineStyle.Solid);
					CIMPolygonSymbol symbol = SymbolFactory.Instance.ConstructPolygonSymbol(
						darkGreen, SimpleFillStyle.Null, outline);
					await MapUtils.FlashGeometryAsync(mapView, perimeter,
					                                  symbol.MakeSymbolReference(),
					                                  milliseconds: 800);
				}
				catch (Exception e)
				{
					_msg.Warn("Error zooming to/flashing verified perimeter", e);
				}
			});
		}
	}
}
