using System.Collections.Generic;
using System.Windows.Forms;
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
	[NotNull] private readonly IInstanceConfigurationAwareViewModel _viewModel;

	[NotNull] private Either<Dataset, TransformerConfiguration> _source;

	[CanBeNull] private string _filterExpression;
	private bool _usedAsReferenceData;

	public DatasetTestParameterValueViewModel(
		[NotNull] TestParameter parameter,
		[CanBeNull] DatasetTestParameterValue datasetParameterValue,
		[NotNull] IInstanceConfigurationAwareViewModel observer) :
		base(parameter, observer)
	{
		_viewModel = observer;

		_source =
			datasetParameterValue?.ValueSource != null
				? new Either<Dataset, TransformerConfiguration>(datasetParameterValue.ValueSource)
				: new Either<Dataset, TransformerConfiguration>(
					datasetParameterValue?.DatasetValue);

		_filterExpression = datasetParameterValue?.FilterExpression;
		_usedAsReferenceData = datasetParameterValue?.UsedAsReferenceData ?? false;

		ImageSource = DatasetSource.Match(d => BlazorImageUtils.GetImageSource(d),
		                                  _ => null);
		ModelName = _source.Match(d => d?.Model?.Name,
		                          TestParameterValueUtils.GetDatasetModelName);

		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	[NotNull]
	public Either<Dataset, TransformerConfiguration> DatasetSource
	{
		get => _source;
		private set => SetProperty(ref _source, value);
	}

	[CanBeNull]
	[UsedImplicitly]
	public string ModelName { get; }

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

	public override object Value
	{
		get => DatasetSource.Match(d => d?.Name, t => t?.Name);
		set { }
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

		if (selection != null && selection.Count == 1)
		{
			DatasetParameterFinderItem selectedItem = selection[0];

			DatasetSource = selectedItem.Source;

			//if (value is DatasetConfig datasetConfig)
			//{
			//	datasetConfig.Data = selectedDataset;
			//}
			//else
			//{
			//	value = selectedDataset;
			//}
		}

		//return value;
	}

	private FinderForm<DatasetParameterFinderItem> GetFinderForm(
		TestParameterType datasetParameterType)
	{
		var finder = new Finder<DatasetParameterFinderItem>();

		DataQualityCategory category = _viewModel.InstanceConfiguration.Category;

		return FinderUtils.GetFinder(category, _viewModel.DatasetProvider, datasetParameterType,
		                             finder);
	}
}
