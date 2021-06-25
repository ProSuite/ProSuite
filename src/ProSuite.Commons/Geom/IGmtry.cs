using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
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