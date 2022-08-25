using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Prism.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DdxEditor.Framework.Events;
using ProSuite.DomainModel.AO.QA;
using ProSuite.Shared.IoCRoot;
using Radzen;

namespace ProSuite.DdxEditor.Content.Blazor;

public partial class QualityConditionTableViewBlazor
{
	// ReSharper disable once NotNullMemberIsNotInitialized
	[NotNull] private IDataGridViewModel _viewModel;
	private bool _discardChanges;

	protected override void OnInitializedCore()
	{
		base.OnInitializedCore();

		var eventAggregator =
			ContainerRegistry.Current.Resolve<IEventAggregator>();

		eventAggregator.GetEvent<DiscardChangesEvent>().Subscribe(OnDiscardChanges);
	}

	private void OnDiscardChanges()
	{
		_discardChanges = true;
	}

	[NotNull]
	[Parameter]
	// ReSharper disable once NotNullMemberIsNotInitialized
	public IDataGridViewModel ViewModel
	{
		get => _viewModel;
		set
		{
			_viewModel = value;
			_viewModel.PropertyChanged += OnPropertyChanged;
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// QualityConditionItem.DiscardChangesCore()
		// fires prior to
		// Item.DiscardChangesCore()
		if (_discardChanges)
		{
			DataGrid.Reload();

			_discardChanges = false;
		}
	}

	protected override void DisposeCore()
	{
		_viewModel.PropertyChanged -= OnPropertyChanged;

		var eventAggregator =
			ContainerRegistry.Current.Resolve<IEventAggregator>();

		eventAggregator.GetEvent<DiscardChangesEvent>().Unsubscribe(OnDiscardChanges);
	}

	private IEnumerable<ViewModelBase> Values { get; set; }

	private void OnLoadData(LoadDataArgs args)
	{
		Values = Assert.NotNull(ViewModel).Values;
	}

	#region layout

	private void OnRowRender(RowRenderEventArgs<ViewModelBase> args)
	{
		// expander or not?
		args.Expandable = args.Data is TestParameterValueCollectionViewModel;
	}

	private void OnCellRender(DataGridCellRenderEventArgs<ViewModelBase> args)
	{
		IDictionary<string, object> attributes = args.Attributes;

		if (args.Data is TestParameterValueCollectionViewModel vm &&
		    ! TestParameterTypeUtils.IsDatasetType(vm.Parameter.Type))
		{
			SetBackgroundColorGrey(args, attributes);
			return;
		}

		if (args.Data is ScalarTestParameterValueViewModel)
		{
			SetBackgroundColorGrey(args, attributes);
		}
	}

	private static void SetBackgroundColorGrey(
		[NotNull] DataGridCellRenderEventArgs<ViewModelBase> args,
		[NotNull] IDictionary<string, object> attributes)
	{
		if (args.Column.Property == "ModelName")
		{
			attributes.Add("colspan", 3);

			if (attributes.ContainsKey("style"))
			{
				attributes["style"] += "; background-color: #adadad";
			}
			else
			{
				attributes.Add("style", "background-color: #adadad");
			}
		}
	}

	#endregion
}
