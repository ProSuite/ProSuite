using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerPrecedence
	{
		IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items);

		T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem;

		Geometry SelectionGeometry { get; set; }
		int SelectionTolerance { get; }

		PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                         bool areaSelect = false);
	}
}
