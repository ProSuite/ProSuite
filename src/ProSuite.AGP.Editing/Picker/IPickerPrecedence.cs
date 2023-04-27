using System.Collections.Generic;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerPrecedence
	{
		IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items);

		IPickableItem PickBest(IEnumerable<IPickableItem> items);

		Geometry SelectionGeometry { get; set; }
	}
}
