using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.Reports
{
	public abstract class ReportBuilderBase : IReportBuilder
	{
		public bool IncludeAssemblyInfo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete instance descriptors are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests, factories, transformers or filters should be
		/// included; otherwise, <c>false</c>.</value>
		public bool IncludeObsolete { get; set; }

		protected IDictionary<Type, IncludedInstanceClass> IncludedTestClasses { get; } =
			new Dictionary<Type, IncludedInstanceClass>();

		protected IDictionary<Type, IncludedInstanceClass> IncludedTransformerClasses { get; } =
			new Dictionary<Type, IncludedInstanceClass>();

		protected IDictionary<Type, IncludedInstanceClass> IncludedFilterClasses { get; } =
			new Dictionary<Type, IncludedInstanceClass>();

		protected List<IncludedTestFactory> IncludedTestFactories { get; } =
			new List<IncludedTestFactory>();

		public void IncludeTestFactory(Type testFactoryType)
		{
			var testFactory = new IncludedTestFactory(testFactoryType);

			if (! IncludeObsolete && testFactory.Obsolete)
			{
				return;
			}

			if (testFactory.InternallyUsed)
			{
				return;
			}

			IncludedTestFactories.Add(testFactory);
		}

		public void IncludeTransformer(Type transformerType, int constructorIndex)
		{
			Include(transformerType, constructorIndex, IncludedTransformerClasses);
		}

		public void IncludeIssueFilter(Type issueFilterType, int ctorIndex)
		{
			Include(issueFilterType, ctorIndex, IncludedFilterClasses);
		}

		public void IncludeTest(Type testType, int constructorIndex)
		{
			Include(testType, constructorIndex, IncludedTestClasses);
		}

		public abstract void AddHeaderItem(string name, string value);

		public abstract void WriteReport();

		private void Include(Type instanceDescriptorType, int constructorIndex,
		                     IDictionary<Type, IncludedInstanceClass> result)
		{
			var isNewInstance = false;
			IncludedInstanceClass classToInclude;
			if (! result.TryGetValue(instanceDescriptorType, out classToInclude))
			{
				classToInclude = new IncludedInstanceClass(instanceDescriptorType);

				if (! IncludeObsolete && classToInclude.Obsolete)
				{
					return;
				}

				if (classToInclude.InternallyUsed)
				{
					return;
				}

				// this test class is to be added, if the constructor is not obsolete
				isNewInstance = true;
			}

			if (classToInclude.Obsolete)
			{
				return;
			}

			IncludedInstanceConstructor instanceConstructor =
				classToInclude.CreateInstanceConstructor(constructorIndex);

			if (! IncludeObsolete && instanceConstructor.Obsolete)
			{
				return;
			}

			if (instanceConstructor.InternallyUsed)
			{
				return;
			}

			classToInclude.IncludeConstructor(instanceConstructor);

			if (isNewInstance)
			{
				result.Add(instanceDescriptorType, classToInclude);
			}
		}

		[NotNull]
		protected IEnumerable<IncludedInstanceClass> GetSortedTestClasses()
		{
			return GetSorted(IncludedTestClasses.Values);
		}

		[NotNull]
		protected IEnumerable<IncludedInstanceClass> GetSortedTransformerClasses()
		{
			return GetSorted(IncludedTransformerClasses.Values);
		}

		[NotNull]
		protected IEnumerable<IncludedInstanceClass> GetSortedIssueFilterClasses()
		{
			return GetSorted(IncludedFilterClasses.Values);
		}

		private static IEnumerable<IncludedInstanceClass> GetSorted(
			[NotNull] IEnumerable<IncludedInstanceClass> instanceClasses)
		{
			var result = new List<IncludedInstanceClass>(instanceClasses);

			result.Sort();

			return result;
		}
	}
}
