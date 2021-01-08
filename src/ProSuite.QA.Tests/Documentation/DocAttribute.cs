using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Globalization;

namespace ProSuite.QA.Tests.Documentation
{
	/// <summary>
	/// Attribute for documenting test classes and constructor parameters. Supports localization by
	/// looking up string resources in <see cref="DocStrings"></see>
	/// </summary>
	[CLSCompliant(false)]
	public class DocAttribute : LocalizedDescriptionAttribute
	{
		private readonly string _resourceName;

		public DocAttribute([NotNull] string resourceName)
			: base(DocStrings.ResourceManager, resourceName)
		{
			_resourceName = resourceName;
		}

		public string ResourceName => _resourceName;
	}
}
