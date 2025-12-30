using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public class WorkListStatusSchema
{
	public WorkListStatusSchema([NotNull] string fieldName,
	                            int fieldIndex,
	                            [NotNull] object todoValue,
	                            [NotNull] object doneValue)
	{
		Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
		Assert.ArgumentCondition(fieldIndex > -1, "field index is negative");
		Assert.ArgumentNotNull(todoValue, nameof(todoValue));
		Assert.ArgumentNotNull(doneValue, nameof(doneValue));

		FieldName = fieldName;
		FieldIndex = fieldIndex;
		TodoValue = todoValue;
		DoneValue = doneValue;
	}

	public int FieldIndex { get; }

	[NotNull]
	public string FieldName { get; }

	[NotNull]
	public object TodoValue { get; }

	[NotNull]
	public object DoneValue { get; }
}
