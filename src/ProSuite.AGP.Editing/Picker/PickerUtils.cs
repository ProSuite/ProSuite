using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickerUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

		private static Geometry ExpandGeometryByPixels(Geometry sketchGeometry,
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

		#endregion

		#region Show Picker

		// TODO daro: change signature to .. Func<IEnumerable<FeatureSelectionBase>> because CancelableProgressor should be null?
		public static async Task ShowAsync(
			[NotNull] IPickerPrecedence precedence,
			[NotNull] Func<Geometry, SpatialRelationship, CancelableProgressor,
				IEnumerable<FeatureSelectionBase>> getCandidates)
		{
			SelectionCombinationMethod selectionMethod =
				KeyboardUtils.IsShiftDown()
					? SelectionCombinationMethod.XOR
					: SelectionCombinationMethod.New;

			// Has to be on GUI thread
			bool altDown = KeyboardUtils.IsAltDown();

			// Polygon-selection allows for more accurate selection in feature-dense areas using contains
			SketchGeometryType sketchGeometryType = ToolUtils.GetSketchGeometryType();
			SpatialRelationship spatialRelationship =
				sketchGeometryType == SketchGeometryType.Polygon ||
				sketchGeometryType == SketchGeometryType.Lasso
					? SpatialRelationship.Contains
					: SpatialRelationship.Intersects;

			await QueuedTaskUtils.Run(async () =>
			{
				precedence.EnsureGeometryNonEmpty();

				// NOTE daro: passing in a delayed cancellable progressor in conjunction with
				// picker window crashes Pro. A non-delayed progressor works fine.
				const CancelableProgressor progressor = null;
				var featureSelection = getCandidates(precedence.SelectionGeometry,
				                                     spatialRelationship,
				                                     progressor).ToList();
				if (altDown)
				{
					await SelectCandidates(precedence, featureSelection,
					                       selectionMethod, PickerMode.PickAll);
				}
				else
				{
					await SelectCandidates(precedence, featureSelection, selectionMethod);
				}
			});
		}

		public static async Task ShowAsync(
			[NotNull] IPickerPrecedence precedence,
			[NotNull] Func<Geometry, SpatialRelationship, CancelableProgressor,
				IEnumerable<FeatureSelectionBase>> getCandidates,
			PickerMode pickerMode)
		{
			SelectionCombinationMethod selectionMethod =
				KeyboardUtils.IsShiftDown()
					? SelectionCombinationMethod.XOR
					: SelectionCombinationMethod.New;

			// Polygon-selection allows for more accurate selection in feature-dense areas using contains
			SketchGeometryType sketchGeometryType = ToolUtils.GetSketchGeometryType();
			SpatialRelationship spatialRelationship =
				sketchGeometryType == SketchGeometryType.Polygon ||
				sketchGeometryType == SketchGeometryType.Lasso
					? SpatialRelationship.Contains
					: SpatialRelationship.Intersects;

			await QueuedTaskUtils.Run(async () =>
			{
				precedence.EnsureGeometryNonEmpty();

				// NOTE daro: passing in a delayed cancellable progressor in conjunction with
				// picker window crashes Pro. A non-delayed progressor works fine.
				const CancelableProgressor progressor = null;
				var featureSelection = getCandidates(precedence.SelectionGeometry,
				                                     spatialRelationship,
				                                     progressor).ToList();

				await SelectCandidates(precedence, featureSelection, selectionMethod, pickerMode);
			});
		}

		/// <summary>
		/// Shows the picker.
		/// </summary>
		/// <typeparam name="T">IPickableItem</typeparam>
		/// <param name="precedence">Your picker precedence implementation.</param>
		/// <param name="orderedSelection">Feature selection, can be ordered by dimension, e.g. points, lines, polygons.</param>
		/// <returns>the picked item. Can be null if user hit ESC!</returns>
		public static async Task<T> ShowAsync<T>(
			IPickerPrecedence precedence,
			IEnumerable<FeatureSelectionBase> orderedSelection)
			where T : class, IPickableItem
		{
			var picker = new PickerService();

			bool isRequestingFeatures =
				typeof(IPickableFeatureItem).IsAssignableFrom(typeof(T));

			bool isRequestingFeatureClasses =
				typeof(IPickableFeatureClassItem).IsAssignableFrom(typeof(T));

			if (isRequestingFeatures || (precedence.IsSingleClick && ! isRequestingFeatureClasses))
			{
				var items = PickableItemsFactory
				            .CreateFeatureItems(orderedSelection)
				            .ToList();

				return (T) await picker.Pick<IPickableFeatureItem>(items, precedence);
			}
			else
			{
				var items = PickableItemsFactory
				            .CreateFeatureClassItems(orderedSelection)
				            .ToList();

				return (T) await picker.Pick<IPickableFeatureClassItem>(items, precedence);
			}
		}

		/// <summary>
		/// Shows the picker.
		/// </summary>
		/// <param name="precedence">Your picker precedence implementation.</param>
		/// <param name="orderedSelection">Feature selection, can be ordered by dimension, e.g. points, lines, polygons.</param>
		/// <returns>the picked item. Can be null if user hit ESC!</returns>
		public static async Task<IPickableItem> ShowAsync(
			IPickerPrecedence precedence,
			IEnumerable<FeatureSelectionBase> orderedSelection)
		{
			if (precedence.IsSingleClick)
			{
				return await ShowAsync<IPickableFeatureItem>(precedence, orderedSelection);
			}

			return await ShowAsync<IPickableFeatureClassItem>(precedence, orderedSelection);
		}

		#endregion

		private static async Task SelectCandidates(IPickerPrecedence precedence,
		                                           List<FeatureSelectionBase> featureSelection,
		                                           SelectionCombinationMethod selectionMethod)
		{
			if (! featureSelection.Any())
			{
				if (selectionMethod == SelectionCombinationMethod.XOR)
				{
					// No addition to and no removal from selection
					return;
				}

				// No candidate (user clicked into empty space):
				ClearSelection();
				return;
			}

			// Clear the selection on the map level, NOT on the layer level
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				ClearSelection();
			}

			var orderedSelection = OrderByGeometryDimension(featureSelection).ToList();

			switch (precedence.GetPickerMode(orderedSelection))
			{
				case PickerMode.ShowPicker:

					IPickableItem pickedItem = await ShowAsync(precedence, orderedSelection);

					if (pickedItem is IPickableFeatureItem featureItem)
					{
						SelectFeature(featureItem, selectionMethod);
					}
					else if (pickedItem is IPickableFeatureClassItem featureClassItem)
					{
						SelectFeatures(featureClassItem, selectionMethod);
					}
					else if (pickedItem == null)
					{
						return;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							$"Unkown pickable item type {pickedItem.GetType()}");
					}

					return;

				case PickerMode.PickAll:
					SelectionUtils.SelectFeatures(orderedSelection, selectionMethod);
					return;
				case PickerMode.PickBest:
					SelectBestPick(precedence, orderedSelection, selectionMethod);
					return;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static async Task SelectCandidates(IPickerPrecedence precedence,
		                                           List<FeatureSelectionBase> featureSelection,
		                                           SelectionCombinationMethod selectionMethod,
		                                           PickerMode pickerMode)
		{
			if (! featureSelection.Any())
			{
				if (selectionMethod == SelectionCombinationMethod.XOR)
				{
					// No addition to and no removal from selection
					return;
				}

				// No candidate (user clicked into empty space):
				ClearSelection();
				return;
			}

			// Clear the selection on the map level, NOT on the layer level
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				ClearSelection();
			}

			var orderedSelection = OrderByGeometryDimension(featureSelection).ToList();

			switch (pickerMode)
			{
				case PickerMode.ShowPicker:

					IPickableItem pickedItem = await ShowAsync(precedence, orderedSelection);

					if (pickedItem is IPickableFeatureItem featureItem)
					{
						SelectFeature(featureItem, selectionMethod);
					}
					else if (pickedItem is IPickableFeatureClassItem featureClassItem)
					{
						SelectFeatures(featureClassItem, selectionMethod);
					}
					else if (pickedItem == null)
					{
						return;
					}
					else
					{
						throw new ArgumentOutOfRangeException(
							$"Unkown pickable item type {pickedItem.GetType()}");
					}

					return;

				case PickerMode.PickAll:
					SelectionUtils.SelectFeatures(orderedSelection, selectionMethod);
					return;
				case PickerMode.PickBest:
					SelectBestPick(precedence, orderedSelection, selectionMethod);
					return;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void SelectBestPick(IPickerPrecedence precedence,
		                                   IEnumerable<FeatureSelectionBase> orderedSelection,
		                                   SelectionCombinationMethod selectionMethod)
		{
			var bestPick =
				precedence.PickBest<IPickableFeatureItem>(
					PickableItemsFactory.CreateFeatureItems(orderedSelection));

			SelectFeature(bestPick, selectionMethod);
		}

		private static void SelectFeature(IPickableFeatureItem pickedItem,
		                                  SelectionCombinationMethod selectionMethod)
		{
			SelectionUtils.SelectFeature(pickedItem.Layer,
			                             selectionMethod,
			                             pickedItem.Oid);
		}

		private static void SelectFeatures(IPickableFeatureClassItem pickedItem,
		                                   SelectionCombinationMethod selectionMethod)
		{
			var featureClassSelections =
				pickedItem.Layers
				          .Select(layer =>
					                  new OidSelection(layer,
					                                   pickedItem.Oids.ToList(),
					                                   MapView.Active.Map.SpatialReference))
				          .Cast<FeatureSelectionBase>()
				          .ToList();

			SelectionUtils.SelectFeatures(featureClassSelections, selectionMethod);
		}

		private static void ClearSelection()
		{
			Map map = MapView.Active?.Map;
			SelectionUtils.ClearSelection(map);
		}
	}
}
