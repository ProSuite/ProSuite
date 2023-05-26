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
			if (KeyboardUtils.IsModifierPressed(Keys.Alt) || areaSelect)
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Control))
			{
				return PickerMode.ShowPicker;
			}

			ICollection<FeatureSelectionBase> featureSelectionBases =
				GetLowestEqualShapeDimension(orderedSelection);

			return featureSelectionBases.Count > 1
				       ? PickerMode.ShowPicker
				       : PickerMode.PickBest;
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

		private static ICollection<FeatureSelectionBase> GetLowestEqualShapeDimension(
			IEnumerable<FeatureSelectionBase> layerSelection)
		{
			var result = new List<FeatureSelectionBase>();

			int? lastShapeDimension = null;

			foreach (FeatureSelectionBase selection in layerSelection)
			{
				if (lastShapeDimension == null)
				{
					lastShapeDimension = selection.ShapeDimension;

					result.Add(selection);

					continue;
				}

				if (lastShapeDimension < selection.ShapeDimension)
				{
					continue;
				}

				result.Add(selection);
			}

			return result;
		}
	}
}
