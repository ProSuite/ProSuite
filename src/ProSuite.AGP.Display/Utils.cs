using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ArcGIS.Core.CIM;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.Display;

public static class Utils
{
	/// <returns>Enumeration of all unique value classes (across all groups);
	/// aware that there may be no groups or no classes; never null</returns>
	public static IEnumerable<CIMUniqueValueClass> GetUniqueValueClasses(CIMUniqueValueRenderer renderer)
	{
		// there may be no groups or no classes per group!
		var none = Array.Empty<CIMUniqueValueClass>();

		if (renderer.Groups is null) return none;
		return renderer.Groups.SelectMany(group => group.Classes ?? none);
	}

	/// <returns>A string representation of the given <paramref name="values"/> from
	/// a unique value renderer class; values are separated by comma, value combinations
	/// by semicolon, and a backslash is used for escaping where necessary.</returns>
	public static string FormatDiscriminatorValue(CIMUniqueValue[] values)
	{
		if (values is null || values.Length <= 0) return null;

		// example: 1,foo,2;-99,bar\,z,0

		const char sep1 = ';';
		const char sep2 = ',';
		const char esc = '\\';
		var sb = new StringBuilder();

		for (int i = 0; i < values.Length; i++)
		{
			if (i > 0) sb.Append(sep1);
			var fieldValues = values[i].FieldValues;
			for (int j = 0; j < fieldValues.Length; j++)
			{
				if (j > 0) sb.Append(sep2);
				string text = fieldValues[j];
				foreach (char c in text)
				{
					if (c is sep1 or sep2)
					{
						sb.Append(esc);
					}

					sb.Append(c);
				}
			}
		}

		return sb.ToString();
	}

	public static string GetPrettyTypeName(CIMObject cim)
	{
		if (cim is null) return null;
		var name = cim.GetType().Name;
		return name.StartsWith("CIM") ? name.Substring(3) : name;
	}

	/// <summary>
	/// Join the given strings into one string, omitting the longest
	/// prefixes and suffixes that are common among all strings.
	/// </summary>
	public static string JoinInfix(string sep, IEnumerable<string> names)
	{
		if (names is null) return string.Empty;

		var list = names.Distinct().ToList();
		if (list.Count <= 0) return string.Empty;
		if (list.Count == 1) return list.Single();

		var shortest = list.MinBy(s => s.Length);

		var pre = shortest.TakeWhile((c, i) => list.All(n => c == n[i])).Count();
		var post = shortest.Reverse().TakeWhile((c, i) => list.All(n => c == n[n.Length - 1 - i]))
		                   .Count();

		return string.Join(sep ?? string.Empty,
		                   list.Select(n => n.Substring(pre, n.Length - pre - post)));
	}

	public static void ShowFeedback(ImportSLDLMButtonBase.IFeedback feedback, Window owner = null)
	{
		const string caption = "SLD/LM Config Validation";
		const int maxMessages = 8;

		ShowFeedback(feedback, maxMessages, caption, owner);
	}

	public static void ShowFeedback(ImportSLDLMButtonBase.IFeedback feedback, int maxMessages, string caption, Window owner = null)
	{
		if (feedback is null)
			throw new ArgumentNullException(nameof(feedback));

		caption ??= "Feedback Messages";
		var sb = new StringBuilder();
		MessageBoxImage icon;

		if (feedback.Errors > 0)
		{
			sb.AppendLine("Validation failed:");
			AppendMessages(sb, feedback, maxMessages);
			icon = MessageBoxImage.Error;
		}
		else if (feedback.Warnings > 0)
		{
			sb.AppendLine("Validated with warnings:");
			AppendMessages(sb, feedback, maxMessages);
			icon = MessageBoxImage.Warning;
		}
		else
		{
			sb.AppendLine("Validation successful");
			icon = MessageBoxImage.Information;
		}

		sb.TrimEnd();

		if (owner is null)
		{
			MessageBox.Show(sb.ToString(), caption, MessageBoxButton.OK, icon);
		}
		else
		{
			MessageBox.Show(owner, sb.ToString(), caption, MessageBoxButton.OK, icon);
		}
	}

	private static void AppendMessages(StringBuilder sb, ImportSLDLMButtonBase.IFeedback feedback, int maxMessages)
	{
		var messages = feedback.Messages.ToList();

		foreach (var message in messages.Take(maxMessages))
		{
			sb.Append("- ");
			sb.AppendLine(message);
		}

		var excess = messages.Count - maxMessages;

		if (excess > 1)
		{
			sb.AppendLine($"- (and {excess} more messages)");
		}
		else if (excess == 1)
		{
			sb.AppendLine("- (and one more message)");
		}
	}
}
