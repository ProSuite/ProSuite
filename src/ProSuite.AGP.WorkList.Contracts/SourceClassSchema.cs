using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

// TODO: (DARO) move to ProSuite.AGP.WorkList. But that will cause a lot of moving types!

public class SourceClassSchema
{
	public SourceClassSchema([NotNull] string oidField, [CanBeNull] string shapeField = null)
	{
		OIDField = oidField;
		ShapeField = shapeField;
	}

	[CanBeNull]
	public string ShapeField { get; }

	[NotNull]
	public string OIDField { get; }
}
