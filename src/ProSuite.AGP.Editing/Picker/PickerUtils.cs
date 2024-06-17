using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickerUtils
	{
		public static Uri GetImagePath(esriGeometryType? geometryType)
		{
			// todo daro introduce image for unkown type
			//if (geometryType == null)
			//{
			//}
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PointGeometry.bmp");
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryPolyline:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/LineGeometry.bmp");
				case esriGeometryType.esriGeometryPolygon:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/PolygonGeometry.bmp",
						UriKind.Absolute);
				case esriGeometryType.esriGeometryMultiPatch:
					return new Uri(
						@"pack://application:,,,/ProSuite.AGP.Editing;component/PickerUI/Images/MultipatchGeometry.bmp");
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {geometryType}");
			}
		}

		[NotNull]
		public static IEnumerable<FeatureSelectionBase> OrderByGeometryDimension(
			[NotNull] IEnumerable<FeatureSelectionBase> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection
			       .GroupBy(classSelection => classSelection.ShapeDimension)
			       .OrderBy(group => group.Key).SelectMany(fcs => fcs);
		}

		private static bool SingleClick(Geometry geometry, int tolerancePixels)
		{
			Envelope extent = geometry.Extent;

			double toleranceMapUnits =
				MapUtils.ConvertScreenPixelToMapLength(MapView.Active, tolerancePixels,
				                                       extent.Center);

			return IsSingleClick(extent);
			//return extent.Width <= toleranceMapUnits && extent.Height <= toleranceMapUnits;
		}

		private static Geometry EnsureNonEmpty([NotNull] Geometry sketch, int tolerancePixel)
		{
			return IsSingleClick(sketch) ? CreateSinglePickGeometry(sketch, tolerancePixel) : sketch;
		}

		private static Geometry CreateSinglePickGeometry([NotNull] Geometry sketch, int tolerancePixel)
		{
			return Buffer(CreateMapPoint(sketch), tolerancePixel);
		}

		private static MapPoint CreateMapPoint([NotNull] Geometry sketch)
		{
			return MapPointBuilderEx.CreateMapPoint(
				new Coordinate2D(sketch.Extent.XMin, sketch.Extent.YMin),
				sketch.SpatialReference);
		}

		private static Geometry Buffer(Geometry sketchGeometry, int selectionTolerancePixels)
		{
			double selectionToleranceMapUnits = MapUtils.ConvertScreenPixelToMapLength(
				MapView.Active, selectionTolerancePixels, sketchGeometry.Extent.Center);

			return GeometryEngine.Instance.Buffer(sketchGeometry, selectionToleranceMapUnits);
		}

		private static bool IsSingleClick(Geometry sketch)
		{
			return ! (sketch.Extent.Width > 0 || sketch.Extent.Height > 0);
		}

		public static async Task Show(
			[NotNull] IPickerPrecedence precedence,
			[NotNull] Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			bool areaSelect = false;

			List<FeatureSelectionBase> selection =
				await QueuedTaskUtils.Run(() =>
				{
					// todo daro Implement this in IPickerPrecedence once it's tested.
					// todo daro compare with OneClickToolBase implementation
					areaSelect = ! SingleClick(precedence.SelectionGeometry, precedence.SelectionTolerance);

					Geometry geometry = EnsureNonEmpty(precedence.SelectionGeometry, precedence.SelectionTolerance);

					return getSelection(geometry).ToList();
				}, progressor);

			if (selection.Count == 0)
			{
				return;
			}

			var orderedSelection = OrderByGeometryDimension(selection).ToList();

			// IPickerPrecedence.GetPickerMode has to be on GUI thread to capture key down events, etc.
			PickerMode mode = precedence.GetPickerMode(orderedSelection, areaSelect);

			switch (mode)
			{
				case PickerMode.ShowPicker:
					await ShowPicker(precedence, orderedSelection);
					break;
				case PickerMode.PickAll:
					await SelectAll(selection, progressor);
					break;
				case PickerMode.PickBest:
					await PickBest(precedence, selection, progressor);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static async Task Show(
			[NotNull] IPickerPrecedence precedence,
			[NotNull] Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection,
			PickerMode mode = PickerMode.ShowPicker,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			List<FeatureSelectionBase> selection =
				await QueuedTaskUtils.Run(() =>
				{
					Geometry geometry = EnsureNonEmpty(precedence.SelectionGeometry, precedence.SelectionTolerance);

					return getSelection(geometry).ToList();

				}, progressor);

			if (selection.Count == 0)
			{
				return;
			}

			switch (mode)
			{
				case PickerMode.ShowPicker:
					await ShowPicker(precedence, OrderByGeometryDimension(selection).ToList());
					break;
				case PickerMode.PickAll:
					await SelectAll(selection, progressor);
					break;
				case PickerMode.PickBest:
					await PickBest(precedence, selection, progressor);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static async Task ShowPicker(IPickerPrecedence precedence,
		                                     List<FeatureSelectionBase> selection)
		{
			Geometry geometry = precedence.SelectionGeometry;
			var picker = new PickerService();

			Func<Task<IPickableFeatureItem>> showPicker =
				await QueuedTask.Run(() =>
				{
					var items = PickableItemsFactory
					            .CreateFeatureItems(selection)
					            .ToList();

					Point pickerLocation = MapView.Active.MapToScreen(geometry.Extent.Center);

					return picker.Pick<IPickableFeatureItem>(items, pickerLocation, precedence);
				});

			// show control on GUI thread
			IPickableFeatureItem pickedItem = await showPicker();

			if (pickedItem == null)
			{
				return;
			}

			await QueuedTask.Run(() =>
			{
				SelectionUtils.SelectFeature(pickedItem.Layer,
				                             SelectionCombinationMethod.New,
				                             pickedItem.Oid);
			});
		}

		private static async Task SelectAll(ICollection<FeatureSelectionBase> selection,
		                                    Progressor progressor)
		{
			await QueuedTaskUtils.Run(
				() => { SelectionUtils.SelectFeatures(selection, SelectionCombinationMethod.New); },
				progressor);
		}

		private static async Task PickBest(IPickerPrecedence precedence,
		                                   IReadOnlyCollection<FeatureSelectionBase> selection,
		                                   Progressor progressor)
		{
			if (! selection.Any())
			{
				return;
			}

			await QueuedTaskUtils.Run(
				() =>
				{
					// all this code has to be in QueuedTask because
					// IEnumerables are enumerated later
					var bestPick =
						precedence.PickBest<IPickableFeatureItem>(
							PickableItemsFactory.CreateFeatureItems(selection));

					//since SelectionCombinationMethod.New is only applied to
					//the current layer but selections of other layers remain,
					//we manually need to clear all selections first.

					SelectionUtils.SelectFeature(
						bestPick.Layer, SelectionCombinationMethod.New,
						bestPick.Oid);
				}, progressor);
		}
	}
}
