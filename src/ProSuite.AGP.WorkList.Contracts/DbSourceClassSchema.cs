using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

// TODO: (DARO) move to ProSuite.AGP.WorkList. But that will cause a lot of moving types!
public class SourceClassSchema
{
	public SourceClassSchema([NotNull] string oidField, [CanBeNull] string shapeField)
	{
		OIDField = oidField;
		ShapeField = shapeField;
	}

	[CanBeNull]
	public string ShapeField { get; }
	[NotNull]
	public string OIDField { get; }
}

public class DbSourceClassSchema : SourceClassSchema
{
	public DbSourceClassSchema(string oidField, string shapeField,
	                           [NotNull] string statusField, int statusFieldIndex,
	                           [NotNull] object todoValue, [NotNull] object doneValue) : base(
		oidField, shapeField)
	{
		StatusFieldIndex = statusFieldIndex;
		StatusField = statusField;
		TodoValue = todoValue;
		DoneValue = doneValue;
	}

	public int StatusFieldIndex { get; }

	[NotNull]
	public string StatusField { get; }

	[NotNull]
	public object TodoValue { get; }

	[NotNull]
	public object DoneValue { get; }
}
