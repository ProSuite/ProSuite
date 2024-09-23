using System;
using System.Collections.Generic;
using System.Windows;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickerPrecedence : IDisposable
	{
		IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items);

		T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem;

		[Obsolete(
			"Use GetSelectionGeometry() which ensures that a single-pick is turned into a polygon")]
		Geometry SelectionGeometry { get; set; }

		int SelectionTolerance { get; }

		bool IsSingleClick { get; }

		Point PickerLocation { get; set; }

		PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                         bool areaSelect = false);

		[Obsolete("Not necessary if using GetSelectionGeometry()")]
		void EnsureGeometryNonEmpty();

		/// <summary>
		/// Returns the geometry which can be used for spatial queries.
		/// For single-click picks, it returns the geometry expanded by the <see cref="PickerPrecedenceBase.SelectionTolerance"/>. 
		/// This method must be called on the CIM thread.
		/// </summary>
		/// <returns></returns>
		Geometry GetSelectionGeometry();
	}
}
