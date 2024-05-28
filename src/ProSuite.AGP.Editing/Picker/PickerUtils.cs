using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

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

		public static Task Show(
			[NotNull] IPickerPrecedence precedence,
			[NotNull] Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			return GetPickerMode() switch
			{
				PickerMode.ShowPicker => ShowPicker(precedence, getSelection),
				PickerMode.PickAll => SelectAll(precedence, progressor, getSelection),
				PickerMode.PickBest => PickBest(precedence, progressor, getSelection),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public static PickerMode GetPickerMode()
		{
			if (KeyboardUtils.IsModifierDown(Key.LeftAlt, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightAlt, exclusive: true))
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierDown(Key.LeftCtrl, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightCtrl, exclusive: true))
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		private static Task PickBest(IPickerPrecedence precedence,
		                             Progressor progressor,
		                             Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection)
		{
			return QueuedTaskUtils.Run(
				() =>
				{
					var selection = getSelection(precedence.SelectionGeometry).ToList();

					if (! selection.Any())
					{
						return;
					}

					// all this code has to be in QueuedTask because
					// IEnumerables are enumerated later
					IEnumerable<IPickableItem> items =
						PickableItemsFactory.CreateFeatureItems(selection);

					var bestPick = precedence.PickBest<IPickableFeatureItem>(items);

					//since SelectionCombinationMethod.New is only applied to
					//the current layer but selections of other layers remain,
					//we manually need to clear all selections first.

					SelectionUtils.SelectFeature(
						bestPick.Layer, SelectionCombinationMethod.New,
						bestPick.Oid);
				}, progressor);
		}

		private static Task SelectAll(
			IPickerPrecedence precedence,
			Progressor progressor,
			Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection)
		{
			return QueuedTaskUtils.Run(() =>
			{
				var selection = getSelection(precedence.SelectionGeometry).ToList();

				SelectionUtils.SelectFeatures(selection,
				                              SelectionCombinationMethod.New);
			}, progressor);
		}

		private static async Task ShowPicker(
			IPickerPrecedence precedence,
			Func<Geometry, IEnumerable<FeatureSelectionBase>> getSelection)
		{
			Geometry geometry = precedence.SelectionGeometry;

			var picker = new PickerService();

			Func<Task<IPickableFeatureItem>> showPicker =
				await QueuedTask.Run(() =>
				{
					IEnumerable<FeatureSelectionBase> selection =
						getSelection(precedence.SelectionGeometry);

					// todo daro reformat
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
	}
}
