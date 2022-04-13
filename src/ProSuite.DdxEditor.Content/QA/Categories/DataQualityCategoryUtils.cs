using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public static class DataQualityCategoryUtils
	{
		[CanBeNull]
		public static DdxModel GetDefaultModel([CanBeNull] DataQualityCategory category)
		{
			return category?.GetDefaultModel();
		}

		[NotNull]
		public static IList<DataQualityCategory> GetCategories(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] Func<DataQualityCategory, bool> predicate)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(predicate, nameof(predicate));

			return modelBuilder.ReadOnlyTransaction(
				() => GetCategoriesTx(modelBuilder, predicate));
		}

		[NotNull]
		public static List<DataQualityCategory> GetCategoriesTx(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] Func<DataQualityCategory, bool> predicate)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(predicate, nameof(predicate));

			IDataQualityCategoryRepository repository = modelBuilder.DataQualityCategories;

			if (repository == null)
			{
				return new List<DataQualityCategory>();
			}

			List<DataQualityCategory> result = repository.GetAll()
			                                             .Where(predicate)
			                                             .ToList();

			// make sure that all subcategory collections are initialized
			foreach (DataQualityCategory category in result)
			{
				modelBuilder.Initialize(category.SubCategories);
			}

			return result;
		}

		[CanBeNull]
		public static DataQualityCategoryTableRow SelectCategory(
			[NotNull] IEnumerable<DataQualityCategory> categories,
			[NotNull] IWin32Window owner,
			bool allowNoCategorySelection = true)
		{
			Assert.ArgumentNotNull(categories, nameof(categories));
			Assert.ArgumentNotNull(owner, nameof(owner));

			List<DataQualityCategoryTableRow> tableRows =
				categories.Select(c => new DataQualityCategoryTableRow(c))
				          .ToList();

			if (allowNoCategorySelection)
			{
				tableRows.Insert(0, new DataQualityCategoryTableRow("<no category>"));
			}

			var finder = new Finder<DataQualityCategoryTableRow>();

			return finder.ShowDialog(owner, tableRows);
		}
	}
}
