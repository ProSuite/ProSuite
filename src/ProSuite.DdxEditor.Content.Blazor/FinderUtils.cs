using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.Blazor;

internal static class FinderUtils
{
	internal static FinderForm<DatasetFinderItem> GetDatasetFinder(DataQualityCategory category,
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType,
		Finder<DatasetFinderItem> finder)
	{
		DdxModel model = category?.GetDefaultModel();

		return finder.CreateForm(GetFinderQueries(model, datasetProvider, datasetParameterType),
		                         allowMultiSelection: false,
		                         columnDescriptors: null,
		                         filterSettingsContext: FinderContextIds.GetId(category));
	}
	
	[NotNull]
	private static IEnumerable<FinderQuery<DatasetFinderItem>> GetFinderQueries(
		[CanBeNull] DdxModel model,
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType)
	{
		if (model != null)
		{
			yield return new FinderQuery<DatasetFinderItem>(
				string.Format("Datasets in {0}", model.Name),
				string.Format("model{0}", model.Id),
				() => GetListItems(datasetProvider, datasetParameterType, model));
		}

		yield return new FinderQuery<DatasetFinderItem>(
			"<All>", "[all]", () => GetListItems(datasetProvider, datasetParameterType));
	}

	[NotNull]
	private static IList<DatasetFinderItem> GetListItems(
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType,
		[CanBeNull] DdxModel model = null)
	{
		if (datasetProvider == null)
		{
			return new List<DatasetFinderItem>();
		}

		List<DatasetFinderItem> result =
			datasetProvider
				.GetDatasets(datasetParameterType, model)
				.Select(dataset => new DatasetFinderItem(dataset))
				.ToList();

		result.AddRange(datasetProvider.GetTransformers(datasetParameterType, model)
		                               .Select(t => new DatasetFinderItem(t)));

		return result;
	}
}
