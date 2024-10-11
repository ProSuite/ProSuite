using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geometry.API
{
	public interface ILine : ISegment
	{
		double Angle { get; }
		double Length2dSquared { get; }

		/// <summary>
		/// Returns - larger 0 for testPoint left of the line from lineStart to lineEnd
		///         - 0 for testPoint on the line
		///         - smaller 0 for test point right of the line
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		double IsLeftXY([NotNull] IPoint testPoint);
	}
}
