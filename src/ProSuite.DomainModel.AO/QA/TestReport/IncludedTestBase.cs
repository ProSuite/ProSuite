using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal abstract class IncludedTestBase : IComparable<IncludedTestBase>
	{
		private readonly Assembly _assembly;
		private readonly bool _obsolete;
		private readonly bool _internallyUsed;
		private readonly string _title;
		private readonly IList<string> _categories;

		protected IncludedTestBase([NotNull] string title,
		                           [NotNull] Assembly assembly,
		                           bool obsolete,
		                           bool internallyUsed,
		                           [NotNull] IEnumerable<string> categories)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNull(assembly, nameof(assembly));
			Assert.ArgumentNotNull(categories, nameof(categories));

			_title = title;
			_assembly = assembly;
			_obsolete = obsolete;
			_internallyUsed = internallyUsed;
			_categories = new List<string>(categories);
		}

		[NotNull]
		public string Title
		{
			get { return _title; }
		}

		public bool Obsolete
		{
			get { return _obsolete; }
		}

		public bool InternallyUsed
		{
			get { return _internallyUsed; }
		}

		[NotNull]
		public abstract string Key { get; }

		[NotNull]
		public Assembly Assembly
		{
			get { return _assembly; }
		}

		[NotNull]
		public IList<string> Categories
		{
			get { return _categories; }
		}

		[NotNull]
		public string GetCommaSeparatedCategories()
		{
			return StringUtils.ConcatenateSorted(_categories, ", ");
		}

		public abstract string IndexTooltip { get; }

		public abstract string Description { get; }

		public abstract Type TestType { get; }

		public virtual IList<IssueCode> IssueCodes
		{
			get { return null; }
		}

		#region IComparable<IncludedTest> Members

		public int CompareTo(IncludedTestBase other)
		{
			return string.Compare(Title, other.Title, StringComparison.CurrentCulture);
		}

		#endregion
	}
}