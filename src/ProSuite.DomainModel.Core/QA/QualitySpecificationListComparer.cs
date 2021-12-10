using System;
using System.Collections.Generic;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// IComparer implementation for quality specifications used to determine the
	/// sort order in quality specification lists.
	/// </summary>
	public class QualitySpecificationListComparer : IComparer<QualitySpecification>
	{
		private readonly StringComparison _stringComparison;

		public QualitySpecificationListComparer(
			StringComparison stringComparison = StringComparison.CurrentCulture)
		{
			_stringComparison = stringComparison;
		}

		#region Implementation of IComparer<QualitySpecification>

		public int Compare(QualitySpecification q1, QualitySpecification q2)
		{
			if (q1 == null && q2 == null)
			{
				return 0;
			}

			if (q1 == null)
			{
				return 1;
			}

			if (q2 == null)
			{
				return -1;
			}

			if (q1.ListOrder < q2.ListOrder)
			{
				return -1;
			}

			if (q1.ListOrder > q2.ListOrder)
			{
				return 1;
			}

			// list order is equal, sort based on names
			return string.Compare(q1.Name, q2.Name, _stringComparison);
		}

		#endregion
	}
}
