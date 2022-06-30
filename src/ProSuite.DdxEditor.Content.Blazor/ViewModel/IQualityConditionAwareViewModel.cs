using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

// todo daro better name
public interface IViewModel : IViewObserver
{
}

// todo daro use IQualityConditionContextAware!!!
public interface IInstanceConfigurationAwareViewModel : IViewModel, INotifyPropertyChanged
{
	InstanceConfiguration InstanceConfiguration { get; }

	ITestParameterDatasetProvider DatasetProvider { get; }

	IList<ViewModelBase> Rows { get; set; }

	void BindTo(InstanceConfiguration instanceConfiguration);

	void DeleteRow(ViewModelBase selectedCollectionRow);

	ViewModelBase InsertRow(TestParameter firstParameter);
}
