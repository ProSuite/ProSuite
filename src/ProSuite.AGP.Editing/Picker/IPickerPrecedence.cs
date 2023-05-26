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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="orderedSelection">Has to be ordered!</param>
		/// <param name="areaSelect">Select by area.</param>
		/// <returns></returns>
		PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                         bool areaSelect = false);
	}
}
