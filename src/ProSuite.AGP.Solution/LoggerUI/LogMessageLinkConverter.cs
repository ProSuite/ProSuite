using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ProSuite.AGP.Solution.LoggerUI
{
	// TODO should be this moved to common utils, because GPService need this too?
	class HtmlTextUtils
	{
		private static readonly Regex _regex = new Regex("(?<=>)(.*?)(?=<)", RegexOptions.Singleline);

		public static string GetTagText(string htmlString)
		{
			var match = _regex.Match(htmlString);
			if (match.Success)
			{

				return match.Value;
			}
			return "";
		}
	}

	// TODO - measure timing for Converter 
	public class LogMessageLinkConverter : IValueConverter
	{
		public object Convert(
			object value,
			Type targetType,
			object parameter,
			System.Globalization.CultureInfo culture)
		{
			string logMsg = value.ToString();
			string linkMsg = HtmlTextUtils.GetTagText(logMsg);

			switch (parameter.ToString())
			{
				case "first":
					if (linkMsg == "")
						return logMsg;
					else
						return logMsg.Before("<");

				case "link":
					return linkMsg;

				case "last":
					if (linkMsg != "")
						return logMsg.After(">");
					else
						return "";

			}
			return logMsg;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			System.Globalization.CultureInfo culture)
		{
			//Put reverse logic here
			throw new NotImplementedException();
		}
	}

	// TODO this could be somewhere in utils
	static class SubstringExtensions
	{
		/// <summary>
		/// Get string value between [first] a and [last] b.
		/// </summary>
		public static string Between(this string value, string a, string b)
		{
			int posA = value.IndexOf(a);
			int posB = value.LastIndexOf(b);
			if (posA == -1)
			{
				return "";
			}
			if (posB == -1)
			{
				return "";
			}
			int adjustedPosA = posA + a.Length;
			if (adjustedPosA >= posB)
			{
				return "";
			}
			return value.Substring(adjustedPosA, posB - adjustedPosA);
		}

		/// <summary>
		/// Get string value after [first] a.
		/// </summary>
		public static string Before(this string value, string a)
		{
			int posA = value.IndexOf(a);
			if (posA == -1)
			{
				return "";
			}
			return value.Substring(0, posA);
		}

		/// <summary>
		/// Get string value after [last] a.
		/// </summary>
		public static string After(this string value, string a)
		{
			int posA = value.LastIndexOf(a);
			if (posA == -1)
			{
				return "";
			}
			int adjustedPosA = posA + a.Length;
			if (adjustedPosA >= value.Length)
			{
				return "";
			}
			return value.Substring(adjustedPosA);
		}
	}

}
