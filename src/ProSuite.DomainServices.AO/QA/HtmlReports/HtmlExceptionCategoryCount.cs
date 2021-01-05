using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlExceptionCategoryCount : IEquatable<HtmlExceptionCategoryCount>
	{
		public HtmlExceptionCategoryCount([NotNull] ExceptionCategory category, int exceptionCount)
		{
			Name = category.Name ?? HtmlReportResources.HtmlTexts_UndefinedExceptionCategory;

			Category = category;
			ExceptionCount = exceptionCount;
		}

		[NotNull]
		[UsedImplicitly]
		public string Name { get; }

		[NotNull]
		public ExceptionCategory Category { get; }

		[UsedImplicitly]
		public int ExceptionCount { get; }

		public bool Equals(HtmlExceptionCategoryCount other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return string.Equals(Name, other.Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((HtmlExceptionCategoryCount) obj);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}
