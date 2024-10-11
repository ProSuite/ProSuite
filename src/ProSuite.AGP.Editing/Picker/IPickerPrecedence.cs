using System;
using System.Collections.Generic;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.AGP.Editing.Picker;

public interface IPickerPrecedence : IDisposable
{
	Point PickerLocation { get; set; }
	SpatialRelationship SpatialRelationship { get; }
	SelectionCombinationMethod SelectionCombinationMethod { get; }

	IEnumerable<T> Order<T>(IEnumerable<T> items) where T : IPickableItem;

	IPickableItem PickBest(IEnumerable<IPickableItem> items);

	PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection);

	/// <summary>
	/// Returns the geometry which can be used for spatial queries.
	/// For single-click picks, it returns the geometry expanded by the <see cref="PickerPrecedenceBase.SelectionTolerance" />.
	/// This method must be called on the CIM thread.
	/// </summary>
	/// <returns></returns>
	Geometry GetSelectionGeometry();

	IPickableItemsFactory CreateItemsFactory();

	IPickableItemsFactory CreateItemsFactory<T>() where T : IPickableItem;
}
