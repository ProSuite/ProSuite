using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA.PropertyEditors;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class DatasetTestParameterValueViewModel : ViewModelBase
{
	private readonly IQualityConditionAwareViewModel _viewModel;
	private Dataset _dataset;
	private string _filterExpression;
	private bool _usedAsReferenceData;

	public DatasetTestParameterValueViewModel([NotNull] string name,
	                                          [NotNull] Dataset dataset,
	                                          string filterExpression,
	                                          bool usedAsReferenceData,
	                                          [NotNull] IQualityConditionAwareViewModel observer) :
		base(name, observer)
	{
		Assert.ArgumentNotNull(dataset, nameof(dataset));

		_viewModel = observer;

		_dataset = dataset;
		_filterExpression = filterExpression;
		_usedAsReferenceData = usedAsReferenceData;

		ImageSource = BlazorImageUtils.GetImageSource(Dataset);

		ModelName = Dataset.Model?.Name;

		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);
	}

	public Dataset Dataset
	{
		get => _dataset;
		private set => SetProperty(ref _dataset, value);
	}

	[UsedImplicitly]
	public string ModelName { get; }

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
		get => Dataset?.ToString();
		set { }
	}

	public void FindDatasetClicked()
	{
		using (FinderForm<DatasetFinderItem> form = GetFinderForm())
		{
			DialogResult result = form.ShowDialog();

			if (result != DialogResult.OK)
			{
				//return value;
			}

			IList<DatasetFinderItem> selection = form.Selection;

			if (selection != null && selection.Count == 1)
			{
				Dataset = selection[0].Dataset;

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
	}

	private FinderForm<DatasetFinderItem> GetFinderForm()
	{
		var finder = new Finder<DatasetFinderItem>();

		DataQualityCategory category = _viewModel.QualityCondition.Category;

		return FinderUtils.GetFinder(category, _viewModel.DatasetProvider, finder);
	}
}
