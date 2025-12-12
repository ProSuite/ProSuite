using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Input;

namespace ProSuite.Commons.AGP.Picker;

public abstract class PickerPrecedenceBase : IPickerPrecedence
{
	[NotNull] private readonly Geometry _sketchGeometry;
	[CanBeNull] private readonly MapPoint _clickPoint;

	[UsedImplicitly]
	protected PickerPrecedenceBase([NotNull] Geometry sketchGeometry,
	                               int tolerance,
	                               Point pickerLocation,
	                               SelectionCombinationMethod? selectionMethod = null)
	{
		_sketchGeometry = sketchGeometry;
		Tolerance = tolerance;
		PickerLocation = pickerLocation;

		IsPointClick = PickerUtils.IsPointClick(sketchGeometry, tolerance, out _clickPoint);
		SpatialRelationship = PickerUtils.GetSpatialRelationship();

		SelectionCombinationMethod = selectionMethod ?? PickerUtils.GetSelectionCombinationMethod();

		AreModifierKeysPressed();
	}

	public PickerPositionPreference PositionPreference { get; set; } =
		PickerPositionPreference.MouseLocationMapOptimized;

	protected List<Key> PressedKeys { get; } = new();

	public int Tolerance { get; }

	public bool IsPointClick { get; }

	public bool AggregateItems =>
		PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl);

	public SelectionCombinationMethod SelectionCombinationMethod { get; }

	public SpatialRelationship SpatialRelationship { get; }

	/// <summary>
	/// Side-effect-free method that returns the geometry which can be used for spatial queries.
	/// For single-click picks, it returns the geometry expanded by the <see cref="Tolerance" />.
	/// This method must be called on the CIM thread.
	/// </summary>
	/// <returns></returns>
	public Geometry GetSelectionGeometry()
	{
		if (IsPointClick)
		{
			Assert.NotNull(_clickPoint);
			return GetSelectionGeometryCore(_clickPoint);
		}

		// Otherwise relational operators and spatial queries return the wrong result
		Geometry simpleGeometry = GeometryUtils.Simplify(_sketchGeometry);
		return Assert.NotNull(simpleGeometry, "Geometry is null");
	}

	[NotNull]
	public virtual IPickableItemsFactory CreateItemsFactory()
	{
		if (IsPointClick)
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
		return items.OrderBy(item => GeometryUtils.GetShapeDimension(item.Geometry.GeometryType))
		            .FirstOrDefault();
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

		if (PickerUtils.GetLowestGeometryDimensionFeatureCount(candidates) > 1)
		{
			modes |= PickerMode.ShowPicker;
		}

		int candidatesCount = candidates.Sum(fs => fs.GetCount());

		if (NoMultiselection && candidatesCount > 1)
		{
			// If area selection: show picker
			if (! IsPointClick)
			{
				modes |= PickerMode.ShowPicker;
			}
			// if not: pick best
		}
		else
		{
			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.LeftAlt))
			{
				modes |= PickerMode.PickAll;
			}

			if (! IsPointClick)
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
		PressedKeys.Clear();
	}

	protected virtual Geometry GetSelectionGeometryCore(Geometry geometry)
	{
		return PickerUtils.ExpandGeometryByPixels(geometry, Tolerance);
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
}
