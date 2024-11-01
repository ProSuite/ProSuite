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
					string.Format("expected {0}, got <null>", typeDesc ?? typeof(T).Name));
			}

			if (! (objParam is T))
			{
				throw new ArgumentException(
					string.Format("expected {0}, got {1}", typeDesc ?? typeof(T).Name,
					              objParam.GetType()));
			}

			return (T) objParam;
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

		private void AddIssueFilters([NotNull] IList<ITest> tests,
		                             [NotNull] IOpenDataset datasetContext)
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
					IIssueFilter filter =
						InstanceFactoryUtils.CreateIssueFilter(issueFilterConfiguration, datasetContext);

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
