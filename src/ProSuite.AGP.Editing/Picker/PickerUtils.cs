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
using ProSuite.Commons.AGP.Core.Spatial;
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

		private static Geometry EnsureNonEmpty([NotNull] Geometry sketch, int tolerancePixel)
		{
			return IsSingleClick(sketch) ? CreateSinglePickGeometry(sketch, tolerancePixel) : sketch;
		}

		private static Geometry CreateSinglePickGeometry([NotNull] Geometry sketch,
		                                                 int tolerancePixel)
		{
			MapPoint mapPoint =
				GeometryFactory.CreatePoint(sketch.Extent.XMin,
				                            sketch.Extent.YMin,
				                            sketch.SpatialReference);

			return ExpandGeometryByPixels(mapPoint, tolerancePixel);
		}

		private static Geometry ExpandGeometryByPixels(Geometry sketchGeometry, int selectionTolerancePixels)
		{
			double selectionToleranceMapUnits = MapUtils.ConvertScreenPixelToMapLength(
				MapView.Active, selectionTolerancePixels, sketchGeometry.Extent.Center);

			double envelopeExpansion = selectionToleranceMapUnits * 2;

			Envelope envelope = sketchGeometry.Extent;

			// NOTE: MapToScreen in stereo map is sensitive to Z value (Picker location!)

			// Rather than creating a non-Z-aware polygon with elliptic arcs by using buffer...
			//Geometry selectionGeometry =
			//	GeometryEngine.Instance.Buffer(sketchGeometry, bufferDistance);

			// Just expand the envelope
			// .. but PickerViewModel needs a polygon to display selection geometry (press space).

			// HasZ, HasM and HasID are inherited from input geometry. Thereßss no need
			// for GeometryUtils.EnsureGeometrySchema()

			return GeometryFactory.CreatePolygon(
				envelope.Expand(envelopeExpansion, envelopeExpansion, false),
				envelope.SpatialReference);
		}

		private static bool IsSingleClick(Geometry sketch)
		{
			return ! (sketch.Extent.Width > 0 || sketch.Extent.Height > 0);
		}

		public static async Task Show(
			[NotNull] Geometry selectionGeometry,
			int selectionTolerance,
			[NotNull] Type precedenceType,
			[NotNull] Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			bool areaSelect = false;
			IPickerPrecedence precedence = null;

			List<FeatureSelectionBase> selection =
				await QueuedTaskUtils.Run(() =>
				{
					// todo daro Implement this in IPickerPrecedence once it's tested.
					// todo daro compare with OneClickToolBase implementation
					areaSelect = ! IsSingleClick(selectionGeometry);

					Geometry geometry = EnsureNonEmpty(selectionGeometry, selectionTolerance);

					precedence = (IPickerPrecedence) Activator.CreateInstance(precedenceType, geometry, selectionTolerance);

					return getSelection(geometry).ToList();
				}, progressor);

			if (selection.Count == 0)
			{
				return;
			}

			var orderedSelection = OrderByGeometryDimension(selection).ToList();

			// IPickerPrecedence.GetPickerMode has to be on GUI thread to capture key down events, etc.
			Assert.NotNull(precedence);
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
			[NotNull] Geometry selectionGeometry,
			int selectionTolerance,
			[NotNull] Type precedenceType,
			[NotNull] Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection,
			PickerMode mode = PickerMode.ShowPicker,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			IPickerPrecedence precedence = null;

			List<FeatureSelectionBase> selection =
				await QueuedTaskUtils.Run(() =>
				{
					Geometry geometry = EnsureNonEmpty(selectionGeometry, selectionTolerance);

					precedence = (IPickerPrecedence)Activator.CreateInstance(precedenceType, geometry, selectionTolerance);

					return getSelection(geometry).ToList();
				}, progressor);

			if (selection.Count == 0)
			{
				return;
			}

			var orderedSelection = OrderByGeometryDimension(selection).ToList();

			// IPickerPrecedence.GetPickerMode has to be on GUI thread to capture key down events, etc.
			Assert.NotNull(precedence);

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
			[CanBeNull] CancelableProgressor progressor = null)
		{
			bool areaSelect = false;

			List<FeatureSelectionBase> selection =
				await QueuedTaskUtils.Run(() =>
				{
					// todo daro Implement this in IPickerPrecedence once it's tested.
					// todo daro compare with OneClickToolBase implementation
					areaSelect = ! IsSingleClick(precedence.SelectionGeometry);

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

			Task<IPickableFeatureItem> showPicker =
				QueuedTask.Run(() =>
				{
					var items = PickableItemsFactory
					            .CreateFeatureItems(selection)
					            .ToList();

					Point pickerLocation = MapView.Active.MapToScreen(geometry.Extent.Center);

					return picker.Pick<IPickableFeatureItem>(items, pickerLocation, precedence);
				});

			// show control on GUI thread
			IPickableFeatureItem pickedItem = await showPicker;

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
