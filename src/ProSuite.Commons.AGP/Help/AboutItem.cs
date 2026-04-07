using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Help;

/// <summary>
/// A row in the about box, consisting of a <see cref="Section"/>
/// (for grouping), a <see cref="Key"/> and a <see cref="Value"/>.
/// Optionally, a short explanatory <see cref="Remark"/> may appear.
/// </summary>
public class AboutItem
{
	[PublicAPI] public string Section { get; }
	[PublicAPI] public string Key { get; }
	[PublicAPI] public string Value { get; }
	[PublicAPI] public string Remark { get; }

	public AboutItem(string section, [NotNull] string key, string value, string remark = null)
	{
		Section = section; // can be null
		Key = key ?? throw new ArgumentNullException(nameof(key));
		Value = value ?? string.Empty;
		Remark = remark;
	}
}
