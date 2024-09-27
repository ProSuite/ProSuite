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

		Geometry SelectionGeometry { get; set; }
		bool IsSingleClick { get; }
		bool AggregateItems { get; }
		Point PickerLocation { get; set; }

		PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection);

		void EnsureGeometryNonEmpty();
	}
}
