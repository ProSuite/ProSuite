using System.ComponentModel;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IInstanceConfigurationViewModel : IViewObserver, IDataGridViewModel
{
	IItemNavigation ItemNavigation { get; }

	void BindTo(InstanceConfiguration instanceConfiguration);

	void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e);

	InstanceConfiguration GetEntity();

	DatasetFinderItem FindDatasetClicked(TestParameter parameter);
}
