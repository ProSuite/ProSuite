using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProSuite.QA.Container.TestContainer
{
	public class CachedTableProps
	{
		public CachedTableProps(
			[NotNull] IReadOnlyTable table,
			IHasGeotransformation hasGeotransformation = null)
		{
			Table = table;
			HasGeotransformation = hasGeotransformation;
		}
		public IReadOnlyTable Table { get; }
		public double SearchDistance { get; set; }
		public IHasGeotransformation HasGeotransformation { get; }

		public override string ToString()
		{
			return $"{Table} props";
		}

		public bool Verify([CanBeNull] IHasGeotransformation otherGeotrans)
		{
			if (otherGeotrans != HasGeotransformation)
			{
				throw new InvalidOperationException($"{Table.Name} has differing geotransformations");
			}

			return true;
		}
	}

}
