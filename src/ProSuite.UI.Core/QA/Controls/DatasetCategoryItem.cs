using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.UI.Core.QA.Controls
{
	public class DatasetCategoryItem : IEquatable<DatasetCategoryItem>
	{
		public DatasetCategoryItem([CanBeNull] DatasetCategory datasetCategory)
		{
			IsNull = datasetCategory == null;

			Name = datasetCategory == null
				       ? "<other>"
				       : datasetCategory.Name;
		}

		[NotNull]
		public string Name { get; }

		public bool IsNull { get; }

		public bool Equals(DatasetCategoryItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.Name, Name);
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

			if (obj.GetType() != typeof(DatasetCategoryItem))
			{
				return false;
			}

			return Equals((DatasetCategoryItem) obj);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}
