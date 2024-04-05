using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Tests.Documentation
{
	/// <summary>
	/// Attribute for documenting test classes and constructor parameters. Supports localization by
	/// looking up string resources in <see cref="DocStrings"></see>
	/// </summary>
	public class DocAttribute : LocalizedDescriptionAttribute
	{
		public DocAttribute([NotNull] string resourceName)
			: base(DocStrings.ResourceManager, resourceName) { }
	}
}
