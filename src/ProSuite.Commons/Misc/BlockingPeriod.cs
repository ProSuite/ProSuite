using System;

namespace ProSuite.Commons.Misc
{
	/// <summary>
	/// Represents a blocking period for some operation. May be used e.g. for
	/// filtering out multiple repeated events (observed for toolStripButtons)
	/// </summary>
	public class BlockingPeriod
	{
		private readonly long _durationTicks;
		private long _startedTicks;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BlockingPeriod"/> class.
		/// </summary>
		/// <param name="durationMilliseconds">The duration of the blocking 
		/// period, in milliseconds.</param>
		public BlockingPeriod(int durationMilliseconds)
		{
			_durationTicks = durationMilliseconds * TimeSpan.TicksPerMillisecond;
		}

		#endregion

		/// <summary>
		/// Starts the blocking period.
		/// </summary>
		public void Start()
		{
			_startedTicks = DateTime.Now.Ticks;
		}

		/// <summary>
		/// Determines whether the current point in time is still within the
		/// blocking period.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the current point in time is within the
		/// blocking period; otherwise, <c>false</c>.
		/// </returns>
		public bool IsWithin()
		{
			return DateTime.Now.Ticks - _startedTicks < _durationTicks;
		}
	}
}
