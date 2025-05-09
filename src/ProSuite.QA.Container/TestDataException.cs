using System;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class TestDataException : Exception
	{
		public TestDataException([NotNull] string message,
		                         [CanBeNull] IReadOnlyRow row)
			: base(message)
		{
			if (row != null)
			{
				DataReference = new RowReference(row, false);
			}
		}

		public TestDataException([NotNull] string message,
		                         [NotNull] IDataReference dataReference,
		                         [CanBeNull] Exception innerException = null)
			: base(message, innerException)
		{
			DataReference = dataReference;
		}

		[CanBeNull]
		public IDataReference DataReference { get; }
	}
}
