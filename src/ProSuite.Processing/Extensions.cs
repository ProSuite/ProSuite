using ProSuite.Commons;

namespace ProSuite.Processing
{
	public static class Extensions
	{
		// Notice: this seems the right place for CP specific extensions.
		// Presently, many are scattered over the utils classes.

		public static double RoundToSignificantDigits(this double value, int digits)
		{
			return MathUtils.RoundToSignificantDigits(value, digits);
		}
	}
}
