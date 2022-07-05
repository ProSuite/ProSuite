using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;

namespace ProSuite.DdxEditor.Content.Blazor;

internal static class FinderUtils
{
	internal static FinderForm<DatasetParameterFinderItem> GetFinder(DataQualityCategory category,
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType,
		Finder<DatasetParameterFinderItem> finder)
	{
		DdxModel model = category?.GetDefaultModel();

		return finder.CreateForm(GetFinderQueries(model, datasetProvider, datasetParameterType),
		                         allowMultiSelection: false,
		                         columnDescriptors: null,
		                         filterSettingsContext: FinderContextIds.GetId(category));
	}

	[NotNull]
	private static IEnumerable<FinderQuery<DatasetParameterFinderItem>> GetFinderQueries(
		[CanBeNull] DdxModel model,
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType)
	{
		if (model != null)
		{
			yield return new FinderQuery<DatasetParameterFinderItem>(
				string.Format("Datasets in {0}", model.Name),
				string.Format("model{0}", model.Id),
				() => GetListItems(datasetProvider, datasetParameterType, model));
		}

		yield return new FinderQuery<DatasetParameterFinderItem>(
			"<All>", "[all]", () => GetListItems(datasetProvider, datasetParameterType));
	}

	[NotNull]
	private static IList<DatasetParameterFinderItem> GetListItems(
		ITestParameterDatasetProvider datasetProvider,
		TestParameterType datasetParameterType,
		[CanBeNull] DdxModel model = null)
	{
		if (datasetProvider == null)
		{
			return new List<DatasetParameterFinderItem>();
		}

		List<DatasetParameterFinderItem> result =
			datasetProvider
				.GetDatasets(datasetParameterType, model)
				.Select(dataset => new DatasetParameterFinderItem(dataset))
				.ToList();

		result.AddRange(datasetProvider.GetTransformers(datasetParameterType, model)
		                               .Select(t => new DatasetParameterFinderItem(t)));

		return result;
	}
}
