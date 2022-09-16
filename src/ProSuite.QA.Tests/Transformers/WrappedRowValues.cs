using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	public class WrappedRowValues : ReadOnlyRowBasedValues
	{
		public WrappedRowValues([NotNull] IReadOnlyRow row,
		                        bool appendBaseRowValue) : base(row)
		{
			if (appendBaseRowValue)
			{
				ExtraValue = new List<IReadOnlyRow> {row};
			}
		}
	}
}
