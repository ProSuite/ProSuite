using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public interface IInstanceConfigurationViewModel : IViewObserver, IDataGridViewModel
{
	[NotNull]
	IItemNavigation ItemNavigation { get; }

	/// <summary>
	/// An optional expression builder for SQL expressions.
	/// </summary>
	[CanBeNull]
	ISqlExpressionBuilder SqlExpressionBuilder { get; }

	void BindTo([NotNull] InstanceConfiguration instanceConfiguration);

	void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e);

	[NotNull]
	InstanceConfiguration GetEntity();

	[CanBeNull]
	DatasetFinderItem FindDatasetClicked([NotNull] TestParameter parameter);
}
