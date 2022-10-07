using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Globalization
{
	public static class CultureInfoUtils
	{
		/// <summary>
		/// Descriptive text for cultureInfo, example:
		/// 
		/// en-US, LCID=1033, English (United States)
		/// Formatting Samples: 1234567.89, 3/20/2012 12:18:43 PM
		/// Region: United States (US), non-metric, currency: USD ($)
		/// </summary>
		[NotNull]
		public static string GetCultureInfoDescription([NotNull] CultureInfo cultureInfo)
		{
			Assert.ArgumentNotNull(cultureInfo, nameof(cultureInfo));

			var sb = new StringBuilder();

			sb.Append(CultureInfo.InvariantCulture.Equals(cultureInfo)
				          ? "Invariant Culture"
				          : cultureInfo.Name);

			if (cultureInfo.IsNeutralCulture)
			{
				sb.Append(" (neutral)");
			}

			sb.AppendFormat(", LCID={0}, {1}", cultureInfo.LCID, cultureInfo.EnglishName);
			sb.AppendLine();

			// Don't try to format (or parse) with a neutral culture. It will fail with an exception.
			if (! cultureInfo.IsNeutralCulture)
			{
				// Format specifier N will use group separators, the default will not.
				sb.AppendFormat(cultureInfo, "Formatting Samples: {0:N}, {1:F}",
				                1234567.89,
				                DateTime.Now);
				sb.AppendLine();

				if (! CultureInfo.InvariantCulture.Equals(cultureInfo))
				{
					try
					{
						var regionInfo = CreateRegionInfo(cultureInfo);
						sb.AppendFormat("Region: {0} ({1}), {2}, currency: {3} ({4})",
						                regionInfo.DisplayName,
						                regionInfo.TwoLetterISORegionName,
						                regionInfo.IsMetric
							                ? "metric"
							                : "non-metric",
						                regionInfo.ISOCurrencySymbol,
						                regionInfo.CurrencySymbol);
					}
					catch (Exception ex)
					{
						sb.AppendFormat(
							"Error getting region info for {0}: {1}",
							cultureInfo.Name, ex.Message);
					}
				}
			}

			return sb.ToString();
		}

		public static void ExecuteUsing([NotNull] CultureInfo culture,
		                                [NotNull] Action action)
		{
			ExecuteUsing(culture, null, action);
		}

		public static void ExecuteUsing([NotNull] string culture,
		                                [NotNull] Action action)
		{
			ExecuteUsing(culture, null, action);
		}

		public static void ExecuteUsing([NotNull] string culture,
		                                [CanBeNull] string uiCulture,
		                                [NotNull] Action action)
		{
			ExecuteUsing(CultureInfo.GetCultureInfo(culture),
			             string.IsNullOrEmpty(uiCulture)
				             ? null
				             : CultureInfo.GetCultureInfo(uiCulture),
			             action);
		}

		public static void ExecuteUsing([NotNull] CultureInfo culture,
		                                [CanBeNull] CultureInfo uiCulture,
		                                [NotNull] Action action)
		{
			Assert.ArgumentNotNull(culture, nameof(culture));
			Assert.ArgumentNotNull(action, nameof(action));

			Thread thread = Thread.CurrentThread;

			CultureInfo origCulture = thread.CurrentCulture;
			CultureInfo origUiCulture = thread.CurrentUICulture;

			try
			{
				thread.CurrentCulture = culture;

				if (uiCulture != null)
				{
					thread.CurrentUICulture = uiCulture;
				}

				action();
			}
			finally
			{
				thread.CurrentCulture = origCulture;
				thread.CurrentUICulture = origUiCulture;
			}
		}

		public static T ExecuteUsing<T>([NotNull] CultureInfo culture,
		                                [NotNull] Func<T> function)
		{
			return ExecuteUsing(culture, null, function);
		}

		public static T ExecuteUsing<T>([NotNull] CultureInfo culture,
		                                [CanBeNull] CultureInfo uiCulture,
		                                [NotNull] Func<T> function)
		{
			Assert.ArgumentNotNull(culture, nameof(culture));
			Assert.ArgumentNotNull(function, nameof(function));

			Thread thread = Thread.CurrentThread;

			CultureInfo origCulture = thread.CurrentCulture;
			CultureInfo origUiCulture = thread.CurrentUICulture;

			try
			{
				thread.CurrentCulture = culture;

				if (uiCulture != null)
				{
					thread.CurrentUICulture = uiCulture;
				}

				return function();
			}
			finally
			{
				thread.CurrentCulture = origCulture;
				thread.CurrentUICulture = origUiCulture;
			}
		}

		[NotNull]
		private static RegionInfo CreateRegionInfo([NotNull] CultureInfo cultureInfo)
		{
			try
			{
				return new RegionInfo(cultureInfo.LCID);
			}
			catch (ArgumentException)
			{
				// "Customized cultures cannot be passed by LCID, only by name"
				return new RegionInfo(cultureInfo.Name);
			}
		}

		#region Nested type: TempCultureInfo

		/// <summary>
		/// Set Thread.CurrentThread.CurrentCulture until disposed.
		/// Usage pattern:
		/// <code>using (new CultureInfoUtils.TempCultureInfo(cultureInfo)) { lengthy activity }</code>
		/// </summary>
		public class TempCultureInfo : IDisposable
		{
			private readonly CultureInfo _origCulture;

			public TempCultureInfo([NotNull] CultureInfo cultureInfo)
			{
				Assert.ArgumentNotNull(cultureInfo, nameof(cultureInfo));

				_origCulture = Thread.CurrentThread.CurrentCulture;

				Thread.CurrentThread.CurrentCulture = cultureInfo;
			}

			public void Dispose()
			{
				Thread.CurrentThread.CurrentCulture = _origCulture;
			}
		}

		#endregion
	}
}
