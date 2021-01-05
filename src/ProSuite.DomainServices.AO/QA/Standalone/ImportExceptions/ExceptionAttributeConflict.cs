using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public class ExceptionAttributeConflict
	{
		public ExceptionAttributeConflict(IssueAttribute attribute,
		                                  object updateValue,
		                                  object currentValue,
		                                  object originalValue,
		                                  [CanBeNull] string currentValueOrigin,
		                                  DateTime? currentValueImportDate)
		{
			Attribute = attribute;
			UpdateValue = updateValue;
			CurrentValue = currentValue;
			OriginalValue = originalValue;
			CurrentValueOrigin = currentValueOrigin;
			CurrentValueImportDate = currentValueImportDate;
		}

		public IssueAttribute Attribute { get; }

		[CanBeNull]
		public object UpdateValue { get; }

		[CanBeNull]
		public object CurrentValue { get; }

		[CanBeNull]
		public object OriginalValue { get; }

		[CanBeNull]
		public string CurrentValueOrigin { get; }

		public DateTime? CurrentValueImportDate { get; }
	}
}
