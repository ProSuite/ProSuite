using System.Collections.Generic;
using System.ComponentModel;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IInstanceConfigurationViewModel : IViewObserver, INotifyPropertyChanged
{
	InstanceConfiguration InstanceConfiguration { get; }

	ITestParameterDatasetProvider DatasetProvider { get; }

	IList<ViewModelBase> Rows { get; }

	void BindTo(InstanceConfiguration instanceConfiguration);

	void DeleteRow(ViewModelBase selectedCollectionRow);

	ViewModelBase InsertRow(TestParameter firstParameter);
}
