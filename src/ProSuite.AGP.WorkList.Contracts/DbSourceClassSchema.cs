using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

// todo (daro): rename to DatabaseSourceClassSchema
public class DbSourceClassSchema : SourceClassSchema
{
	public DbSourceClassSchema(string oidField, string shapeField,
	                           [NotNull] string statusField, int statusFieldIndex,
	                           [NotNull] object todoValue, [NotNull] object doneValue,
	                           params string[] additionalSubFields) : base(
		oidField, shapeField)
	{
		StatusFieldIndex = statusFieldIndex;
		StatusField = statusField;
		TodoValue = todoValue;
		DoneValue = doneValue;
		AdditionalSubFields = additionalSubFields;
	}

	public int StatusFieldIndex { get; }

	[NotNull]
	public string StatusField { get; }

	[NotNull]
	public object TodoValue { get; }

	[NotNull]
	public object DoneValue { get; }

	public string[] AdditionalSubFields { get; }
}
