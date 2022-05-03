using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public abstract class ReportBuilderBase : IReportBuilder
	{
		private readonly IDictionary<Type, IncludedInstanceClass> _includedTestClasses =
			new Dictionary<Type, IncludedInstanceClass>();

		private readonly IDictionary<Type, IncludedInstanceClass> _includedTransformerClasses =
			new Dictionary<Type, IncludedInstanceClass>();

		private readonly List<IncludedTestFactory> _includedTestFactories =
			new List<IncludedTestFactory>();

		public bool IncludeAssemblyInfo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete tests or factories are
		/// included.
		/// </summary>
		/// <value><c>true</c> if obsolete tests or factories should be included; otherwise, <c>false</c>.</value>
		public bool IncludeObsolete { get; set; }

		protected IDictionary<Type, IncludedInstanceClass> IncludedTestClasses => _includedTestClasses;

		protected IDictionary<Type, IncludedInstanceClass> IncludedTransformerClasses => _includedTransformerClasses;

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
			IncludedInstanceClass transformerClass;
			if (!IncludedTransformerClasses.TryGetValue(testType, out transformerClass))
			{
				transformerClass = new IncludedInstanceClass(testType);

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

			IncludedInstanceConstructor testConstructor =
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
			IncludedInstanceClass testClass;
			if (! IncludedTestClasses.TryGetValue(testType, out testClass))
			{
				testClass = new IncludedInstanceClass(testType);

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

			IncludedInstanceConstructor testConstructor =
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
		protected IEnumerable<IncludedInstanceClass> GetSortedTestClasses()
		{
			var result = new List<IncludedInstanceClass>(IncludedTestClasses.Values);

			result.Sort();

			return result;
		}

		[NotNull]
		protected IEnumerable<IncludedInstanceClass> GetSortedTransformerClasses()
		{
			var result = new List<IncludedInstanceClass>(IncludedTransformerClasses.Values);

			result.Sort();

			return result;
		}

	}
}
