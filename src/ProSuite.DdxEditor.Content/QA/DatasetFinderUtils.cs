using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.QA
{
	/// <summary>
	/// Public lift of the dataset/transformer finder-query logic used by the classic
	/// Blazor parameter grid (<c>ProSuite.DdxEditor.Content.Blazor.FinderUtils</c>), so it
	/// can also be used by the algorithm-first UI's dataset parameter editor (see the
	/// "Find" button of <c>DatasetEditor</c> in <c>ProSuite.Shared.DdxEditor.Content</c>).
	/// </summary>
	public static class DatasetFinderUtils
	{
		/// <summary>
		/// Creates a modal finder form offering the datasets and transformers valid for
		/// <paramref name="datasetParameterType"/>, scoped to the default model of
		/// <paramref name="category"/> (if any) plus an "All" query.
		/// </summary>
		[NotNull]
		public static FinderForm<DatasetFinderItem> GetDatasetFinder(
			[CanBeNull] DataQualityCategory category,
			[CanBeNull] ITestParameterDatasetProvider datasetProvider,
			TestParameterType datasetParameterType,
			[NotNull] Finder<DatasetFinderItem> finder)
		{
			DdxModel model = category?.GetDefaultModel();

			return finder.CreateForm(GetFinderQueries(model, datasetProvider, datasetParameterType),
			                         allowMultiSelection: false,
			                         columnDescriptors: null,
			                         filterSettingsContext: FinderContextIds.GetId(category));
		}

		/// <summary>
		/// Gets the finder queries (model-scoped, plus "All") for
		/// <paramref name="datasetParameterType"/>.
		/// </summary>
		[NotNull]
		public static IEnumerable<FinderQuery<DatasetFinderItem>> GetFinderQueries(
			[CanBeNull] DdxModel model,
			[CanBeNull] ITestParameterDatasetProvider datasetProvider,
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

		/// <summary>
		/// Gets the datasets and transformers valid for <paramref name="datasetParameterType"/>,
		/// optionally restricted to <paramref name="model"/>.
		/// </summary>
		[NotNull]
		public static IList<DatasetFinderItem> GetListItems(
			[CanBeNull] ITestParameterDatasetProvider datasetProvider,
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
}
