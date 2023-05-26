using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.Editing.Picker
{
	public class StandardPickerPrecedence : IPickerPrecedence
	{
		public Geometry SelectionGeometry { get; set; }

		public PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                                bool areaSelect = false)
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Alt))
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Control))
			{
				return PickerMode.ShowPicker;
			}

			if (areaSelect)
			{
				return PickerMode.PickAll;
			}

			if (CountLowestShapeDimensionFeatures(orderedSelection) > 1)
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items;
		}

		[CanBeNull]
		public T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem
		{
			return items.FirstOrDefault() as T;
		}

		private static int CountLowestShapeDimensionFeatures(
			IEnumerable<FeatureSelectionBase> layerSelection)
		{
			var count = 0;

			int? lastShapeDimension = null;

			foreach (FeatureSelectionBase selection in layerSelection)
			{
				if (lastShapeDimension == null)
				{
					lastShapeDimension = selection.ShapeDimension;

					count += selection.GetCount();

					continue;
				}

				if (lastShapeDimension < selection.ShapeDimension)
				{
					continue;
				}

				count += selection.GetCount();
			}

			return count;
		}
	}
}
