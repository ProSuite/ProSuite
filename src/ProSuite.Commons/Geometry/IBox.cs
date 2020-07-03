using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	/// <summary>
	/// Summary description for IGeometry.
	/// </summary>
	public interface IBox : IGmtry
	{
		[NotNull]
		IPnt Min { get; }

		[NotNull]
		IPnt Max { get; }

		double GetMaxExtent();

		[NotNull]
		IBox Clone();

		bool Contains([NotNull] IBox box);

		bool Contains([NotNull] IBox box, [NotNull] int[] dimensionList);

		bool Contains([NotNull] IPnt point);

		bool Contains([NotNull] IPnt point, [NotNull] int[] dimensionList);

		void Include([NotNull] IBox box);
	}
}