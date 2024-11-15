using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

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
			SubFields = new TableSubFields(table, false);
		}

		public IReadOnlyTable Table { get; }
		public double SearchDistance { get; set; }
		public IHasGeotransformation HasGeotransformation { get; }

		internal TableSubFields SubFields { get; set; }

		public override string ToString()
		{
			return $"{Table} props";
		}

		public bool Verify([CanBeNull] IHasGeotransformation otherGeotrans)
		{
			if (otherGeotrans != HasGeotransformation)
			{
				throw new InvalidOperationException(
					$"{Table.Name} has differing geotransformations");
			}

			return true;
		}

		public bool AdaptSubFields(string subFields, out string adaptedSubfields)
		{
			return SubFields.AdaptSubFields(subFields, out adaptedSubfields);
		}
	}
}
