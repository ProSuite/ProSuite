using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

		public static Geometry ExpandGeometryByPixels(Geometry sketchGeometry, int tolerancePixels)
		{
			Envelope envelope = sketchGeometry.Extent;
			MapPoint center = envelope.Center;

			double toleranceMapUnits =
				MapUtils.ConvertScreenPixelToMapLength(MapView.Active, tolerancePixels, center);

			double expansion = toleranceMapUnits * 2;

			// NOTE: MapToScreen in stereo map is sensitive to Z value (Picker location!)
			// HasZ, HasM and HasID are inherited from input geometry.
			// There is no need for GeometryUtils.EnsureGeometrySchema()

			return GeometryFactory.CreatePolygon(
				envelope.Expand(expansion, expansion, false),
				envelope.SpatialReference);
		}

		public static bool IsPointClick(Geometry geometry, double tolerance,
		                                out MapPoint clickPoint)
		{
			clickPoint = null;

			if (geometry is null) return false;
			if (geometry.IsEmpty) return false;

			if (geometry is MapPoint point)
			{
				clickPoint = point;
				return true;
			}

			var extent = geometry.Extent;
			if (extent.Length < tolerance)
			{
				clickPoint = extent.Center;
				return true;
			}

			return false;
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

		public static async Task<IPickableItem> ShowPickerAsync(
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
				// Return empty list instead of list with null item. Happens
				// when user doesn't pick an item from picker window.
				case PickerMode.ShowPicker:
					IPickableItem pick =
						await ShowPickerAsync(precedence, candidates, itemsFactory);
					return pick == null
						       ? new List<IPickableItem>()
						       : new List<IPickableItem> { pick };

				case PickerMode.PickAll:
					return itemsFactory.CreateItems(candidates).ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = itemsFactory.CreateItems(candidates);
					return new List<IPickableItem> { precedence.PickBest(items) };

				case PickerMode.None:
					return new List<IPickableItem>();
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
					IPickableItem pick =
						await ShowPickerAsync(precedence, candidates, itemsFactory);
					return pick == null ? new List<T>() : new List<T> { (T) pick };

				case PickerMode.PickAll:
					return itemsFactory.CreateItems(candidates).OfType<T>().ToList();

				case PickerMode.PickBest:
					IEnumerable<IPickableItem> items = itemsFactory.CreateItems(candidates);
					return new List<T> { (T) precedence.PickBest(items) };

				case PickerMode.None:
					return new List<T>();
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

			foreach (var itemsByLayer in items.OfType<IPickableFeatureItem>()
			                                  .GroupBy(item => item.Layer))
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
