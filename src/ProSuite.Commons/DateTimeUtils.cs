using System;
using System.Globalization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	/// <summary>
	/// Helper methods for dealing with DateTime values
	/// </summary>
	public static class DateTimeUtils
	{
		// For DateTime format strings, see
		// http://msdn.microsoft.com/en-us/library/8kb3ddd4.aspx
		public const string TimestampFormat = "yyyyMMddHHmmss";

		/// <summary>
		/// Formats the specified nullable date time using a preconfigured TimestampFormat
		/// </summary>
		/// <param name="nullableDateTime">The nullable date time.</param>
		/// <returns></returns>
		/// <remarks>Warning: converts to UTC before formatting</remarks>
		public static string FormatTimestamp(DateTime? nullableDateTime)
		{
			return FormatTimestamp(nullableDateTime, string.Empty);
		}

		/// <summary>
		/// Formats the specified nullable date time using a preconfigured TimestampFormat
		/// </summary>
		/// <param name="nullableDateTime">The nullable date time.</param>
		/// <param name="nullString">The string to return if the DateTime is null.</param>
		/// <returns></returns>
		/// <remarks>Warning: converts to UTC before formatting</remarks>
		public static string FormatTimestamp(DateTime? nullableDateTime,
		                                     [CanBeNull] string nullString)
		{
			return nullableDateTime.HasValue
				       ? nullableDateTime.Value.ToUniversalTime()
				                         .ToString(TimestampFormat, CultureInfo.InvariantCulture)
				       : nullString ?? string.Empty;
		}

		public static bool IsToday(DateTime? dateTime)
		{
			return dateTime.HasValue && IsToday(dateTime.Value);
		}

		/// <summary>
		/// Determines whether the specified date time is today.
		/// </summary>
		/// <param name="dateTime">The date time.</param>
		/// <returns>
		/// 	<c>true</c> if the specified date time is today; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsToday(DateTime dateTime)
		{
			DateTime today = DateTime.Today;

			return dateTime.Year == today.Year &&
			       dateTime.Month == today.Month &&
			       dateTime.Day == today.Day;
		}

		/// <summary>
		/// Formats the specified nullable date time using a given DateTime format string
		/// </summary>
		/// <param name="nullableDateTime">The nullable date time.</param>
		/// <param name="format">The format.</param>
		/// <returns></returns>
		[NotNull]
		public static string Format([CanBeNull] DateTime? nullableDateTime,
		                            [NotNull] string format)
		{
			return Format(nullableDateTime, format, string.Empty);
		}

		/// <summary>
		/// Formats the specified nullable date time using a given DateTime format string
		/// </summary>
		/// <param name="nullableDateTime">The nullable date time.</param>
		/// <param name="format">The format.</param>
		/// <param name="nullString">The string to return if the DateTime is null.</param>
		/// <returns></returns>
		[NotNull]
		public static string Format([CanBeNull] DateTime? nullableDateTime,
		                            [NotNull] string format,
		                            [CanBeNull] string nullString)
		{
			return nullableDateTime.HasValue
				       ? nullableDateTime.Value.ToString(format)
				       : nullString ?? string.Empty;
		}

		/// <summary>
		/// Formats the specified time span in the format hh:mm:ss[.ffff]. The milliseconds can be hidden.
		/// </summary>
		/// <param name="timeSpan"></param>
		/// <param name="hideMilliseconds"></param>
		/// <returns></returns>
		public static string Format(TimeSpan timeSpan, bool hideMilliseconds)
		{
			// NOTE: TimeSpan.ToString(string format) is not supported in .NET 4.0 and higher
			int seconds = hideMilliseconds
				              ? timeSpan.Seconds +
				                (int) Math.Round((double) timeSpan.Milliseconds / 1000)
				              : timeSpan.Seconds;

			string result = hideMilliseconds
				                ? string.Format(
					                "{0:00}:{1:00}:{2:00}",
					                Math.Truncate(timeSpan.TotalHours),
					                timeSpan.Minutes,
					                seconds)
				                : string.Format(
					                "{0:00}:{1:00}:{2:00}.{3}",
					                Math.Truncate(timeSpan.TotalHours),
					                timeSpan.Minutes,
					                seconds,
					                timeSpan.Milliseconds);
			return result;
		}

		public static TimeSpan GetRandomizedInterval(
			double minTimeSec,
			double maxTimeSec,
			int? seed = null)
		{
			Random random = seed != null ? new Random(seed.Value) : new Random();

			double averageWaitTimeSec = maxTimeSec - minTimeSec;

			double waitSec = minTimeSec + averageWaitTimeSec * random.NextDouble();

			return TimeSpan.FromSeconds(waitSec);
		}

		public static bool IsLater(DateTime time, int thanHour, int thanMinute)
		{
			if (time.Hour > thanHour)
			{
				return true;
			}

			if (time.Hour == thanHour && time.Minute > thanMinute)
			{
				return true;
			}

			return false;
		}

		public static bool IsLater(int hour, int minute, DateTime thanTime)
		{
			if (hour > thanTime.Hour)
			{
				return true;
			}

			if (hour == thanTime.Hour && minute > thanTime.Minute)
			{
				return true;
			}

			return false;
		}

		public static bool IsWeekEnd(DateTime dateTime)
		{
			return dateTime.DayOfWeek == DayOfWeek.Saturday ||
			       dateTime.DayOfWeek == DayOfWeek.Sunday;
		}

		public static bool IsWeekDay(DateTime dateTime)
		{
			return ! IsWeekEnd(dateTime);
		}
	}
}
