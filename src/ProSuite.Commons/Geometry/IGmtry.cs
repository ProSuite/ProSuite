using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	public interface IGmtry
	{
		int Dimension { get; }

		[NotNull]
		IBox Extent { get; }

		[CanBeNull]
		IGmtry Border { get; }

		bool Intersects([NotNull] IBox box);
	}
}