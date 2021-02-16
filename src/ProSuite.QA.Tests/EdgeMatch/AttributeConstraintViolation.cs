using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.EdgeMatch
{
	public class AttributeConstraintViolation
	{
		internal AttributeConstraintViolation([NotNull] string description,
		                                      [NotNull] IEnumerable<string> fieldNames,
		                                      [NotNull] string textValue) :
			this(description, FormatAffectedComponents(fieldNames), textValue) { }

		internal AttributeConstraintViolation([NotNull] string description,
		                                      [NotNull] string affectedComponents,
		                                      [NotNull] string textValue)
		{
			Assert.ArgumentNotNullOrEmpty(description, nameof(description));
			Assert.ArgumentNotNullOrEmpty(affectedComponents, nameof(affectedComponents));
			Assert.ArgumentNotNullOrEmpty(textValue, nameof(textValue));

			Description = description;
			AffectedComponents = affectedComponents;
			TextValue = textValue;
		}

		[NotNull]
		public string Description { get; }

		[NotNull]
		public string AffectedComponents { get; }

		[NotNull]
		public string TextValue { get; }

		[NotNull]
		private static string FormatAffectedComponents(
			[NotNull] IEnumerable<string> fieldNames)
		{
			Assert.ArgumentNotNull(fieldNames, nameof(fieldNames));
			List<string> fieldList = fieldNames.ToList();
			Assert.ArgumentCondition(fieldList.Count > 0, "At least one field expected");

			return Assert.NotNullOrEmpty(EdgeMatchUtils.FormatAffectedComponents(fieldList));
		}
	}
}
