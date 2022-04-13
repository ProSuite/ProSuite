using System.Collections.Generic;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public interface IObjectSubtypeView : IBoundView
	<ObjectSubtype,
		IObjectSubtypeObserver>
	{
		void BindToCriteria(
			SortableBindingList<ObjectSubtypeCriterionTableRow> criteriumItems);

		IList<ObjectSubtypeCriterionTableRow> GetSelectedCriteria();

		bool HasSelectedCriteria { get; }

		bool RemoveCriteriaEnabled { get; set; }
	}
}
