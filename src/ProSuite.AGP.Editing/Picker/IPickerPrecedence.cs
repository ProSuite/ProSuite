using System;
using System.Collections.Generic;
using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerPrecedence : IDisposable
	{
		IEnumerable<T> Order<T>(IEnumerable<T> items) where T : IPickableItem;

		T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem;

		int SelectionTolerance { get; }

		bool IsSingleClick { get; }
		bool AggregateItems { get; }
		Point PickerLocation { get; set; }

		PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection);

		/// <summary>
		/// Returns the geometry which can be used for spatial queries.
		/// For single-click picks, it returns the geometry expanded by the <see cref="PickerPrecedenceBase.SelectionTolerance"/>. 
		/// This method must be called on the CIM thread.
		/// </summary>
		/// <returns></returns>
		Geometry GetSelectionGeometry();
	}
}
