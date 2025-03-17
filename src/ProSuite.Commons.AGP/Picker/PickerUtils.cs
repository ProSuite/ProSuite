using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.Commons.AGP.Picker
{
	public static class PickerUtils
	{
		#region move, refactor

		public static Uri GetImagePath(esriGeometryType? geometryType)
		{
			// todo: daro introduce image for unknown type
			//if (geometryType == null)
			//{
			//}
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					return new Uri(
						@"pack://application:,,,/ProSuite.Commons.AGP;component/PickerUI/Images/PointGeometry.bmp");
				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryPolyline:
					return new Uri(
						@"pack://application:,,,/ProSuite.Commons.AGP;component/PickerUI/Images/LineGeometry.bmp");
				case esriGeometryType.esriGeometryPolygon:
					return new Uri(
						@"pack://application:,,,/ProSuite.Commons.AGP;component/PickerUI/Images/PolygonGeometry.bmp",
						UriKind.Absolute);
				case esriGeometryType.esriGeometryMultiPatch:
					return new Uri(
						@"pack://application:,,,/ProSuite.Commons.AGP;component/PickerUI/Images/MultipatchGeometry.bmp");
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

			return selection.OrderBy(fcs => fcs.ShapeDimension);

			// According to documentation, Enumerable.OrderBy uses a stable sort algorithm:
			// https://learn.microsoft.com/dotnet/api/system.linq.enumerable.orderby
			// Therefore, the above should be equivalent to the original code below:
			//return selection
			//       .GroupBy(classSelection => classSelection.ShapeDimension)
			//       .OrderBy(group => group.Key)
			//       .SelectMany(fcs => fcs);
		}

		[Obsolete($"use {nameof(CreatePolygon)}")]
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

			// HasZ, HasM and HasID are inherited from input geometry.
			// There is no need for GeometryUtils.EnsureGeometrySchema()

			return GeometryFactory.CreatePolygon(
				envelope.Expand(envelopeExpansion, envelopeExpansion, false),
				envelope.SpatialReference);
		}

		public static Geometry CreatePolygon(Point screenPoint, int expansionPixels)
		{
			double selectionToleranceMapUnits =
				MapUtils.ConvertScreenPixelToMapLength(MapView.Active, expansionPixels,
				                                       screenPoint);

			// TODO: (daro) revise multiplication by 2
			double envelopeExpansion = selectionToleranceMapUnits * 2;

			MapPoint mapPoint = MapView.Active.ScreenToMap(screenPoint);
			Envelope envelope = mapPoint.Extent;

			// NOTE: MapToScreen in stereo map is sensitive to Z value (Picker location!)

			// Rather than creating a non-Z-aware polygon with elliptic arcs by using buffer...
			//Geometry selectionGeometry =
			//	GeometryEngine.Instance.Buffer(sketchGeometry, bufferDistance);

			// Just expand the envelope
			// .. but PickerViewModel needs a polygon to display selection geometry (press space).

			// HasZ, HasM and HasID are inherited from input geometry.
			// There is no need for GeometryUtils.EnsureGeometrySchema()

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
			var sketchGeometryType = MapView.Active?.GetSketchType() ?? SketchGeometryType.None;
			return sketchGeometryType is SketchGeometryType.Polygon or SketchGeometryType.Lasso
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

		private static async Task<IPickableItem> ShowPickerAsync(
			IPickerPrecedence precedence,
			IEnumerable<FeatureSelectionBase> candidates,
			IPickableItemsFactory factory)
		{
			var items = factory.CreateItems(candidates);
			IPickerViewModel vm = factory.CreateViewModel(precedence.GetSelectionGeometry());

			var picker = new PickerService(precedence);

			return await picker.Pick(items, vm);
		}

		public static async Task<List<IPickableItem>> GetItemsAsync(
			ICollection<FeatureSelectionBase> candidates,
			IPickerPrecedence precedence)
		{
			IPickableItemsFactory itemsFactory = precedence.CreateItemsFactory();

			switch (precedence.GetPickerMode(candidates))
			{
				// TODO: daro return empty list instead of list with null item. Happens
				// when user doesn't pick an item from picker window.
				case PickerMode.ShowPicker:
					IPickableItem pick = await ShowPickerAsync(precedence, candidates, itemsFactory);
					return pick == null ? [] : [pick];

				case PickerMode.PickAll:
					return itemsFactory.CreateItems(candidates).ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = itemsFactory.CreateItems(candidates);
					return [precedence.PickBest(items)];

				case PickerMode.None:
					return [];
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static async Task<List<T>> GetItemsAsync<T>(
			IEnumerable<FeatureSelectionBase> candidates,
			IPickerPrecedence precedence, PickerMode pickerMode)
			where T : IPickableItem
		{
			IPickableItemsFactory itemsFactory = precedence.CreateItemsFactory<T>();

			switch (pickerMode)
			{
				// Return empty list instead of list with null item. Happens
				// when user doesn't pick an item from picker window.
				case PickerMode.ShowPicker:
					IPickableItem pick = await ShowPickerAsync(precedence, candidates, itemsFactory);
					return pick == null ? [] : [(T) pick];

				case PickerMode.PickAll:
					return itemsFactory.CreateItems(candidates).OfType<T>().ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = itemsFactory.CreateItems(candidates);
					return [(T) precedence.PickBest(items)];

				case PickerMode.None:
					return [];
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		public static void Select(List<IPickableItem> items,
		                          SelectionCombinationMethod selectionMethod)
		{
			// No candidate (user clicked into empty space):
			if (items.Count == 0)
			{
				ClearSelection();
				return;
			}

			// New selection: clear the selection on the map level, NOT on the layer level
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				ClearSelection();
			}

			foreach (IPickableFeatureClassItem item in items.OfType<IPickableFeatureClassItem>())
			{
				// Important to loop over each layer, they could have different definition queries!
				foreach (BasicFeatureLayer layer in item.Layers)
				{
					SelectionUtils.SelectRows(layer, selectionMethod, item.Oids.ToList());
				}
			}

			foreach (var itemsByLayer in items.OfType<IPickableFeatureItem>().GroupBy(item => item.Layer))
			{
				BasicFeatureLayer layer = itemsByLayer.Key;
				List<long> oids = itemsByLayer.Select(item => item.Oid).ToList();

				SelectionUtils.SelectRows(layer, selectionMethod, oids);
			}
		}

		private static void ClearSelection()
		{
			Map map = MapView.Active?.Map;
			SelectionUtils.ClearSelection(map);
		}
	}
}
