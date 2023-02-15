using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.Carto;

public static class CubicBezier
{
	/// <summary>
	/// Degree elevation from quadratic to cubic: given a quadratic
	/// (2nd order) Bezier curve, find the cubic (3rd order) Bezier
	/// curve that has the same shape. Let the quadratic be defined
	/// by points Q0 Q1 Q2, and the cubic by points P0 P1 P2 P3.
	/// Since the first and last points must be the same, we have
	/// P0 = Q0 and P3 = Q2. This function computes the intermediate
	/// points P1 and P2, given the three points of the quadratic curve.
	/// See https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Degree_elevation
	/// </summary>
	public static void FromQuadratic(Pair q0, Pair q1, Pair q2, out Pair p1, out Pair p2)
	{
		const double c1 = 1.0 / 3.0;
		const double c2 = 2.0 / 3.0;
		p1 = c1 * q0 + c2 * q1;
		p2 = c2 * q1 + c1 * q2;
	}
}
