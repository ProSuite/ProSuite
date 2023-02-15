using ProSuite.Commons;
using ProSuite.Commons.Logging;

namespace ProSuite.Processing
{
	public static class Extensions
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// Notice: this seems the right place for CP specific extensions.
		// Presently, many are scattered over the utils classes.

		public static double RoundToSignificantDigits(this double value, int digits)
		{
			return MathUtils.RoundToSignificantDigits(value, digits);
		}
	}
}
