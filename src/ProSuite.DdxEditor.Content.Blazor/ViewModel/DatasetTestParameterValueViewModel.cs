using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Text;
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
	[CanBeNull] private string _rowFilterExpression;

	public List<RowFilterConfiguration> RowFilterConfigurations { get; } =
		new List<RowFilterConfiguration>();

	private bool _usedAsReferenceData;

	private DatasetTestParameterValueViewModel(
		[NotNull] TestParameter parameter,
		[CanBeNull] object value,
		[CanBeNull] string imageSource,
		[CanBeNull] string modelName,
		[CanBeNull] string filterExpression,
		bool usedAsReferenceData,
		[NotNull] Either<Dataset, TransformerConfiguration> datasetSource,
		[CanBeNull] ICollection<RowFilterConfiguration> rowFilters,
		[NotNull] IInstanceConfigurationViewModel observer) :
		base(parameter, value, observer)
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

		InitializeRowFilters(rowFilters);
	}

	private void InitializeRowFilters([CanBeNull] IEnumerable<RowFilterConfiguration> rowFilters)
	{
		if (rowFilters == null)
		{
			return;
		}

		RowFilterConfigurations.AddRange(rowFilters);

		_rowFilterExpression =
			StringUtils.Concatenate(RowFilterConfigurations.Select(rf => rf.Name), ", ");
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

	[CanBeNull]
	[UsedImplicitly]
	public string RowFilterExpression
	{
		get => _rowFilterExpression;
		set => SetProperty(ref _rowFilterExpression, value);
	}

	[UsedImplicitly]
	public bool UsedAsReferenceData
	{
		get => _usedAsReferenceData;
		set => SetProperty(ref _usedAsReferenceData, value);
	}

	public string ImageSource { get; set; }

	public string DisplayValue => DatasetSource.Match(
		GetDisplayName,
		t => t?.Name);

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

	public void FindRowFilterClicked(Either<Dataset, TransformerConfiguration> soureDataset)
	{
		Dataset parameterDataset = soureDataset.Match(d => d, t => null);
		using FinderForm<InstanceConfigurationInCategoryTableRow> form =
			GetRowFilterFinderForm(parameterDataset);

		DialogResult result = form.ShowDialog();

		if (result != DialogResult.OK)
		{
			return;
		}

		IList<InstanceConfigurationInCategoryTableRow> selection = form.Selection;

		if (selection?.Count != 1)
		{
			return;
		}

		InstanceConfigurationInCategoryTableRow selectedItem = selection[0];

		RowFilterConfiguration rowFilter =
			(RowFilterConfiguration) selectedItem.InstanceConfiguration;

		RowFilterConfigurations.Clear();
		RowFilterConfigurations.Add(rowFilter);
		RowFilterExpression = rowFilter?.Name;
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
		                                              datasetValue?.RowFilterConfigurations,
		                                              observer);
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

	private FinderForm<InstanceConfigurationInCategoryTableRow> GetRowFilterFinderForm(
		[CanBeNull] Dataset parameterDataset)
	{
		var finder = new Finder<InstanceConfigurationInCategoryTableRow>();

		DataQualityCategory category = _viewModel.InstanceConfiguration.Category;

		return FinderUtils.GetRowFilterFinder(_viewModel.RowFilterProvider, parameterDataset,
		                                      category, finder);
	}

	private string GetDisplayName([CanBeNull] Dataset dataset)
	{
		return dataset == null ? null : $"{dataset.DisplayName ?? dataset.Name} [{ModelName}]";
	}
}
