using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.Commons.AGP.Picker;

public abstract class PickerPrecedenceBase : IPickerPrecedence
{
	[UsedImplicitly]
	protected PickerPrecedenceBase([NotNull] Geometry sketchGeometry,
	                               int selectionTolerance,
	                               Point pickerLocation,
	                               SelectionCombinationMethod? selectionMethod = null)
	{
		SketchGeometry = sketchGeometry;
		SelectionTolerance = selectionTolerance;
		PickerLocation = pickerLocation;

		IsSingleClick = PickerUtils.IsSingleClick(sketchGeometry);
		SpatialRelationship = PickerUtils.GetSpatialRelationship();

		SelectionCombinationMethod = selectionMethod ?? PickerUtils.GetSelectionCombinationMethod();

		AreModifierKeysPressed();
	}

	protected List<Key> PressedKeys { get; } = new();

	/// <summary>
	/// The original sketch geometry, without expansion or simplification.
	/// </summary>
	private Geometry SketchGeometry { get; set; }

	public int SelectionTolerance { get; }

	public bool IsSingleClick { get; }

	public bool AggregateItems =>
		PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl);

	public SelectionCombinationMethod SelectionCombinationMethod { get; }

	public SpatialRelationship SpatialRelationship { get; }

	/// <summary>
	/// Side-effect-free method that returns the geometry which can be used for spatial queries.
	/// For single-click picks, it returns the geometry expanded by the <see cref="SelectionTolerance" />.
	/// This method must be called on the CIM thread.
	/// </summary>
	/// <returns></returns>
	public Geometry GetSelectionGeometry()
	{
		if (! IsSingleClick)
		{
			return SketchGeometry;
		}

		return GetSelectionGeometryCore(PickerLocation);
	}

	[NotNull]
	public virtual IPickableItemsFactory CreateItemsFactory()
	{
		if (IsSingleClick)
		{
			return CreateItemsFactory<IPickableFeatureItem>();
		}

		if (AggregateItems)
		{
			return CreateItemsFactory<IPickableFeatureClassItem>();
		}

		return CreateItemsFactory<IPickableFeatureItem>();
	}

	[NotNull]
	public virtual IPickableItemsFactory CreateItemsFactory<T>() where T : IPickableItem
	{
		bool isRequestingFeatures =
			typeof(IPickableFeatureItem).IsAssignableFrom(typeof(T));

		bool isRequestingFeatureClasses =
			typeof(IPickableFeatureClassItem).IsAssignableFrom(typeof(T));

		if (isRequestingFeatures)
		{
			return new PickableFeatureItemsFactory();
		}

		if (isRequestingFeatureClasses)
		{
			return new PickableFeatureClassItemsFactory();
		}

		throw new ArgumentOutOfRangeException($"Unkown type of {nameof(IPickableItem)}");
	}

	[CanBeNull]
	public virtual IPickableItem PickBest(IEnumerable<IPickableItem> items)
	{
		return items.FirstOrDefault();
	}

	public Point PickerLocation { get; set; }

	public bool NoMultiselection { get; set; }

	public virtual PickerMode GetPickerMode(ICollection<FeatureSelectionBase> candidates)
	{
		if (candidates.Count == 0)
		{
			return PickerMode.None;
		}

		var modes = PickerMode.PickBest;

		if (PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl))
		{
			// always show picker if CTRL pressed
			return PickerMode.ShowPicker;
		}

		if (NoMultiselection && candidates.Sum(fs => fs.GetCount()) > 1)
		{
			// if area selection: show picker
			if (! IsSingleClick)
			{
				modes |= PickerMode.ShowPicker;
			}
			// ...if not: pick best
		}
		else
		{
			if (CountLowestGeometryDimension(PickerUtils.OrderByGeometryDimension(candidates)) > 1)
			{
				modes |= PickerMode.ShowPicker;
			}

			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.LeftAlt))
			{
				modes |= PickerMode.PickAll;
			}

			if (! IsSingleClick)
			{
				modes |= PickerMode.PickAll;
			}
		}

		// the higher mode wins
		var result = PickerMode.PickBest;

		if ((modes & PickerMode.ShowPicker) != 0)
		{
			result = PickerMode.ShowPicker;
		}

		if ((modes & PickerMode.PickAll) != 0)
		{
			result = PickerMode.PickAll;
		}

		return result;
	}

	public virtual IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
	{
		return items;
	}

	public void Dispose()
	{
		SketchGeometry = null;
		PressedKeys.Clear();
	}

	protected virtual Geometry GetSelectionGeometryCore(Point screenPoint)
	{
		return PickerUtils.CreatePolygon(screenPoint, SelectionTolerance);
	}

	private void AreModifierKeysPressed()
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

	protected static int CountLowestGeometryDimension(
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
