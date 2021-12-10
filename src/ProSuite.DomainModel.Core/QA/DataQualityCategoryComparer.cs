using System;
using System.Collections.Generic;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// IComparer implementation for quality specifications used to determine the
	/// sort order in quality specification lists.
	/// </summary>
	public class DataQualityCategoryComparer : IComparer<DataQualityCategory>
	{
		private readonly StringComparison _stringComparison;

		public DataQualityCategoryComparer(
			StringComparison stringComparison = StringComparison.CurrentCulture)
		{
			_stringComparison = stringComparison;
		}

		#region Implementation of IComparer<QualitySpecification>

		public int Compare(DataQualityCategory c1, DataQualityCategory c2)
		{
			if (c1 == null && c2 == null)
			{
				return 0;
			}

			if (c1 == null)
			{
				return 1;
			}

			if (c2 == null)
			{
				return -1;
			}

			int parentCompare = Compare(c1.ParentCategory, c2.ParentCategory);
			if (parentCompare != 0)
			{
				return parentCompare;
			}

			if (c1.ListOrder < c2.ListOrder)
			{
				return -1;
			}

			if (c1.ListOrder > c2.ListOrder)
			{
				return 1;
			}

			// list order is equal, sort based on names
			return string.Compare(c1.Name, c2.Name, _stringComparison);
		}

		#endregion
	}
}
