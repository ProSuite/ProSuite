using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public interface IConnectLineCalculator
	{
		/// <summary>
		/// Finds the first possible connection to the adjust target line.
		/// </summary>
		/// <param name="curveToConnectTo">Source part curve</param>
		/// <param name="curveToSearch"></param>
		/// <param name="searchForward">Direction to search</param>
		/// <param name="fallBackConnection"></param>
		/// <returns>Index of the first adjustable point</returns>
		[CanBeNull]
		IPath FindConnection([NotNull] ICurve curveToConnectTo,
		                     [NotNull] ICurve curveToSearch,
		                     bool searchForward,
		                     [CanBeNull] out IPath fallBackConnection);

		/// <summary>
		/// Determines whether the candidate point is valid to use as start point of 
		/// a connect line.
		/// </summary>
		/// <param name="candidatePoint"></param>
		/// <param name="curveToConnectTo"></param>
		/// <param name="connectionLine"></param>
		/// <param name="pointToConnectTo"></param>
		/// <returns></returns>
		[ContractAnnotation(
			"=>true, connectionLine:notnull,pointToConnectTo:notnull; =>false, connectionLine:canbenull,pointToConnectTo:canbenull")]
		bool IsValidConnectablePoint([NotNull] IPoint candidatePoint,
		                             [NotNull] ICurve curveToConnectTo,
		                             [CanBeNull] out IPath connectionLine,
		                             [CanBeNull] out IPoint pointToConnectTo);
	}
}
