using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Help;

public static class AboutUtils
{
	public static string GetPlainText(IList<AboutItem> items)
	{
		if (items is null)
		{
			return string.Empty;
		}

		// Order sections by first occurrence in the list:

		int index = 0;
		var sectionOrder = new Dictionary<string, int>();
		foreach (var item in items)
		{
			var section = item.Section ?? string.Empty; // cannot use null as dict key
			sectionOrder.TryAdd(section, ++index);
		}

		string lastSection = null;
		var buffer = new StringBuilder();
		var groups = items.GroupBy(item => item.Section)
		                  .OrderBy(g => sectionOrder.GetValueOrDefault(g.Key, int.MaxValue));

		foreach (var group in groups)
		{
			if (group.Key != lastSection)
			{
				buffer.AppendLine();
				buffer.AppendLine(group.Key);
				lastSection = group.Key;
			}

			foreach (var item in group)
			{
				if (item.Key == "-")
				{
					buffer.Append($"  - {item.Value}");
				}
				else if (string.IsNullOrWhiteSpace(item.Key))
				{
					buffer.Append($"- {item.Value}");
				}
				else
				{
					buffer.Append($"- {item.Key}: {item.Value}");
				}

				if (! string.IsNullOrEmpty(item.Remark))
				{
					buffer.Append($" ({item.Remark})");
				}

				buffer.AppendLine();
			}
		}

		return buffer.Trim().ToString();
	}
}
