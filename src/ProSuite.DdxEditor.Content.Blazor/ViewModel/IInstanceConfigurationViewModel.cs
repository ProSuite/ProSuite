using System.ComponentModel;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IInstanceConfigurationViewModel : IViewObserver, IDataGridViewModel
{
	ITestParameterDatasetProvider DatasetProvider { get; }

	bool IsPersistent { get; }
	bool Discard { get; set; }
	IItemNavigation ItemNavigation { get; }

	void BindTo(InstanceConfiguration instanceConfiguration);

	void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e);

	InstanceConfiguration GetEntity();
}
