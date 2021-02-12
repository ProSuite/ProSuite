using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[AttributeTest]
	public class QaGdbConstraintFactory : TestFactory
	{
		private const string _allowNullValuesForCodedValueDomains =
			"AllowNullValuesForCodedValueDomains";

		private const string _allowNullValuesForRangeDomains =
			"AllowNullValuesForRangeDomains";

		#region issue codes

		[CanBeNull] private static ITestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes =>
			_codes ?? (_codes = new AggregatedTestIssueCodes(
				           GdbConstraintUtils.Codes,
				           QaConstraint.Codes,
				           QaValue.Codes));

		#endregion

		public override string GetTestTypeDescription()
		{
			return typeof(QaConstraint).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return new List<TestParameter>
			       {
				       new TestParameter("table", typeof(ITable),
				                         DocStrings.QaGdbConstraintFactory_table),
				       new TestParameter(_allowNullValuesForCodedValueDomains,
				                         typeof(bool),
				                         description: DocStrings
					                         .QaGdbConstraintFactory_AllowNullValuesForCodedValueDomains,
				                         isConstructorParameter: false)
				       {
					       DefaultValue = true
				       },
				       new TestParameter(_allowNullValuesForRangeDomains,
				                         typeof(bool),
				                         description: DocStrings
					                         .QaGdbConstraintFactory_AllowNullValuesForRangeDomains,
				                         isConstructorParameter: false)
				       {
					       DefaultValue = true
				       }
			       }.AsReadOnly();
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaGdbConstraintFactory;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			throw new InvalidOperationException(
				"The tests are created in the method CreateTestInstances");
		}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] constructorArguments =
				base.Args(datasetContext, testParameters, out tableParameters);
			Assert.AreEqual(1, constructorArguments.Length,
			                "unexpected constructor argument count");

			var allArguments = new List<object>(3) {constructorArguments[0]};

			foreach (TestParameter parameter in testParameters)
			{
				if (parameter.IsConstructorParameter)
				{
					// constructor parameters are already in list
					continue;
				}

				object value;
				if (! TryGetArgumentValue(parameter, datasetContext, out value))
				{
					value = parameter.DefaultValue;
				}

				allArguments.Add(value);
			}

			return allArguments.ToArray();
		}

		protected override IList<ITest> CreateTestInstances(object[] args)
		{
			Assert.AreEqual(3, args.Length, "Unexpected argument count");

			var table = (ITable) args[0];
			var allowNullValuesForCodedValueDomains = (bool) args[1];
			var allowNullValuesForRangeDomains = (bool) args[2];

			var result = new List<ITest>(2);

			IList<ConstraintNode> nodes = GdbConstraintUtils.GetGdbConstraints(
				table, allowNullValuesForCodedValueDomains,
				allowNullValuesForRangeDomains);

			if (nodes.Count > 0)
			{
				// add test for subtypes/domains 
				result.Add(new QaConstraint(table, nodes,
				                            errorDescriptionVersion: 1));
			}

			IList<string> fields = GdbConstraintUtils.GetUuidFields(table);

			if (fields.Count > 0)
			{
				var validUuidTest = new QaValue(table, fields);
				result.Add(validUuidTest);
			}

			return result;
		}

		protected override void SetPropertyValue(ITest test, TestParameter testParameter,
		                                         object value)
		{
			var ignoredParameters = new[]
			                        {
				                        _allowNullValuesForCodedValueDomains,
				                        _allowNullValuesForRangeDomains
			                        };

			if (ignoredParameters.Any(
				param => string.Equals(testParameter.Name, param,
				                       StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}

			base.SetPropertyValue(test, testParameter, value);
		}
	}
}
