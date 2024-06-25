using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Picker
{
	public class PickerPrecedence : IPickerPrecedence, IDisposable
	{
		private readonly bool _isSingleClick;

		private Geometry _selectionGeometry;
		private List<Key> _pressedKeys { get; } = new();

		[UsedImplicitly]
		public PickerPrecedence(Geometry selectionGeometry, int selectionTolerance)
		{
			_selectionGeometry = selectionGeometry;
			SelectionTolerance = selectionTolerance;

			_isSingleClick = PickerUtils.IsSingleClick(SelectionGeometry);

			AreModifierKeysPressed();
		}

		public bool IsSingleClick => _isSingleClick;

		public Geometry SelectionGeometry
		{
			get => _selectionGeometry;
			set => _selectionGeometry = value;
		}

		public int SelectionTolerance { get; set; }

		public void AreModifierKeysPressed()
		{
			if (KeyboardUtils.IsAltDown())
			{
				_pressedKeys.Add(Key.LeftAlt);
				_pressedKeys.Add(Key.RightAlt);
			}

			if (KeyboardUtils.IsCtrlDown())
			{
				_pressedKeys.Add(Key.LeftCtrl);
				_pressedKeys.Add(Key.RightCtrl);
			}

			if (KeyboardUtils.IsShiftDown())
			{
				_pressedKeys.Add(Key.LeftShift);
				_pressedKeys.Add(Key.LeftShift);
			}
		}

		public void EnsureGeometryNonEmpty()
		{
			SelectionGeometry = PickerUtils.EnsureNonEmpty(_selectionGeometry, SelectionTolerance);
		}

		public virtual PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                                        bool areaSelect = false)
		{
			if (_pressedKeys.Contains(Key.LeftShift) || _pressedKeys.Contains(Key.RightShift))
			{
				return PickerMode.PickAll;
			}

			if(_pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl))
			{
				return PickerMode.ShowPicker;
			}

			areaSelect = ! _isSingleClick;
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

		public void Dispose()
		{
			SelectionGeometry = null;
			SelectionTolerance = 0;
			_pressedKeys.Clear();
		}
	}
}
