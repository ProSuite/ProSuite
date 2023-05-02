using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.Editing.Picker
{
	public class StandardPickerPrecedence : IPickerPrecedence
	{
		public Geometry SelectionGeometry { get; set; }

		public PickerMode GetPickerMode(int candidateCount, bool areaSelect = false)
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Alt))
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Control))
			{
				return PickerMode.ShowPicker;
			}

			if (candidateCount > 1 && areaSelect)
			{
				return PickerMode.PickAll;
			}

			if (candidateCount > 1)
			{
				return PickerMode.ShowPicker;
			}

			if (areaSelect)
			{
				return PickerMode.PickAll;
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
	}
}
