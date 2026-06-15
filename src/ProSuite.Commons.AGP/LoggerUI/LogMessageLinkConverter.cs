using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ProSuite.Commons.AGP.LoggerUI;

// TODO should be this moved to common utils, because GPService need this too?
public static class HtmlTextUtils
{
	private static readonly Regex _regex =
		new Regex("(?<=>)(.*?)(?=<)", RegexOptions.Singleline);

	public static string GetTagText(string htmlString)
	{
		var match = _regex.Match(htmlString);
		return match.Success ? match.Value : string.Empty;
	}
}

// TODO - measure timing for Converter 
public class LogMessageLinkConverter : IValueConverter
{
	public object Convert(
		object value,
		Type targetType,
		object parameter,
		CultureInfo culture)
	{
		string logMsg = System.Convert.ToString(value);
		string linkMsg = HtmlTextUtils.GetTagText(logMsg);
		string paramName = System.Convert.ToString(parameter);

		switch (paramName)
		{
			case "first":
				return linkMsg == "" ? logMsg : logMsg.Before("<");

			case "link":
				return linkMsg;

			case "last":
				return linkMsg != "" ? logMsg.After(">") : string.Empty;
		}

		return logMsg;
	}

	public object ConvertBack(
		object value,
		Type targetType,
		object parameter,
		CultureInfo culture)
	{
		//Put reverse logic here
		throw new NotImplementedException();
	}
}

// TODO this could be somewhere in utils
public static class SubstringExtensions
{
	/// <summary>
	/// Get string value between [first] a and [last] b.
	/// </summary>
	public static string Between(this string value, string a, string b)
	{
		int posA = value.IndexOf(a, StringComparison.Ordinal);
		int posB = value.LastIndexOf(b, StringComparison.Ordinal);
		if (posA == -1 || posB == -1)
		{
			return string.Empty;
		}

		int adjustedPosA = posA + a.Length;
		if (adjustedPosA >= posB)
		{
			return string.Empty;
		}

		return value.Substring(adjustedPosA, posB - adjustedPosA);
	}

	/// <summary>
	/// Get string value after [first] a.
	/// </summary>
	public static string Before(this string value, string a)
	{
		int posA = value.IndexOf(a, StringComparison.Ordinal);
		if (posA == -1)
		{
			return string.Empty;
		}

		return value.Substring(0, posA);
	}

	/// <summary>
	/// Get string value after [last] a.
	/// </summary>
	public static string After(this string value, string a)
	{
		int posA = value.LastIndexOf(a, StringComparison.Ordinal);
		if (posA == -1)
		{
			return string.Empty;
		}

		int adjustedPosA = posA + a.Length;
		if (adjustedPosA >= value.Length)
		{
			return string.Empty;
		}

		return value.Substring(adjustedPosA);
	}
}
