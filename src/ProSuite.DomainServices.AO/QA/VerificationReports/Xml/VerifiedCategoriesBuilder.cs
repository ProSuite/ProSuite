using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class VerifiedCategoriesBuilder
	{
		[NotNull] private readonly List<XmlVerifiedCategory> _rootCategories =
			new List<XmlVerifiedCategory>();

		[NotNull] private readonly Dictionary<string, XmlVerifiedCategory> _categoriesByUuid
			= new Dictionary<string, XmlVerifiedCategory>(StringComparer.OrdinalIgnoreCase);

		private bool _sortOrderDirty;

		[NotNull]
		public List<XmlVerifiedCategory> RootCategories
		{
			get
			{
				if (_sortOrderDirty)
				{
					_rootCategories.Sort();
					_sortOrderDirty = false;
				}

				return _rootCategories;
			}
		}

		public void AddVerifiedCondition(
			[NotNull] XmlVerifiedQualityCondition xmlVerifiedCondition)
		{
			Assert.ArgumentNotNull(xmlVerifiedCondition, nameof(xmlVerifiedCondition));

			DataQualityCategory category = xmlVerifiedCondition.Category;

			if (category == null)
			{
				AddToUndefinedCategory(xmlVerifiedCondition);
			}
			else
			{
				AddToCategory(xmlVerifiedCondition, category);
			}
		}

		private void AddToUndefinedCategory(
			[NotNull] XmlVerifiedQualityCondition xmlVerifiedCondition)
		{
			string key = string.Empty;

			XmlVerifiedCategory xmlCategory;
			if (! _categoriesByUuid.TryGetValue(key, out xmlCategory))
			{
				xmlCategory = new XmlVerifiedCategory("<no category>");

				_categoriesByUuid.Add(key, xmlCategory);
				_rootCategories.Add(xmlCategory);

				_sortOrderDirty = true;
			}

			xmlCategory.AddCondition(xmlVerifiedCondition);
		}

		private void AddToCategory(
			[NotNull] XmlVerifiedQualityCondition xmlVerifiedCondition,
			[NotNull] DataQualityCategory category)
		{
			XmlVerifiedCategory xmlCategory = GetVerifiedCategory(category);

			xmlCategory.AddCondition(xmlVerifiedCondition);
		}

		[NotNull]
		private XmlVerifiedCategory GetVerifiedCategory(
			[NotNull] DataQualityCategory category)
		{
			string key = Assert.NotNull(category.Uuid);

			XmlVerifiedCategory xmlCategory;
			if (! _categoriesByUuid.TryGetValue(key, out xmlCategory))
			{
				xmlCategory = new XmlVerifiedCategory(category.Name,
				                                      category.Abbreviation,
				                                      category.Uuid,
				                                      category.Description,
				                                      category.ListOrder);

				_categoriesByUuid.Add(key, xmlCategory);

				if (category.ParentCategory == null)
				{
					_rootCategories.Add(xmlCategory);
				}
				else
				{
					// ensure that parent category is added also
					XmlVerifiedCategory parentVerifiedCategory =
						GetVerifiedCategory(category.ParentCategory);

					parentVerifiedCategory.AddSubCategory(xmlCategory);
				}

				_sortOrderDirty = true;
			}

			return xmlCategory;
		}
	}
}
