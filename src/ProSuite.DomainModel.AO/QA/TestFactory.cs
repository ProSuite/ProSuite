using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Marker interface to identify test factories in reports.
	/// </summary>
	[PublicAPI]
	public interface ITestFactory : IInstanceInfo { }

	public abstract class TestFactory : InstanceFactory, ITestFactory
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestFactory"/> class.
		/// </summary>
		/// <remarks>required for ClassDescriptor instantiation</remarks>
		protected TestFactory() { }

		protected TestFactory([CanBeNull] QualityCondition condition)
		{
			Condition = condition;
		}

		#endregion

		[CanBeNull]
		public QualityCondition Condition { get; set; }

		[NotNull]
		public override string[] TestCategories => InstanceUtils.GetCategories(GetType());

		[NotNull]
		public IList<ITest> CreateTests([NotNull] IOpenDataset datasetContext)
		{
			IList<ITest> tests = Create(datasetContext, Parameters, CreateTestInstances);

			AddIssueFilters(tests, datasetContext);

			return tests;
		}

		public virtual string Export([NotNull] QualityCondition qualityCondition)
		{
			return null;
		}

		[CanBeNull]
		public virtual QualityCondition CreateQualityCondition(
			[NotNull] StreamReader file,
			[NotNull] IList<Dataset> datasets,
			[NotNull] IEnumerable<TestParameterValue> parameterValues)
		{
			return null;
		}

		protected static T ValidateType<T>(object objParam,
		                                   [CanBeNull] string typeDesc = null)
		{
			if (objParam == null)
			{
				throw new ArgumentException(
					string.Format("expected {0}, got <null>",
					              typeDesc ?? typeof(T).Name));
			}

			if (! (objParam is T))
			{
				throw new ArgumentException(
					string.Format("expected {0}, got {1}", typeDesc ?? typeof(T).Name,
					              objParam.GetType()));
			}

			return (T) objParam;
		}

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

				if (setMethod == null || ! setMethod.IsPublic)
				{
					continue;
				}

				TestParameterAttribute testParameterAttribute = null;
				if (additionalPropertiesSet == null ||
				    ! additionalPropertiesSet.Contains(property.Name))
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

		#region Non-public methods

		[NotNull]
		protected abstract ITest CreateTestInstance([NotNull] object[] args);

		[NotNull]
		protected virtual IList<ITest> CreateTestInstances([NotNull] object[] args)
		{
			ITest test = CreateTestInstance(args);
			return new[] { test };
		}

		private void AddIssueFilters([NotNull] IList<ITest> tests, IOpenDataset datasetContext)
		{
			if (Condition == null)
			{
				return;
			}

			foreach (ITest test in tests)
			{
				if (! (test is IFilterEditTest filterTest)) continue;

				IList<IIssueFilter> filters = new List<IIssueFilter>();

				foreach (var issueFilterConfiguration in Condition.IssueFilterConfigurations)
				{
					DefaultTestFactory factory = (DefaultTestFactory)
						TestFactoryUtils.CreateTestFactory(issueFilterConfiguration);
					Assert.NotNull(factory);
					IIssueFilter filter = factory.CreateInstance<IIssueFilter>(datasetContext);

					//TODO: should be something like this:
					//var factory = InstanceFactoryUtils.CreateIssueFilterFactory(issueFilterConfiguration);
					//Assert.NotNull(factory);
					//IIssueFilter filter = factory.Create(datasetContext, issueFilterConfiguration);

					filter.Name = issueFilterConfiguration.Name;
					filters.Add(filter);
				}

				if (filters.Count > 0)
				{
					filterTest.SetIssueFilters(Condition.IssueFilterExpression, filters);
				}
			}
		}

		[NotNull]
		protected virtual object[] Args(
			[NotNull] IOpenDataset datasetContext,
			[NotNull] IList<TestParameter> testParameters,
			[NotNull] out List<TableConstraint> tableParameters)
		{
			return GetConstructorArgs(datasetContext, testParameters,
			                          Condition?.ParameterValues, out tableParameters);
		}

		protected bool TryGetArgumentValue(
			[NotNull] TestParameter parameter,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] out object value)
		{
			return TryGetArgumentValue(parameter, Condition?.ParameterValues, datasetContext,
			                           null, out value);
		}

		[NotNull]
		protected IList<T> Create<T>([NotNull] IOpenDataset datasetContext,
		                             [NotNull] IList<TestParameter> testParameters,
		                             Func<object[], IList<T>> createFromArgs)
			where T : IInvolvesTables
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(testParameters, nameof(testParameters));

			_msg.VerboseDebug(() => $"Creating test(s) (Condition: {Condition?.Name})...");

			IList<TestParameterValue> parameterValues = Condition?.ParameterValues;

			try
			{
				List<TableConstraint> sortedTableParameters;
				object[] constructorArguments = Args(datasetContext,
				                                     testParameters,
				                                     out sortedTableParameters);

				IList<T> results = createFromArgs(constructorArguments);

				foreach (var created in results)
				{
					ApplyTableParameters(created, sortedTableParameters);
				}

				// apply non-constructor arguments
				foreach (TestParameter parameter in testParameters.Where(
					         p => ! p.IsConstructorParameter))
				{
					object value;
					if (! TryGetArgumentValue(
						    parameter, parameterValues, datasetContext,
						    out value, out List<TableConstraint> tableConstraints))
					{
						// TODO apply the defined DefaultValue?
						continue;
					}

					foreach (T instance in results)
					{
						int preInvolvedTablesCount = instance.InvolvedTables.Count;
						// remark: calling the instance property must add the datasets
						// to the involved tables when needed. 
						SetPropertyValue(instance, parameter, value);

						if (preInvolvedTablesCount < instance.InvolvedTables.Count)
						{
							SetNonConstructorConstraints(instance, preInvolvedTablesCount,
							                             tableConstraints);
						}
						else
						{
							Assert.True(
								tableConstraints?.FirstOrDefault(
									x => ! string.IsNullOrWhiteSpace(x.FilterExpression)) == null,
								"Cannot apply where constraints to not involved tables");
						}
					}
				}

				return results;
			}
			catch (Exception e)
			{
				if (Condition == null)
				{
					throw new AssertionException(
						"Unable to create test for undefined condition", e);
				}

				StringBuilder sb = InstanceFactoryUtils.GetErrorMessageWithDetails(Condition, e);

				throw new InvalidOperationException(sb.ToString(), e);
			}
		}

		#endregion

		public override string ToString()
		{
			return Condition == null
				       ? base.ToString()
				       : $"TestFactory for {Condition} with parameters: {InstanceUtils.GetTestSignature(this)}";
		}
	}
}
