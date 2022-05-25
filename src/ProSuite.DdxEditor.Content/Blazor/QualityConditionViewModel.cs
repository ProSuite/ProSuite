using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;
using ProSuite.UI.QA.PropertyEditors;

namespace ProSuite.DdxEditor.Content.Blazor;

public class QualityConditionViewModel
{
	public event EventHandler SavedChanges;

	public QualityConditionViewModel(QualityCondition qualityCondition,
	                                 ITestParameterDatasetProvider datasetProvider)
	{
		QualityCondition = qualityCondition;

		DatasetProvider = datasetProvider;
		
		IEnumerable<TestParameterViewModel> items =
			qualityCondition.ParameterValues.Select(v => new TestParameterViewModel(v));

		Items = new List<TestParameterViewModel>(items);
	}

	// todo daro maybe parameter QualityCondition is not necessary here!
	public void NotifySavedChanges([CanBeNull] QualityCondition qualityCondition = null)
	{
		SavedChanges?.Invoke(this, null);
	}

	private QualityCondition QualityCondition { get; }
	public ITestParameterDatasetProvider DatasetProvider { get; }

	public IList<TestParameterViewModel> Items { get; set; }

	protected TestParameterType TestParameterTypes => TestParameterType.PolygonDataset;

	public void Clicked()
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
				Dataset selectedDataset = selection[0].Dataset;

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

	[NotNull]
	private FinderForm<DatasetFinderItem> GetFinderForm()
	{
		var finder = new Finder<DatasetFinderItem>();

		DataQualityCategory category = QualityCondition?.Category;
		DdxModel model = category?.GetDefaultModel();

		return finder.CreateForm(GetFinderQueries(model),
		                         allowMultiSelection: false,
		                         columnDescriptors: null,
		                         filterSettingsContext: FinderContextIds.GetId(category));
	}

	[NotNull]
	private IEnumerable<FinderQuery<DatasetFinderItem>> GetFinderQueries(
		[CanBeNull] DdxModel model)
	{
		if (model != null)
		{
			yield return new FinderQuery<DatasetFinderItem>(
				string.Format("Datasets in {0}", model.Name),
				string.Format("model{0}", model.Id),
				() => GetListItems(model));
		}

		yield return new FinderQuery<DatasetFinderItem>(
			"<All>", "[all]", () => GetListItems());
	}

	[NotNull]
	private IList<DatasetFinderItem> GetListItems([CanBeNull] DdxModel model = null)
	{
		// todo daro: drop! only for testing
		return new List<DatasetFinderItem>();

		if (DatasetProvider == null)
		{
			return new List<DatasetFinderItem>();
		}

		return DatasetProvider.GetDatasets(TestParameterTypes, model)
		                      .Select(dataset => new DatasetFinderItem(dataset))
		                      .ToList();
	}
}
