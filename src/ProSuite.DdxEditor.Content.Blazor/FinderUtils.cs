using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;
using ProSuite.UI.QA.PropertyEditors;

namespace ProSuite.DdxEditor.Content.Blazor;

public static class FinderUtils
{
	public static FinderForm<DatasetFinderItem> GetFinder(DataQualityCategory category,
	                                                      ITestParameterDatasetProvider datasetProvider,
	                                                      Finder<DatasetFinderItem> finder)
	{
		DdxModel model = category?.GetDefaultModel();

		return finder.CreateForm(GetFinderQueries(model, datasetProvider),
		                         allowMultiSelection: false,
		                         columnDescriptors: null,
		                         filterSettingsContext: FinderContextIds.GetId(category));
	}

	[NotNull]
	private static IEnumerable<FinderQuery<DatasetFinderItem>> GetFinderQueries(
		[CanBeNull] DdxModel model, ITestParameterDatasetProvider datasetProvider)
	{
		if (model != null)
		{
			yield return new FinderQuery<DatasetFinderItem>(
				string.Format("Datasets in {0}", model.Name),
				string.Format("model{0}", model.Id),
				() => GetListItems(datasetProvider, model));
		}

		yield return new FinderQuery<DatasetFinderItem>(
			"<All>", "[all]", () => GetListItems(datasetProvider));
	}

	[NotNull]
	private static IList<DatasetFinderItem> GetListItems(
		ITestParameterDatasetProvider datasetProvider,
		[CanBeNull] DdxModel model = null)
	{
		if (datasetProvider == null)
		{
			return new List<DatasetFinderItem>();
		}

		// todo daro add more TestParameterTypes?
		return datasetProvider.GetDatasets(TestParameterType.PolygonDataset, model)
		                      .Select(dataset => new DatasetFinderItem(dataset))
		                      .ToList();
	}
}
