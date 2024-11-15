using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.PickerUI;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickerUtils
	{
		#region move, refactor

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

		public static Geometry EnsureNonEmpty([NotNull] Geometry sketch, int tolerancePixel)
		{
			return IsSingleClick(sketch)
				       ? CreateSinglePickGeometry(sketch, tolerancePixel)
				       : sketch;
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

		public static Geometry ExpandGeometryByPixels(Geometry sketchGeometry,
		                                              int selectionTolerancePixels)
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

		public static bool IsSingleClick(Geometry sketch)
		{
			return ! (sketch.Extent.Width > 0 || sketch.Extent.Height > 0);
		}

		public static SpatialRelationship GetSpatialRelationship()
		{
			SketchGeometryType sketchGeometryType = ToolUtils.GetSketchGeometryType();
			return sketchGeometryType == SketchGeometryType.Polygon ||
			       sketchGeometryType == SketchGeometryType.Lasso
				       ? SpatialRelationship.Contains
				       : SpatialRelationship.Intersects;
		}

		public static SelectionCombinationMethod GetSelectionCombinationMethod()
		{
			return KeyboardUtils.IsShiftDown()
				       ? SelectionCombinationMethod.XOR
				       : SelectionCombinationMethod.New;
		}

		#endregion

		#region Show Picker

		public static async Task<IPickableItem> ShowPickerAsync(
			IPickerPrecedence precedence,
			IEnumerable<FeatureSelectionBase> orderedSelection)
		{
			return await ShowPickerAsync(precedence, orderedSelection,
			                             precedence.CreateItemsFactory());
		}

		public static async Task<IPickableItem> ShowPickerAsync<T>(
			IPickerPrecedence precedence,
			IEnumerable<FeatureSelectionBase> orderedSelection)
			where T : IPickableItem
		{
			return await ShowPickerAsync(precedence, orderedSelection,
			                             precedence.CreateItemsFactory<T>());
		}

		private static async Task<IPickableItem> ShowPickerAsync(IPickerPrecedence precedence,
		                                                         IEnumerable<FeatureSelectionBase>
			                                                         orderedSelection,
		                                                         IPickableItemsFactory factory)
		{
			var items = factory.CreateItems(orderedSelection).ToList();
			IPickerViewModel vm = factory.CreateViewModel(precedence.GetSelectionGeometry());

			var picker = new PickerService(precedence);

			return await picker.Pick(items, vm);
		}

		public static async Task<List<IPickableItem>> GetItems(
			IEnumerable<FeatureSelectionBase> candidates,
			IPickerPrecedence precedence)
		{
			var ordered = OrderByGeometryDimension(candidates).ToList();

			switch (GetPickerMode(precedence, ordered))
			{
				case PickerMode.ShowPicker:
					IPickableItem pick = await ShowPickerAsync(precedence, ordered);
					return new List<IPickableItem> { pick };

				case PickerMode.PickAll:
					return new PickableFeatureClassItemsFactory().CreateItems(ordered).ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = precedence.CreateItemsFactory()
					                                             .CreateItems(ordered);
					return new List<IPickableItem> { precedence.PickBest(items) };

				case null:
					return new List<IPickableItem>(0);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static async Task<List<IPickableItem>> GetItems<T>(
			IEnumerable<FeatureSelectionBase> candidates,
			IPickerPrecedence precedence, PickerMode? pickerMode)
			where T : IPickableItem
		{
			var ordered = OrderByGeometryDimension(candidates).ToList();

			switch (pickerMode)
			{
				case PickerMode.ShowPicker:
					IPickableItem pick = await ShowPickerAsync<T>(precedence, ordered);
					return new List<IPickableItem> { pick };

				case PickerMode.PickAll:
					return new PickableFeatureClassItemsFactory().CreateItems(ordered).ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = precedence.CreateItemsFactory()
					                                             .CreateItems(ordered);
					return new List<IPickableItem> { precedence.PickBest(items) };

				case null:
					return new List<IPickableItem>(0);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		public static void Select(List<IPickableItem> items,
		                          SelectionCombinationMethod selectionMethod)
		{
			foreach (IPickableFeatureClassItem item in items.OfType<IPickableFeatureClassItem>())
			{
				SelectionUtils.SelectRows(item.Layers.First(),
				                          selectionMethod, item.Oids);
			}

			foreach (IPickableFeatureItem item in items.OfType<IPickableFeatureItem>())
			{
				SelectionUtils.SelectRows(item.Layer,
				                          selectionMethod, new[] { item.Oid });
			}
		}

		private static PickerMode? GetPickerMode(
			[NotNull] IPickerPrecedence precedence, ICollection<FeatureSelectionBase> candidates)
		{
			SelectionCombinationMethod selectionMethod = precedence.SelectionCombinationMethod;

			if (! candidates.Any() && selectionMethod == SelectionCombinationMethod.XOR)
			{
				// No addition to and no removal from selection
				return null;
			}

			if (! candidates.Any())
			{
				// No candidate (user clicked into empty space):
				ClearSelection();
				return null;
			}

			// Clear the selection on the map level, NOT on the layer level
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				ClearSelection();
			}

			return precedence.GetPickerMode(candidates);
		}

		private static void ClearSelection()
		{
			Map map = MapView.Active?.Map;
			SelectionUtils.ClearSelection(map);
		}
	}
}
