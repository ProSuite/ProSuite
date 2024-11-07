using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.PickerUI;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.AGP.Editing.Picker;

public interface IPickableItemsFactory
{
	IEnumerable<IPickableItem> CreateItems(IEnumerable<FeatureSelectionBase> candidates);

	// todo daro to IPickerPrecedence?
	IPickerViewModel CreateViewModel(Geometry selectionGeometry);
}
