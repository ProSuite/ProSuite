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

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class DatasetTestParameterValueViewModel : ViewModelBase
{
	[NotNull] private readonly IInstanceConfigurationViewModel _viewModel;

	[CanBeNull] private string _filterExpression;
	private bool _usedAsReferenceData;

	private DatasetTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                           [CanBeNull] object value,
	                                           [CanBeNull] string imageSource,
	                                           [CanBeNull] string modelName,
	                                           [CanBeNull] string filterExpression,
	                                           bool usedAsReferenceData,
	                                           [NotNull]
	                                           Either<Dataset, TransformerConfiguration> datasetSource,
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

	public string ImageSource { get; set; }

	public void FindDatasetClicked()
	{
		TestParameterType parameterType = TestParameterTypeUtils.GetParameterType(DataType);

		using FinderForm<DatasetParameterFinderItem> form = GetFinderForm(parameterType);

		DialogResult result = form.ShowDialog();

		if (result != DialogResult.OK)
		{
			//return value;
		}

		IList<DatasetParameterFinderItem> selection = form.Selection;

		if (selection == null || selection.Count != 1)
		{
			return;
		}

		DatasetParameterFinderItem selectedItem = selection[0];

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
		                                              observer);
	}

	private FinderForm<DatasetParameterFinderItem> GetFinderForm(
		TestParameterType datasetParameterType)
	{
		var finder = new Finder<DatasetParameterFinderItem>();

		DataQualityCategory category = _viewModel.InstanceConfiguration.Category;

		if (_viewModel.InstanceConfiguration is TransformerConfiguration transformer)
		{
			// Do not allow circular references!
			_viewModel.DatasetProvider.Exclude(transformer);
		}

		return FinderUtils.GetFinder(category, _viewModel.DatasetProvider, datasetParameterType,
		                             finder);
	}
}
