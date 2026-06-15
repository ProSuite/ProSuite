using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.Controls
{
	internal class DataQualityCategoryItem : IEquatable<DataQualityCategoryItem>
	{
		public DataQualityCategoryItem([CanBeNull] DataQualityCategory category)
		{
			Category = category;

			IsUndefinedCategory = category == null;
			Name = category == null
				       ? "<no category>"
				       : category.Name;

			IsRootCategory = category?.ParentCategory == null;
		}

		public bool IsRootCategory { get; }

		[UsedImplicitly]
		public bool IsUndefinedCategory { get; private set; }

		[CanBeNull]
		public DataQualityCategory Category { get; }

		[NotNull]
		public string Name { get; }

		[NotNull]
		public List<DataQualityCategoryItem> SubCategories { get; } =
			new List<DataQualityCategoryItem>();

		[NotNull]
		public List<SpecificationDataset> SpecificationDatasets { get; } =
			new List<SpecificationDataset>();

		public bool Equals(DataQualityCategoryItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(Category, other.Category);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((DataQualityCategoryItem) obj);
		}

		public override int GetHashCode()
		{
			return Category?.GetHashCode() ?? 0;
		}
	}
}
