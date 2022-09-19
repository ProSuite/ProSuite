using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public interface IObjectSubtypeObserver : IViewObserver
	{
		[NotNull]
		IList<ObjectSubtypeCriterionTableRow> AddTargetClicked();

		[NotNull]
		IList<ObjectAttribute> RemoveTargetClicked();

		void TargetSelectionChanged();
	}
}
