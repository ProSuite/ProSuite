using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class DatasetTestParameterValueViewModel : ViewModelBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly IInstanceConfigurationViewModel _viewModel;
	[CanBeNull] private Either<Dataset, TransformerConfiguration> _datasetSource;
	[CanBeNull] private string _filterExpression;

	private bool _usedAsReferenceData;

	private DatasetTestParameterValueViewModel(
		[NotNull] TestParameter parameter,
		[CanBeNull] object value,
		[CanBeNull] string imageSource,
		[CanBeNull] string modelName,
		[CanBeNull] string filterExpression,
		bool usedAsReferenceData,
		[CanBeNull] Either<Dataset, TransformerConfiguration> datasetSource,
		[NotNull] IInstanceConfigurationViewModel observer,
		bool required) :
		base(parameter, value, observer, required, "Dataset not set")
	{
		// todo daro rename
		_viewModel = observer;

		_filterExpression = filterExpression;
		_usedAsReferenceData = usedAsReferenceData;
		_datasetSource = datasetSource;

		ImageSource = imageSource;
		ModelName = modelName;

		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);

		Validate();
	}

	[CanBeNull]
	public Either<Dataset, TransformerConfiguration> DatasetSource
	{
		get => _datasetSource;
		private set => SetProperty(ref _datasetSource, value);
	}

	[CanBeNull]
	[UsedImplicitly]
	public string ModelName { get; set; }

	[CanBeNull]
	[UsedImplicitly]
	public string FilterExpression
	{
		get => _filterExpression;
		set => SetProperty(ref _filterExpression, value);
	}

	[UsedImplicitly]
	public bool UsedAsReferenceData
	{
		get => _usedAsReferenceData;
		set => SetProperty(ref _usedAsReferenceData, value);
	}

	[CanBeNull]
	public string ImageSource { get; private set; }

	public string DisplayValue => GetDisplayName();

	public bool? IsDataset => DatasetSource?.Match(dataset => dataset != null, _ => false);

	public void GoTo()
	{
		TransformerConfiguration match = DatasetSource?.Match(d => null, t => t);

		if (match != null)
		{
			_viewModel.ItemNavigation.GoToItem(match);
		}
	}

	public void OnInput(ChangeEventArgs args)
	{
		FilterExpression = args.Value?.ToString();
	}

	public void FindDatasetClicked()
	{
		TestParameterType parameterType = TestParameterTypeUtils.GetParameterType(DataType);

		using FinderForm<DatasetFinderItem> form = GetDatasetFinderForm(parameterType);

		DialogResult result = form.ShowDialog();

		if (result != DialogResult.OK)
		{
			//return value;
		}

		IList<DatasetFinderItem> selection = form.Selection;

		if (selection == null || selection.Count != 1)
		{
			return;
		}

		DatasetFinderItem selectedItem = selection[0];

		Either<Dataset, TransformerConfiguration> source = selectedItem.Source;

		ModelName = source.Match(d => d?.Model?.Name, InstanceConfigurationUtils.GetDatasetModelName);

		ImageSource = source.Match(BlazorImageUtils.GetImageSource, BlazorImageUtils.GetImageSource);

		FilterExpression = null;
		UsedAsReferenceData = false;

		// triggers OnPropertyChanged and updates the entity
		DatasetSource = source;
	}

	protected override bool ValidateCore()
	{
		if (DatasetSource == null)
		{
			return false;
		}

		bool valid = DatasetSource.Match(dataset => dataset != null, newConfiguration =>
		{
			InstanceConfiguration current = _viewModel.GetEntity();

			var configurationNames = new NotificationCollection();

			NotificationUtils.Add(configurationNames, current.Name);

			if (! TestParameterValueUtils.CheckCircularReferencesInGraph(
				    current, newConfiguration, configurationNames))
			{
				return true;
			}

			_msg.Warn(
				$"Not allowed circular {current.GetType().Name} references: {NotificationUtils.Concatenate(configurationNames, " -> ")}. Value is reset.");

			return false;
		});

		if (valid)
		{
			return true;
		}

		// don't set on property because this
		// triggers validation again.
		_datasetSource = null;
		ModelName = null;
		ImageSource = null;
		FilterExpression = null;
		UsedAsReferenceData = false;

		return false;
	}

	protected override void ResetValueCore()
	{
		DatasetSource = null;
	}

	[NotNull]
	public static DatasetTestParameterValueViewModel CreateInstance(
		[NotNull] TestParameter parameter,
		[CanBeNull] DatasetTestParameterValue datasetValue,
		[NotNull] IInstanceConfigurationViewModel observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(observer, nameof(observer));

		Either<Dataset, TransformerConfiguration> source = null;

		if (datasetValue?.ValueSource != null)
		{
			source = new Either<Dataset, TransformerConfiguration>(datasetValue.ValueSource);
		}
		else if (datasetValue?.DatasetValue != null)
		{
			source = new Either<Dataset, TransformerConfiguration>(datasetValue.DatasetValue);
		}

		object value = source?.Match(d => d?.Name, t => t?.Name);

		string modelName =
			source?.Match(d => d?.Model?.Name, InstanceConfigurationUtils.GetDatasetModelName);

		string imageSource = source?.Match(BlazorImageUtils.GetImageSource, BlazorImageUtils.GetImageSource);

		string filterExpression = null;
		var usedAsReferenceData = false;

		if (datasetValue != null)
		{
			filterExpression = datasetValue.FilterExpression;
			usedAsReferenceData = datasetValue.UsedAsReferenceData;
		}

		return new DatasetTestParameterValueViewModel(parameter, value, imageSource, modelName,
		                                              filterExpression, usedAsReferenceData, source,
		                                              observer,
		                                              parameter.IsConstructorParameter);
	}

	private FinderForm<DatasetFinderItem> GetDatasetFinderForm(
		TestParameterType datasetParameterType)
	{
		var finder = new Finder<DatasetFinderItem>();

		InstanceConfiguration instanceConfiguration = _viewModel.GetEntity();

		DataQualityCategory category = instanceConfiguration.Category;

		if (instanceConfiguration is TransformerConfiguration transformer)
		{
			// Do not allow circular references!
			_viewModel.DatasetProvider.Exclude(transformer);
		}

		return FinderUtils.GetDatasetFinder(category, _viewModel.DatasetProvider,
		                                    datasetParameterType,
		                                    finder);
	}

	public string GetDisplayName(bool qualified = true)
	{
		return DatasetSource?.Match(dataset => GetDisplayName(dataset, qualified), t => t?.Name);
	}

	private string GetDisplayName([CanBeNull] IModelElement dataset, bool qualified = true)
	{
		string name = dataset == null ? null : $"{dataset.DisplayName ?? dataset.Name}";

		if (string.IsNullOrEmpty(name))
		{
			return null;
		}

		return qualified ? $"{name} [{ModelName}]" : name;
	}
}
