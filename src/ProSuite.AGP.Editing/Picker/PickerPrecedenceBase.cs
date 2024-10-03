using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Picker
{
	// todo daro IDisposable not necessary?
	public abstract class PickerPrecedenceBase : IPickerPrecedence, IDisposable
	{
		protected List<Key> PressedKeys { get; } = new();

		[UsedImplicitly]
		protected PickerPrecedenceBase(Geometry sketchGeometry,
		                               int selectionTolerance,
		                               Point pickerLocation)
		{
			SketchGeometry = sketchGeometry;
			SelectionGeometry = sketchGeometry;
			SelectionTolerance = selectionTolerance;
			PickerLocation = pickerLocation;

			IsSingleClick = PickerUtils.IsSingleClick(SelectionGeometry);

			AreModifierKeysPressed();
		}

		/// <summary>
		/// The original sketch geometry, without expansion or simplification.
		/// </summary>
		public Geometry SketchGeometry { get; set; }

		[Obsolete(
			"Use GetSelectionGeometry() which ensures that a single-pick is turned into a polygon")]
		public Geometry SelectionGeometry { get; set; }

		/// <summary>
		/// Side-effect-free method that returns the geometry which can be used for spatial queries.
		/// For single-click picks, it returns the geometry expanded by the <see cref="SelectionTolerance"/>. 
		/// This method must be called on the CIM thread.
		/// </summary>
		/// <returns></returns>
		public Geometry GetSelectionGeometry()
		{
			// TODO: Simplify polygons?
			return PickerUtils.EnsureNonEmpty(SketchGeometry, SelectionTolerance);
		}

		public int SelectionTolerance { get; }

		public bool IsSingleClick { get; }

		public bool AggregateItems =>
			PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl);

		public Point PickerLocation { get; set; }

		public void AreModifierKeysPressed()
		{
			if (KeyboardUtils.IsAltDown())
			{
				PressedKeys.Add(Key.LeftAlt);
				PressedKeys.Add(Key.RightAlt);
			}

			if (KeyboardUtils.IsCtrlDown())
			{
				PressedKeys.Add(Key.LeftCtrl);
				PressedKeys.Add(Key.RightCtrl);
			}

			if (KeyboardUtils.IsShiftDown())
			{
				PressedKeys.Add(Key.LeftShift);
				PressedKeys.Add(Key.RightShift);
			}
		}

		public void EnsureGeometryNonEmpty()
		{
			SelectionGeometry = PickerUtils.EnsureNonEmpty(SelectionGeometry, SelectionTolerance);
		}

		public virtual PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection)
		{
			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.LeftAlt))
			{
				return PickerMode.PickAll;
			}

			if (PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl))
			{
				return PickerMode.ShowPicker;
			}

			bool areaSelect = ! IsSingleClick;
			if (areaSelect)
			{
				return PickerMode.PickAll;
			}

			if (CountLowestShapeDimension(orderedSelection) > 1)
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		public virtual IEnumerable<T> Order<T>(IEnumerable<T> items) where T : IPickableItem
		{
			return items;
		}

		[CanBeNull]
		public virtual T PickBest<T>(IEnumerable<IPickableItem> items)
			where T : class, IPickableItem
		{
			return items.FirstOrDefault() as T;
		}
		
		protected static int CountLowestShapeDimension(
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
			SketchGeometry = null;
			PressedKeys.Clear();
		}
	}
}
