using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Base class for instance definitions. The definitions can be instantiated in every
	/// environment in order to get the metadata.
	/// </summary>
	public abstract class TestFactoryDefinition : InstanceInfoBase
	{
		public override string[] TestCategories => InstanceUtils.GetCategories(GetType());

		public override string ToString()
		{
			return $"{GetType().Name} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}

		#region Overrides of InstanceInfoBase

		// TODO: Document intention of this property at base class level or remove it.
		public override Type InstanceType => null;

		#endregion

		protected static void AddConstructorParameters(
			[NotNull] List<TestParameter> parameters,
			[NotNull] Type qaTestType,
			int constructorIndex,
			[NotNull] IList<int> ignoreParameters)
		{
			ConstructorInfo constr = qaTestType.GetConstructors()[constructorIndex];

			IList<ParameterInfo> constrParams = constr.GetParameters();
			for (var iParam = 0; iParam < constrParams.Count; iParam++)
			{
				if (ignoreParameters.Contains(iParam))
				{
					continue;
				}

				ParameterInfo constrParam = constrParams[iParam];

				var testParameter = new TestParameter(
					constrParam.Name, constrParam.ParameterType,
					InstanceUtils.GetDescription(constrParam),
					isConstructorParameter: true);

				parameters.Add(testParameter);
			}
		}

		protected static void AddOptionalTestParameters(
			[NotNull] List<TestParameter> parameters,
			[NotNull] Type qaTestType,
			[CanBeNull] IEnumerable<string> ignoredTestParameters = null,
			[CanBeNull] IEnumerable<string> additionalProperties = null)
		{
			Dictionary<string, TestParameter> attributesByName =
				parameters.ToDictionary(parameter => parameter.Name);

			if (ignoredTestParameters != null)
			{
				foreach (string ignoreAttribute in ignoredTestParameters)
				{
					attributesByName.Add(ignoreAttribute, null);
				}
			}

			HashSet<string> additionalPropertiesSet =
				additionalProperties != null
					? new HashSet<string>(additionalProperties)
					: null;

			foreach (PropertyInfo property in qaTestType.GetProperties())
			{
				MethodInfo setMethod = property.GetSetMethod();

				if (setMethod == null || !setMethod.IsPublic)
				{
					continue;
				}

				TestParameterAttribute testParameterAttribute = null;
				if (additionalPropertiesSet == null ||
				    !additionalPropertiesSet.Contains(property.Name))
				{
					testParameterAttribute =
						ReflectionUtils.GetAttribute<TestParameterAttribute>(property);

					if (testParameterAttribute == null)
					{
						continue;
					}
				}

				if (attributesByName.ContainsKey(property.Name))
				{
					continue;
				}

				var testParameter = new TestParameter(
					property.Name, property.PropertyType,
					InstanceUtils.GetDescription(property),
					isConstructorParameter: false);

				if (testParameterAttribute != null)
				{
					testParameter.DefaultValue = testParameterAttribute.DefaultValue;
				}
				else
				{
					object defaultValue;
					if (ReflectionUtils.TryGetDefaultValue(property, out defaultValue))
					{
						testParameter.DefaultValue = defaultValue;
					}
				}

				parameters.Add(testParameter);
				attributesByName.Add(property.Name, testParameter);
			}
		}
	}
}
