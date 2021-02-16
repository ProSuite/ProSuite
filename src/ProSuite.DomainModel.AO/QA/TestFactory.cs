using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public abstract class TestFactory : ITestImplementationInfo
	{
		private IList<TestParameter> _parameters;

		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestFactory"/> class.
		/// </summary>
		/// <remarks>required for ClassDescriptor instantiation</remarks>
		protected TestFactory() : this(null) { }

		protected TestFactory([CanBeNull] QualityCondition condition)
		{
			Condition = condition;
		}

		#endregion

		[CanBeNull]
		public QualityCondition Condition { get; set; }

		[NotNull]
		public IList<TestParameter> Parameters
		{
			get
			{
				if (_parameters == null)
				{
					_parameters = CreateParameters();
				}

				return new ReadOnlyList<TestParameter>(_parameters);
			}
		}

		[NotNull]
		public TestParameter GetParameter(string parameterName)
		{
			Assert.ArgumentNotNullOrEmpty(parameterName, nameof(parameterName));

			foreach (TestParameter parameter in Parameters)
			{
				if (string.Equals(parameterName, parameter.Name,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return parameter;
				}
			}

			throw new ArgumentException(string.Format("Unknown test parameter: {0} {1}",
			                                          parameterName, GetTestTypeDescription()));
		}

		[NotNull]
		public virtual string[] TestCategories => ReflectionUtils.GetCategories(GetType());

		[CanBeNull]
		public virtual string GetTestDescription()
		{
			return null;
		}

		[CanBeNull]
		public virtual string GetParameterDescription([NotNull] string parameterName)
		{
			// TODO: revise, case-insensitive match is ok? (parameter name search is insensitive elsewhere)
			foreach (TestParameter parameter in Parameters)
			{
				if (string.Equals(parameter.Name, parameterName,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return parameter.Description;
				}
			}

			return null;
		}

		[NotNull]
		public IList<ITest> CreateTests([NotNull] IOpenDataset datasetContext)
		{
			return CreateTests(datasetContext, Parameters);
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

		//public bool CheckTypes(out string error)
		//{
		//    error = "Not implemented";
		//    return false;
		//}

		public abstract string GetTestTypeDescription();

		[Obsolete]
		public virtual bool Validate(out string error)
		{
			throw new NotImplementedException();
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
					TestImplementationUtils.GetDescription(constrParam),
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
					TestImplementationUtils.GetDescription(property),
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
			return new[] {test};
		}

		[NotNull]
		protected abstract IList<TestParameter> CreateParameters();

		[NotNull]
		protected virtual object[] Args(
			[NotNull] IOpenDataset datasetContext,
			[NotNull] IList<TestParameter> testParameters,
			[NotNull] out List<TableConstraint> tableParameters)
		{
			return GetConstructorArgs(datasetContext, testParameters, out tableParameters);
		}

		[NotNull]
		protected object[] GetConstructorArgs(
			[NotNull] IOpenDataset datasetContext,
			[NotNull] IList<TestParameter> testParameters,
			[NotNull] out List<TableConstraint> tableParameters)
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(testParameters, nameof(testParameters));

			tableParameters = new List<TableConstraint>();

			var constructorArgs = new List<object>(testParameters.Count);

			// TODO refactor, clarify
			foreach (TestParameter parameter in testParameters.Where(
				p => p.IsConstructorParameter))
			{
				object value;
				if (! TryGetArgumentValue(parameter, datasetContext, tableParameters,
				                          out value))
				{
					// TODO test; if this can EVER occur: apply DefaultValue?
					Assert.Fail("no argument value for test parameter {0}",
					            parameter.Name);
				}

				constructorArgs.Add(value);
			}

			return constructorArgs.ToArray();
		}

		protected bool TryGetArgumentValue(
			[NotNull] TestParameter parameter,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] out object value)
		{
			return TryGetArgumentValue(parameter, datasetContext, null, out value);
		}

		protected bool TryGetArgumentValue(
			[NotNull] TestParameter parameter,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] out object value,
			[CanBeNull] out List<TableConstraint> tableConstraint)
		{
			List<TableConstraint> constraints = new List<TableConstraint>();
			bool success = TryGetArgumentValue(parameter, datasetContext, constraints, out value);
			tableConstraint = constraints;
			return success;
		}

		private bool TryGetArgumentValue(
			[NotNull] TestParameter parameter,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] ICollection<TableConstraint> tableConstraints,
			[CanBeNull] out object value)
		{
			if (Condition == null)
			{
				value = null;
				return false;
			}

			var valuesForParameter = new List<object>();

			var parameterValueList = new List<DatasetTestParameterValue>();

			foreach (TestParameterValue parameterValue in Condition.ParameterValues)
			{
				if (! Equals(parameterValue.TestParameterName, parameter.Name))
				{
					continue;
				}

				object valueForParameter = GetValue(parameterValue, parameter, datasetContext);

				valuesForParameter.Add(valueForParameter);

				// add value to list anyway, 
				// correct type is checked at the end of parameterIndex Loop
				parameterValueList.Add(parameterValue as DatasetTestParameterValue);
			}

			if (valuesForParameter.Count == 0 && ! parameter.IsConstructorParameter)
			{
				value = null;
				return false;
			}

			value = GetArgumentValue(parameter, valuesForParameter);

			// if correct type, add to dataSetList
			if (tableConstraints != null &&
			    valuesForParameter.Count > 0 &&
			    valuesForParameter[0] is ITable)
			{
				foreach (DatasetTestParameterValue datasetParameterValue in parameterValueList)
				{
					Dataset dataset =
						Assert.NotNull(datasetParameterValue.DatasetValue, "dataset is null");

					var table = (ITable) datasetContext.OpenDataset(
						dataset, Assert.NotNull(datasetParameterValue.DataType));

					Assert.NotNull(table, "Dataset not found in current context: {0}",
					               dataset.Name);

					DdxModel dataModel = dataset.Model;

					bool useCaseSensitiveSql = dataModel != null &&
					                           ModelElementUtils.UseCaseSensitiveSql(
						                           table, dataModel.SqlCaseSensitivity);

					tableConstraints.Add(new TableConstraint(
						                     table, datasetParameterValue.FilterExpression,
						                     useCaseSensitiveSql));
				}
			}

			return true;
		}

		[NotNull]
		private object GetArgumentValue([NotNull] TestParameter parameter,
		                                [NotNull] IList valueList)
		{
			if (parameter.ArrayDimension == 0)
			{
				if (valueList.Count != 1)
				{
					throw new ArgumentException(
						string.Format("expected 1 value for {0}, got {1} ({2})",
						              parameter.Name, valueList.Count,
						              GetTestTypeDescription()));
				}

				return valueList[0];
			}

			if (parameter.ArrayDimension == 1)
			{
				int arrayDimension = parameter.ArrayDimension;
				Type paramType = parameter.Type;

				for (var i = 0; i < arrayDimension; i++)
				{
					paramType = paramType.MakeArrayType();
				}

				int valueCount = valueList.Count;
				var list = (IList) Activator.CreateInstance(paramType, valueCount);

				for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
				{
					list[valueIndex] = valueList[valueIndex];
				}

				return list;
			}

			throw new InvalidOperationException(
				"Cannot handle multi dimensional parameter array");
		}

		[CanBeNull]
		private static object GetValue([NotNull] TestParameterValue paramVal,
		                               [NotNull] TestParameter parameter,
		                               [NotNull] IOpenDataset datasetContext)
		{
			Assert.ArgumentNotNull(paramVal, nameof(paramVal));
			Assert.ArgumentNotNull(parameter, nameof(parameter));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			var scalarParameterValue = paramVal as ScalarTestParameterValue;
			if (scalarParameterValue != null)
			{
				if (scalarParameterValue.DataType == null)
				{
					scalarParameterValue.DataType = parameter.Type;
					_msg.VerboseDebugFormat(
						"DataType of scalarParameterValue {0} needed to be initialized.",
						scalarParameterValue.TestParameterName);
				}

				return scalarParameterValue.GetValue();
			}

			var datasetParameterValue = paramVal as DatasetTestParameterValue;
			if (datasetParameterValue != null)
			{
				if (datasetParameterValue.DatasetValue == null &&
				    ! parameter.IsConstructorParameter)
				{
					return null;
				}

				Dataset dataset =
					Assert.NotNull(datasetParameterValue.DatasetValue, "dataset is null");

				datasetParameterValue.DataType = parameter.Type;

				object result = datasetContext.OpenDataset(dataset, datasetParameterValue.DataType);

				Assert.NotNull(result, "Dataset not found in current context: {0}",
				               dataset.Name);

				return result;
			}

			throw new ArgumentException($"Unhandled type {paramVal.GetType()}");
		}

		[NotNull]
		private IList<ITest> CreateTests([NotNull] IOpenDataset datasetContext,
		                                 [NotNull] IList<TestParameter> testParameters)
		{
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(testParameters, nameof(testParameters));

			try
			{
				List<TableConstraint> sortedTableParameters;
				object[] constructorArguments = Args(datasetContext,
				                                     testParameters,
				                                     out sortedTableParameters);

				IList<ITest> tests = CreateTestInstances(constructorArguments);

				foreach (ITest test in tests)
				{
					ApplyTableParameters(test, sortedTableParameters);
				}

				// apply non-constructor arguments
				foreach (TestParameter parameter in testParameters.Where(
					p => ! p.IsConstructorParameter))
				{
					object value;
					if (! TryGetArgumentValue(parameter, datasetContext, out value,
					                          out List<TableConstraint> tableConstraints))
					{
						// TODO apply the defined DefaultValue?
						continue;
					}

					foreach (ITest test in tests)
					{
						int preInvolvedTablesCount = test.InvolvedTables.Count;
						SetPropertyValue(test, parameter, value);
						SetNonConstructorConstraints(test, preInvolvedTablesCount,
						                             tableConstraints);
					}
				}

				return tests;
			}
			catch (Exception e)
			{
				QualityCondition condition = Condition;
				if (condition == null)
				{
					throw new AssertionException(
						"Unable to create test for undefined condition", e);
				}

				var sb = new StringBuilder();

				sb.AppendFormat("Unable to create test(s) for quality condition {0}",
				                condition.Name);
				sb.AppendLine();
				sb.AppendLine("with parameters:");

				foreach (TestParameterValue value in condition.ParameterValues)
				{
					string stringValue;
					try
					{
						stringValue = value.StringValue;
					}
					catch (Exception e1)
					{
						_msg.Debug(
							string.Format(
								"Error getting string value for parameter {0} of condition {1}",
								value.TestParameterName,
								condition.Name),
							e1);

						stringValue = $"<error: {e1.Message} (see log for details)>";
					}

					sb.AppendFormat("  {0} : {1}", value.TestParameterName, stringValue);
					sb.AppendLine();
				}

				sb.AppendFormat("error message: {0}",
				                ExceptionUtils.GetInnermostMessage(e));
				sb.AppendLine();

				throw new InvalidOperationException(sb.ToString(), e);
			}
		}

		protected virtual void SetPropertyValue([NotNull] ITest test,
		                                        [NotNull] TestParameter testParameter,
		                                        [CanBeNull] object value)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			Type testType = test.GetType();

			string propertyName = testParameter.Name;

			PropertyInfo propertyInfo = testType.GetProperty(propertyName);
			Assert.NotNull(propertyInfo,
			               "Property not found for test type {0}: {1}",
			               testType.Name, propertyName);

			MethodInfo setMethod = propertyInfo.GetSetMethod();
			Assert.NotNull(setMethod,
			               "Set method not found for property {0} on test type {1}",
			               propertyName, testType.Name);

			setMethod.Invoke(test, new[] {value});
		}

		private void SetNonConstructorConstraints(
			[NotNull] ITest test, int preInvolvedTablesCount,
			[CanBeNull] IList<TableConstraint> tableConstraints)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			if (! (tableConstraints?.Count > 0))
			{
				return;
			}

			int idx = preInvolvedTablesCount;

			foreach (TableConstraint constraint in tableConstraints)
			{
				test.SetConstraint(idx, constraint.FilterExpression);
				test.SetSqlCaseSensitivity(idx, constraint.QaSqlIsCaseSensitive);
				idx++;
			}

			Assert.AreEqual(test.InvolvedTables.Count, idx,
			                $"Expected {idx} involved Tables, got {test.InvolvedTables.Count}");
		}

		private static void ApplyTableParameters(
			[NotNull] ITest test,
			[NotNull] IList<TableConstraint> sortedTableParameters)
		{
			int tableCount = sortedTableParameters.Count;

			if (tableCount == 0)
			{
				// Geodatabase Topology / Geometric Network tests
			}
			else if (tableCount != test.InvolvedTables.Count)
			{
				throw new InvalidOperationException(
					string.Format("Error in implementation of {0}:\n" +
					              " {0} instance contains {1} tables, expected are {2}",
					              test.GetType(), test.InvolvedTables.Count, tableCount));
			}

			if (tableCount > 0)
			{
				Assert.NotNull(test.InvolvedTables, "involved tables is null");

				for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
				{
					TableConstraint value = sortedTableParameters[tableIndex];
					ITable table = value.Table;

					if (table != test.InvolvedTables[tableIndex])
					{
						throw new InvalidOperationException(
							string.Format(
								"Error in implementation of {0}: table #{1} in instance is {2}, expected is {3}",
								test.GetType(), tableIndex,
								((IDataset) test.InvolvedTables[tableIndex]).Name,
								((IDataset) table).Name));
					}

					if (StringUtils.IsNotEmpty(value.FilterExpression))
					{
						test.SetConstraint(tableIndex, value.FilterExpression);
					}

					test.SetSqlCaseSensitivity(tableIndex, value.QaSqlIsCaseSensitive);
				}
			}
		}

		#endregion

		#region Nested types

		protected class TableConstraint
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="TableConstraint"/> class.
			/// </summary>
			/// <param name="table">The table.</param>
			/// <param name="filterExpression">The filter expression.</param>
			/// <param name="qaSqlIsCaseSensitive">Indicates if SQL statements referring to this table should be treated as case-sensitive (only if evaluated by the QA sql engine)</param>
			public TableConstraint([NotNull] ITable table,
			                       [CanBeNull] string filterExpression,
			                       bool qaSqlIsCaseSensitive)
			{
				Assert.ArgumentNotNull(table, nameof(table));

				Table = table;
				FilterExpression = filterExpression;
				QaSqlIsCaseSensitive = qaSqlIsCaseSensitive;
			}

			[NotNull]
			public ITable Table { get; }

			[CanBeNull]
			public string FilterExpression { get; }

			public bool QaSqlIsCaseSensitive { get; }
		}

		#endregion
	}
}
