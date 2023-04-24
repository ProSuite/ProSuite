using System;
using System.Collections.Generic;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Selection
{
	public class FeatureClassSelectionComparer : IComparer<FeatureClassSelection>
	{
		[CanBeNull] private readonly IComparer<FeatureClassSelection> _baseCompare;
		//[CanBeNull] private readonly Func<FeatureClassSelection, FeatureClassSelection, int> _baseCompare;


		public FeatureClassSelectionComparer(
			[CanBeNull] IComparer<FeatureClassSelection> baseCompare)
		{
			_baseCompare = baseCompare;
		}

		//public FeatureClassSelectionComparer(
		//	[CanBeNull] Func<FeatureClassSelection, FeatureClassSelection, int> baseCompare)
		//{
		//	_baseCompare = baseCompare;
		//}

		public int Compare(FeatureClassSelection x, FeatureClassSelection y)
		{
			if (x == y)
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (_baseCompare != null)
			{
				return _baseCompare.Compare(x, y);
			}

			// todo daro assert
			throw new ArgumentOutOfRangeException();
		}
	}
}
