using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class DefaultTestConfigurator : TestConfigurator
	{
		private QualityCondition _qualityCondition;
		private string _testDescription;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override void SetQualityCondition(QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			Type type = GetType();

			_qualityCondition = qualityCondition;

			TestFactory factory = Assert.NotNull(
				TestFactoryUtils.CreateTestFactory(_qualityCondition),
				$"Cannot create test factory for condition {_qualityCondition.Name}");

			_testDescription = factory.TestDescription;

			IList<TestParameter> parameters = factory.Parameters;

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			try
			{
				foreach (TestParameterValue value in qualityCondition.ParameterValues)
				{
					TestParameter parameter = FindTestParameter(parameters,
					                                            value.TestParameterName);
					Assert.NotNull(parameter,
					               "Parameter {0} not found in {1}",
					               value.TestParameterName, qualityCondition.Name);

					MethodInfo getProp = type.GetMethod(
						ReflectionUtils.GetPropertyGetMethodName(value.TestParameterName));
					Assert.NotNull(getProp,
					               "Get Property {0} not found in {1}",
					               value.TestParameterName, ReflectionUtils.GetFullName(type));

					MethodInfo setProp = type.GetMethod(
						ReflectionUtils.GetPropertySetMethodName(value.TestParameterName));
					Assert.NotNull(setProp,
					               "Set Property {0} not found in {1}",
					               value.TestParameterName, ReflectionUtils.GetFullName(type));

					switch (parameter.ArrayDimension)
					{
						case 0:
							TestParameterValue setValue = value;
							if (! parameter.IsConstructorParameter
							    && value is DatasetTestParameterValue dsValue
							    && dsValue.DatasetValue == null
							    && type.GetProperty(parameter.Name)?.GetSetMethod() is MethodInfo
								    setMethod)
							{
								setMethod.Invoke(this, new object[] {null});
							}
							else
							{
								setProp.Invoke(this, new object[] {setValue});
							}

							break;

						case 1:
							//System.Collections.IList list =
							//    (System.Collections.IList)getProp.Invoke(this, new object[] { });
							//list.Add(value);
							setProp.Invoke(this, new object[] {value});
							break;

						default:
							throw new InvalidOperationException(
								string.Format("Cannot handle {0}-dimensional arrays",
								              parameter.ArrayDimension));
					}
				}
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		public override IList<TestParameterValue> GetTestParameterValues()
		{
			Assert.NotNull(_qualityCondition, "_qualityCondition != null");

			Type type = GetType();

			TestFactory factory = Assert.NotNull(
				TestFactoryUtils.CreateTestFactory(_qualityCondition),
				$"Cannot create test factory for condition {_qualityCondition.Name}");

			var values = new List<TestParameterValue>();

			foreach (TestParameter testParam in factory.Parameters)
			{
				MethodInfo getProp =
					type.GetMethod(ReflectionUtils.GetPropertyGetMethodName(testParam.Name));
				Assert.NotNull(getProp, "Get Property {0} not found in {1}",
				               testParam.Name, ReflectionUtils.GetFullName(type));

				MethodInfo setProp =
					type.GetMethod(ReflectionUtils.GetPropertySetMethodName(testParam.Name));
				Assert.NotNull(setProp, "Set Property {0} not found in {1}",
				               testParam.Name, ReflectionUtils.GetFullName(type));

				object o = getProp.Invoke(this, new object[] { });
				if (testParam.IsConstructorParameter)
				{
					Assert.NotNull(o, "parameter {0} not set", testParam.Name);
				}

				if (o is IEnumerable valueList)
				{
					foreach (object l in valueList)
					{
						AddValue(testParam, values, l);
					}
				}
				else
				{
					AddValue(testParam, values, o);
				}
			}

			return values;
		}

		public override string GetTestDescription()
		{
			return _testDescription;
		}

		#region Non-public methods

		[UsedImplicitly]
		protected bool TrySetContext(object o)
		{
			// called from generated subclass

			var provider = o as IQualityConditionContextAware;

			if (provider == null)
			{
				return false;
			}

			provider.DatasetProvider = DatasetProvider;
			provider.QualityCondition = QualityCondition;
			return true;
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender,
		                                                      ResolveEventArgs args)
		{
			return AssemblyResolveUtils.TryLoadAssembly(
				args.Name, Assembly.GetExecutingAssembly().CodeBase, _msg.Debug);
		}

		[CanBeNull]
		private static TestParameter FindTestParameter(
			[NotNull] IEnumerable<TestParameter> parameters,
			[NotNull] string name)
		{
			return parameters.FirstOrDefault(
				parameter => string.Equals(parameter.Name, name,
				                           StringComparison.OrdinalIgnoreCase));
		}

		private static void AddValue([NotNull] TestParameter testParameter,
		                             [NotNull] ICollection<TestParameterValue> values,
		                             object o)
		{
			TestParameterValue value;

			if (o is ParameterConfig paramConfig)
			{
				value = paramConfig.GetTestParameterValue();
			}
			else if (o is ParameterPropertyBase paramProp)
			{
				value = paramProp.GetParameterConfig().GetTestParameterValue();
			}
			else if (o is TestParameterValue parameterValue)
			{
				value = parameterValue;
			}
			else if (o is null)
			{
				value = null;
			}
			else
			{
				throw new NotImplementedException("Unhandled type " + o.GetType());
			}

			if (value == null)
			{
				value = TestParameterTypeUtils.GetEmptyParameterValue(testParameter);
			}
			else if (string.IsNullOrEmpty(value.TestParameterName))
			{
				if (value is DatasetTestParameterValue dsValue)
				{
					Dataset dataset = dsValue.DatasetValue;
					string constraint = dsValue.FilterExpression;
					bool usedAsReferenceData = dsValue.UsedAsReferenceData;

					TestParameterTypeUtils.AssertValidDataset(testParameter, dataset);
					value = new DatasetTestParameterValue(testParameter, dataset, constraint,
					                                      usedAsReferenceData);
				}
				else if (value is ScalarTestParameterValue)
				{
					value = new ScalarTestParameterValue(testParameter, value.StringValue);
				}
				else
				{
					throw new ArgumentException($"Unhandled type {value.GetType()}");
				}
			}

			values.Add(value);
		}

		#endregion
	}
}
