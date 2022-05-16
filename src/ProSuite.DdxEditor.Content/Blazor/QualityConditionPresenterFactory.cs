using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;

namespace ProSuite.DdxEditor.Content.Blazor;

public class QualityConditionPresenterFactory : IQualityConditionPresenterFactory
{
	public IItemNavigation ItemNavigation { get; set; }
	public QualityConditionItemAdapter Item { get; set; }

	public void CreateObserver(IQualityConditionView view)
	{
		new QualityConditionPresenter(Item, view, ItemNavigation);
	}
}
