using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Geometry = ArcGIS.Core.Geometry.Geometry;

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

		public static async Task Show(IPickerPrecedence precedence,
		                              CancelableProgressor progressor,
		                              Func<Geometry, IEnumerable<FeatureSelectionBase>>
			                              findFeatureSelection)
		{
			switch (precedence.Mode)
			{
				case PickerMode.ShowPicker:

					await ShowPicker(precedence, findFeatureSelection);

					break;
				case PickerMode.PickAll:

					await SelectAll(precedence, progressor, findFeatureSelection);
					break;
				case PickerMode.PickBest:

					await PickBest(precedence, progressor, findFeatureSelection);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static async Task PickBest(IPickerPrecedence precedence,
		                                   CancelableProgressor progressor,
		                                   Func<Geometry, IEnumerable<FeatureSelectionBase>>
			                                   findFeatureSelection)
		{
			await QueuedTask.Run(
				() =>
				{
					var selection = findFeatureSelection(precedence.SelectionGeometry).ToList();

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
						bestPick.Oid, clearExistingSelection: true);
				}, progressor);
		}

		private static async Task SelectAll(IPickerPrecedence precedence,
		                                    CancelableProgressor progressor,
		                                    Func<Geometry, IEnumerable<FeatureSelectionBase>>
			                                    findFeatureSelection)
		{
			await QueuedTask.Run(() =>
			{
				var selection = findFeatureSelection(precedence.SelectionGeometry).ToList();

				SelectionUtils.SelectFeatures(selection,
				                              SelectionCombinationMethod.New,
				                              clearExistingSelection: true);
			}, progressor);
		}

		private static async Task ShowPicker(IPickerPrecedence precedence, Func<Geometry, IEnumerable<FeatureSelectionBase>> findFeatureSelection)
		{
			Geometry geometry = precedence.SelectionGeometry;

			var picker = new PickerService();

			Func<Task<IPickableFeatureItem>> showControlOrPickBest =
				await QueuedTask.Run(() =>
				{
					IEnumerable<FeatureSelectionBase> selection =
						findFeatureSelection(precedence.SelectionGeometry);

					var items = PickableItemsFactory.CreateFeatureItems(OrderByGeometryDimension(selection))
					                                .ToList();

					Point pickerLocation = MapView.Active.MapToScreen(geometry.Extent.Center);

					return picker.Pick<IPickableFeatureItem>(
						items, pickerLocation, precedence);
				});

			// show control on GUI thread
			IPickableFeatureItem pickedItem = await showControlOrPickBest();

			if (pickedItem == null)
			{
				return;
			}

			await QueuedTask.Run(() =>
			{
				SelectionUtils.SelectFeature(pickedItem.Layer,
				                             SelectionCombinationMethod.New,
				                             pickedItem.Oid, true);
			});
		}

		public static async Task Show(IPickerPrecedence precedence, List<IPickableItem> items, Func<IEnumerable<FeatureSelectionBase>> getSelection)
		{
			switch (precedence.Mode)
			{
				case PickerMode.ShowPicker:

					Geometry geometry = precedence.SelectionGeometry;

					var picker = new PickerService();

					Func<Task<IPickableFeatureItem>> showControlOrPickBest =
						await QueuedTask.Run(() =>
						{
							Point pickerLocation = MapView.Active.MapToScreen(geometry.Extent.Center);

							return picker.Pick<IPickableFeatureItem>(
								items, pickerLocation, precedence);
						});

					// show control on GUI thread
					IPickableFeatureItem pickedItem = await showControlOrPickBest();

					if (pickedItem == null)
					{
						return;
					}

					await QueuedTask.Run(() =>
					{
						SelectionUtils.SelectFeature(pickedItem.Layer,
						                             SelectionCombinationMethod.New,
						                             pickedItem.Oid, true);
					});

					break;
				case PickerMode.PickAll:
					break;
				case PickerMode.PickBest:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
