using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public abstract class ReportBuilderBase : IReportBuilder
	{
		private readonly IDictionary<Type, IncludedTestClass> _includedTestClasses =
			new Dictionary<Type, IncludedTestClass>();

		private readonly IDictionary<Type, IncludedTransformer> _includedTransformerClasses =
			new Dictionary<Type, IncludedTransformer>();

		private readonly List<IncludedTestFactory> _includedTestFactories =
			new List<IncludedTestFactory>();

		public bool IncludeAssemblyInfo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete tests or factories are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests or factories should be included; otherwise, <c>false</c>.</value>
		public bool IncludeObsolete { get; set; }

		protected IDictionary<Type, IncludedTestClass> IncludedTestClasses => _includedTestClasses;

		protected IDictionary<Type, IncludedTransformer> IncludedTransformerClasses => _includedTransformerClasses;

		protected List<IncludedTestFactory> IncludedTestFactories => _includedTestFactories;

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

		public void IncludeTransformer(Type testType, int constructorIndex)
		{
			var newTransformer = false;
			IncludedTransformer transformerClass;
			if (!IncludedTransformerClasses.TryGetValue(testType, out transformerClass))
			{
				transformerClass = new IncludedTransformer(testType);

				if (!IncludeObsolete && transformerClass.Obsolete)
				{
					return;
				}

				if (transformerClass.InternallyUsed)
				{
					return;
				}

				// this test class is to be added, if the constructor is not obsolete
				newTransformer = true;
			}

			if (transformerClass.Obsolete)
			{
				return;
			}

			IncludedTestConstructor testConstructor =
				transformerClass.CreateTestConstructor(constructorIndex);

			if (!IncludeObsolete && testConstructor.Obsolete)
			{
				return;
			}

			if (testConstructor.InternallyUsed)
			{
				return;
			}

			transformerClass.IncludeConstructor(testConstructor);

			if (newTransformer)
			{
				IncludedTransformerClasses.Add(testType, transformerClass);
			}

		}

		public void IncludeTest(Type testType, int constructorIndex)
		{
			var newTestClass = false;
			IncludedTestClass testClass;
			if (! IncludedTestClasses.TryGetValue(testType, out testClass))
			{
				testClass = new IncludedTestClass(testType);

				if (! IncludeObsolete && testClass.Obsolete)
				{
					return;
				}

				if (testClass.InternallyUsed)
				{
					return;
				}

				// this test class is to be added, if the constructor is not obsolete
				newTestClass = true;
			}

			if (testClass.Obsolete)
			{
				return;
			}

			IncludedTestConstructor testConstructor =
				testClass.CreateTestConstructor(constructorIndex);

			if (! IncludeObsolete && testConstructor.Obsolete)
			{
				return;
			}

			if (testConstructor.InternallyUsed)
			{
				return;
			}

			testClass.IncludeConstructor(testConstructor);

			if (newTestClass)
			{
				IncludedTestClasses.Add(testType, testClass);
			}
		}

		public abstract void AddHeaderItem(string name, string value);

		public abstract void WriteReport();

		[NotNull]
		protected IEnumerable<IncludedTestClass> GetSortedTestClasses()
		{
			var result = new List<IncludedTestClass>(IncludedTestClasses.Values);

			result.Sort();

			return result;
		}

		[NotNull]
		protected IEnumerable<IncludedTransformer> GetSortedTransformerClasses()
		{
			var result = new List<IncludedTransformer>(IncludedTransformerClasses.Values);

			result.Sort();

			return result;
		}

	}
}
