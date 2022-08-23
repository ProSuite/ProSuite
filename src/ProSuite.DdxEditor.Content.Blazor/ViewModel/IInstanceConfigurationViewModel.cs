using System.ComponentModel;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IInstanceConfigurationViewModel : IViewObserver, IDataGridViewModel
{
	InstanceConfiguration InstanceConfiguration { get; }

	ITestParameterDatasetProvider DatasetProvider { get; }

	void BindTo(InstanceConfiguration instanceConfiguration);

	void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e);
}
