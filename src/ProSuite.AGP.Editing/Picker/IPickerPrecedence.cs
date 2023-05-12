using System.Collections.Generic;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerPrecedence
	{
		PickerMode GetPickerMode(int candidateCount, bool areaSelect = false);

		IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items);

		T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem;

		Geometry SelectionGeometry { get; set; }
	}
}
