using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaGdbConstraintFactory : QaFactoryBase
	{
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

			var allArguments = new List<object>(4) { constructorArguments[0] };

			foreach (TestParameter parameter in testParameters)
			{
				if (parameter.IsConstructorParameter)
				{
					// constructor parameters are already in list
					continue;
				}

				if (! TryGetArgumentValue(parameter, datasetContext, out object value))
				{
					value = parameter.DefaultValue;
				}

				allArguments.Add(value);
			}

			return allArguments.ToArray();
		}

		protected override IList<ITest> CreateTestInstances(object[] args)
		{
			Assert.AreEqual(4, args.Length, "Unexpected argument count");

			var table = (IReadOnlyTable) args[0];
			var allowNullValuesForCodedValueDomains = (bool) args[1];
			var allowNullValuesForRangeDomains = (bool) args[2];
			var fieldsToCheck = (IList<string>) args[3];

			var result = new List<ITest>(2);

			var fieldsToCheckDict =
				fieldsToCheck != null
					? new HashSet<string>(fieldsToCheck, StringComparer.InvariantCultureIgnoreCase)
					: null;

			if (! DatasetUtils.IsRegisteredAsObjectClass(table))
			{
				_msg.Warn("QaGdbConstraintFactory is limited to OID check for unregistered tables");
			}

			IList<ConstraintNode> nodes = GdbConstraintUtils.GetGdbConstraints(
				table, allowNullValuesForCodedValueDomains,
				allowNullValuesForRangeDomains, fieldsToCheckDict);

			if (nodes.Count > 0)
			{
				// add test for subtypes/domains 
				result.Add(new QaConstraint(table, nodes,
				                            errorDescriptionVersion: 1));
			}

			IList<string> fields = GdbConstraintUtils.GetUuidFields(table);

			IList<string> checkFields = fields;
			if (fieldsToCheck != null)
			{
				checkFields = new List<string>();
				foreach (string field in fields)
				{
					if (fieldsToCheckDict.Contains(field))
					{
						checkFields.Add(field);
					}
				}
			}

			if (checkFields.Count > 0)
			{
				var validUuidTest = new QaValue(table, checkFields);
				result.Add(validUuidTest);
			}

			return result;
		}

		protected override void SetPropertyValue(object test, TestParameter testParameter,
		                                         object value)
		{
			var factoryDef = (QaGdbConstraintFactoryDefinition) FactoryDefinition;
			var ignoredParameters = new[]
			                        {
				                        factoryDef.AllowNullValuesForCodedValueDomains,
				                        factoryDef.AllowNullValuesForRangeDomains,
				                        factoryDef.FieldsParameterName
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
