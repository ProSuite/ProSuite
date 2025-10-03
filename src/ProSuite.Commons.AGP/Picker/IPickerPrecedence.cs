using System;
using System.Collections.Generic;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.Commons.AGP.Picker;

public interface IPickerPrecedence : IDisposable
{
	Point PickerLocation { get; set; }
	SpatialRelationship SpatialRelationship { get; }
	SelectionCombinationMethod SelectionCombinationMethod { get; }
	bool NoMultiselection { get; set; }

	IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items);

	IPickableItem PickBest(IEnumerable<IPickableItem> items);

	PickerMode GetPickerMode(ICollection<FeatureSelectionBase> candidates);
	/// <summary>
	/// Returns the geometry which can be used for spatial queries.
	/// For single-click picks, it returns the geometry expanded by the PickerPrecedenceBase.Tolerance.
	/// This method must be called on the CIM thread.
	/// </summary>
	/// <returns></returns>
	Geometry GetSelectionGeometry();

	IPickableItemsFactory CreateItemsFactory();

	IPickableItemsFactory CreateItemsFactory<T>() where T : IPickableItem;
}
