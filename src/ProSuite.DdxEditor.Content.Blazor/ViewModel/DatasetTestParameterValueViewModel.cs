using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
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
	[NotNull] private readonly IInstanceConfigurationViewModel _viewModel;

	[CanBeNull] private string _filterExpression;

	private bool _usedAsReferenceData;

	private DatasetTestParameterValueViewModel(
		[NotNull] TestParameter parameter,
		[CanBeNull] object value,
		[CanBeNull] string imageSource,
		[CanBeNull] string modelName,
		[CanBeNull] string filterExpression,
		bool usedAsReferenceData,
		[NotNull] Either<Dataset, TransformerConfiguration> datasetSource,
		[NotNull] IInstanceConfigurationViewModel observer,
		bool required,
		bool validateOnPersistence) :
		base(parameter, value, observer, required, validateOnPersistence, "Dataset not set")
	{
		Assert.ArgumentNotNull(datasetSource, nameof(datasetSource));

		_viewModel = observer;

		_filterExpression = filterExpression;
		_usedAsReferenceData = usedAsReferenceData;

		DatasetSource = datasetSource;
		ImageSource = imageSource;
		ModelName = modelName;

		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);

		Validate();
	}

	[NotNull]
	public Either<Dataset, TransformerConfiguration> DatasetSource { get; private set; }

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

	public string ImageSource { get; private set; }

	public string DisplayValue => GetDisplayName();

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

		DatasetSource = source;

		ModelName = source.Match(d => d?.Model?.Name, TestParameterValueUtils.GetDatasetModelName);

		ImageSource = source.Match(BlazorImageUtils.GetImageSource, _ => null);

		FilterExpression = null;
		UsedAsReferenceData = false;

		// IMPORTANT: set last because it triggers OnPropertyChanged
		// which updates the entity
		Value = source.Match(d => d?.Name, t => t.Name);
	}

	protected override bool ValidateCore()
	{
		return DisplayValue != null;
	}

	[NotNull]
	public static DatasetTestParameterValueViewModel CreateInstance(
		[NotNull] TestParameter parameter,
		[CanBeNull] DatasetTestParameterValue datasetValue,
		[NotNull] IInstanceConfigurationViewModel observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(observer, nameof(observer));

		Either<Dataset, TransformerConfiguration> source =
			datasetValue?.ValueSource != null
				? new Either<Dataset, TransformerConfiguration>(datasetValue.ValueSource)
				: new Either<Dataset, TransformerConfiguration>(datasetValue?.DatasetValue);

		object value = source.Match(d => d?.Name, t => t?.Name);

		string modelName =
			source.Match(d => d?.Model?.Name, TestParameterValueUtils.GetDatasetModelName);

		string imageSource = source.Match(BlazorImageUtils.GetImageSource, _ => null);

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
		                                              parameter.IsConstructorParameter,
		                                              parameter.IsConstructorParameter);
	}

	private FinderForm<DatasetFinderItem> GetDatasetFinderForm(
		TestParameterType datasetParameterType)
	{
		var finder = new Finder<DatasetFinderItem>();

		DataQualityCategory category = _viewModel.InstanceConfiguration.Category;

		if (_viewModel.InstanceConfiguration is TransformerConfiguration transformer)
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
		return DatasetSource.Match(dataset => GetDisplayName(dataset, qualified), t => t?.Name);
	}

	private string GetDisplayName([CanBeNull] Dataset dataset, bool qualified = true)
	{
		string name = dataset == null ? null : $"{dataset.DisplayName ?? dataset.Name}";

		return qualified ? $"{name} [{ModelName}]" : name;
	}
}
