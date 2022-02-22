using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.PickerUI
{
	public static class PickerUtils
	{
		/// <summary>
		/// Picks a single feature from the list of features in the provided selection sets.
		/// Must be called on the UI thread.
		/// </summary>
		/// <param name="selectionByLayer"></param>
		/// <param name="pickerWindowLocation"></param>
		/// <returns></returns>
		public static async Task<PickableFeatureItem> PickSingleFeatureAsync(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByLayer,
			Point pickerWindowLocation)
		{
			IList<IPickableItem> pickableItems =
				await QueuedTaskUtils.Run(
					delegate
					{
						selectionByLayer =
							GeometryReducer.ReduceByGeometryDimension(selectionByLayer)
							               .ToList();

						return PickableItemsFactory.CreateFeatureItems(selectionByLayer);
					});

			var picker = new Picker(pickableItems, pickerWindowLocation);

			// Must not be called from a background Task!
			return await picker.PickSingle() as PickableFeatureItem;
		}

		/// <summary>
		/// Picks a single feature from the list of provided feature classes.
		/// Must be called on the UI thread.
		/// </summary>
		/// <param name="selectionByLayer"></param>
		/// <param name="pickerWindowLocation"></param>
		/// <returns></returns>
		public static async Task<PickableFeatureClassItem> PickSingleFeatureClassItemsAsync(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByLayer,
			Point pickerWindowLocation)
		{
			IList<IPickableItem> pickingCandidates =
				await QueuedTaskUtils.Run(
					() => PickableItemsFactory.CreateFeatureClassItems(
						selectionByLayer));

			var picker = new Picker(pickingCandidates, pickerWindowLocation);

			return await picker.PickSingle() as PickableFeatureClassItem;
		}
	}
}
