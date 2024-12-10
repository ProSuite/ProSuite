using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.Commons.AGP.Picker;

public interface IPickableItemsFactory
{
	IEnumerable<IPickableItem> CreateItems(IEnumerable<FeatureSelectionBase> candidates);

	// todo: daro to IPickerPrecedence?
	IPickerViewModel CreateViewModel(Geometry selectionGeometry);
}
