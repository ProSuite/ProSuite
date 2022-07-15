using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA;
using ProSuite.UI.QA.BoundTableRows;

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

	internal static FinderForm<InstanceConfigurationInCategoryTableRow> GetRowFilterFinder(
		IRowFilterConfigurationProvider rowFilterProvider,
		[CanBeNull] Dataset forDataset,
		[CanBeNull] DataQualityCategory category,
		Finder<InstanceConfigurationInCategoryTableRow> finder)
	{
		DdxModel model = category?.GetDefaultModel();

		IList<ColumnDescriptor> columnDescriptors =
			new List<ColumnDescriptor>
			{
				new("Image"),
				new("Name"),
				new("Description"),
				new("AlgorithmImplementation")
			};

		return finder.CreateForm(GetFinderQueries(rowFilterProvider, forDataset, category),
		                         allowMultiSelection: true,
		                         columnDescriptors: columnDescriptors,
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
	private static IEnumerable<FinderQuery<InstanceConfigurationInCategoryTableRow>>
		GetFinderQueries(
			IRowFilterConfigurationProvider rowFilterProvider,
			Dataset forDataset,
			DataQualityCategory category)
	{
		if (category != null)
		{
			yield return new FinderQuery<InstanceConfigurationInCategoryTableRow>(
				string.Format("Datasets in {0}", category.Name),
				string.Format("model{0}", category.Id),
				() => GetListItems(rowFilterProvider, forDataset, category));
		}

		yield return new FinderQuery<InstanceConfigurationInCategoryTableRow>(
			"<All>", "[all]", () => GetListItems(rowFilterProvider, forDataset, category));
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

	[NotNull]
	private static IList<InstanceConfigurationInCategoryTableRow> GetListItems(
		[CanBeNull] IRowFilterConfigurationProvider rowFilterProvider,
		[NotNull] Dataset forDataset,
		[CanBeNull] DataQualityCategory category = null)
	{
		if (rowFilterProvider == null)
		{
			return new List<InstanceConfigurationInCategoryTableRow>();
		}

		List<InstanceConfigurationInCategoryTableRow> result =
			rowFilterProvider.GetFilterConfigurations(forDataset, category)
			                 .Select(dataset =>
				                         new InstanceConfigurationInCategoryTableRow(dataset, -1))
			                 .ToList();

		return result;
	}
}
