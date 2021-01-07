using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	public abstract class DataQualityCategoryRepositoryTestBase :
		RepositoryTestBase<IDataQualityCategoryRepository>
	{
		[Test]
		public void CanGetCategoryHierarchy()
		{
			var cat1 = new DataQualityCategory("1", "1");
			var cat2 = new DataQualityCategory("2", "2");
			var cat1_1 = new DataQualityCategory("1", "1.1");
			var cat1_2 = new DataQualityCategory("2", "1.2");
			var cat1_1_1 = new DataQualityCategory("1", "1.1.1");

			cat1.AddSubCategory(cat1_1);
			cat1.AddSubCategory(cat1_2);
			cat1_1.AddSubCategory(cat1_1_1);

			var qspecA = new QualitySpecification("qspec A") {Category = cat1_1};
			var qspecB = new QualitySpecification("qspec B") {Category = cat1_1};
			var qspecC = new QualitySpecification("qspec C") {Category = cat1_1_1};
			var qspec1 = new QualitySpecification("qspec X");
			var qspec2 = new QualitySpecification("qspec Y");

			CreateSchema(cat1, cat2, cat1_1, cat1_2, cat1_1_1,
			             qspecA, qspecB, qspecC, qspec1, qspec2);

			UnitOfWork.NewTransaction(
				delegate
				{
					IList<DataQualityCategory> categories =
						Repository.GetTopLevelCategories();
					var qspecRepository = Resolve<IQualitySpecificationRepository>();
					Assert.AreEqual(2, categories.Count);

					Assert.IsTrue(categories.Contains(cat1));
					Assert.IsTrue(categories.Contains(cat2));

					string hierarchy = FormatHierarchy(qspecRepository, categories);

					const string expected = @"* qspec X
* qspec Y
> 1
  > 1
    * qspec A
    * qspec B
    > 1
      * qspec C
  > 2
> 2
";
					Console.WriteLine(hierarchy);

					Assert.AreEqual(expected, hierarchy);
				});
		}

		[Test]
		public void CanGetQualifiedNames()
		{
			var cat1 = new DataQualityCategory("1", "1");
			var cat2 = new DataQualityCategory("2", "2");
			var cat1_1 = new DataQualityCategory("1", "1.1");
			var cat1_2 = new DataQualityCategory("2", "1.2");
			var cat1_1_1 = new DataQualityCategory("1", "1.1.1");

			cat1.AddSubCategory(cat1_1);
			cat1.AddSubCategory(cat1_2);
			cat1_1.AddSubCategory(cat1_1_1);

			var qspecA = new QualitySpecification("A") {Category = cat1_1};
			var qspecB = new QualitySpecification("B") {Category = cat1_1};
			var qspecC = new QualitySpecification("C") {Category = cat1_1_1};
			var qspec1 = new QualitySpecification("X");
			var qspec2 = new QualitySpecification("Y");

			CreateSchema(cat1, cat2, cat1_1, cat1_2, cat1_1_1,
			             qspecA, qspecB, qspecC, qspec1, qspec2);

			UnitOfWork.NewTransaction(
				delegate
				{
					var qspecs = Resolve<IQualitySpecificationRepository>();

					Assert.AreEqual("1/1/A", GetQualifiedName(qspecs.Get("A")));
					Assert.AreEqual("1/1/B", GetQualifiedName(qspecs.Get("B")));
					Assert.AreEqual("1/1/1/C", GetQualifiedName(qspecs.Get("C")));
					Assert.AreEqual("X", GetQualifiedName(qspecs.Get("X")));
					Assert.AreEqual("Y", GetQualifiedName(qspecs.Get("Y")));
				});
		}

		[NotNull]
		private static string GetQualifiedName(
			[CanBeNull] QualitySpecification qualitySpecification)
		{
			if (qualitySpecification == null)
			{
				return string.Empty;
			}

			return qualitySpecification.GetQualifiedName();
		}

		[NotNull]
		private string FormatHierarchy(
			[NotNull] IQualitySpecificationRepository qspecRepository,
			[NotNull] IEnumerable<DataQualityCategory> categories)
		{
			var writer = new StringWriter();
			foreach (QualitySpecification qspec in qspecRepository.Get(category: null)
			                                                      .OrderBy(qs => qs.Name))
			{
				WriteQualitySpecification(writer, qspec);
			}

			foreach (DataQualityCategory category in categories.OrderBy(c => c.Name))
			{
				WriteCategory(writer, category);
			}

			return writer.ToString();
		}

		private void WriteCategory([NotNull] TextWriter writer,
		                           [NotNull] DataQualityCategory category,
		                           int level = 0)
		{
			writer.WriteLine(@"{0}> {1}", GetIndentationPadding(level), category.Name);

			foreach (QualitySpecification qspec in
				Resolve<IQualitySpecificationRepository>().Get(category)
				                                          .OrderBy(qs => qs.Name))
			{
				WriteQualitySpecification(writer, qspec, level + 1);
			}

			foreach (DataQualityCategory child in category.SubCategories.OrderBy(c => c.Name))
			{
				WriteCategory(writer, child, level + 1);
			}
		}

		private static void WriteQualitySpecification([NotNull] TextWriter writer,
		                                              [NotNull] QualitySpecification
			                                              qualitySpecification, int level = 0)
		{
			writer.WriteLine(@"{0}* {1}",
			                 GetIndentationPadding(level),
			                 qualitySpecification.Name);
		}

		private static string GetIndentationPadding(int level)
		{
			return new string(' ', level * 2);
		}
	}
}
