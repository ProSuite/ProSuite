using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

// todo (daro): rename to DatabaseSourceClassSchema
public class DbSourceClassSchema : SourceClassSchema
{
	public DbSourceClassSchema([NotNull] string statusField,
	                           [NotNull] object todoValue, [NotNull] object doneValue,
	                           [NotNull] Dictionary<string, int> subFields) : base(subFields)
	{
		StatusField = statusField;
		TodoValue = todoValue;
		DoneValue = doneValue;
	}

	[NotNull]
	public string StatusField { get; }

	[NotNull]
	public object TodoValue { get; }

	[NotNull]
	public object DoneValue { get; }
}
