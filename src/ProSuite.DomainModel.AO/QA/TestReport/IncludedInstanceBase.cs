using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public abstract class IncludedInstanceBase : IComparable<IncludedInstanceBase>
	{
		protected IncludedInstanceBase([NotNull] string title,
		                               [NotNull] Assembly assembly,
		                               bool obsolete,
		                               bool internallyUsed,
		                               [NotNull] IEnumerable<string> categories)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNull(assembly, nameof(assembly));
			Assert.ArgumentNotNull(categories, nameof(categories));

			Title = title;
			Assembly = assembly;
			Obsolete = obsolete;
			InternallyUsed = internallyUsed;
			Categories = new List<string>(categories);
		}

		[NotNull]
		public string Title { get; }

		[NotNull]
		public Assembly Assembly { get; }

		public bool Obsolete { get; }

		public bool InternallyUsed { get; }

		[NotNull]
		public IList<string> Categories { get; }

		[NotNull]
		public abstract string Key { get; }

		public abstract string IndexTooltip { get; }

		public abstract string Description { get; }

		public abstract Type InstanceType { get; }

		public virtual IList<IssueCode> IssueCodes => null;

		#region IComparable<IncludedInstanceBase> Members

		public int CompareTo(IncludedInstanceBase other)
		{
			return string.Compare(Title, other.Title, StringComparison.CurrentCulture);
		}

		#endregion
	}
}
