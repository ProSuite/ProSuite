using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	/// <summary>
	/// Base class for test, row-filter, issue-filter, transformer factories.
	/// </summary>
	public abstract class InstanceFactory : InstanceInfoBase
	{
		protected static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly Dictionary<string, IReadOnlyTable> _transformerCache =
			new Dictionary<string, IReadOnlyTable>();

		public static IReadOnlyTable GetOrCreateTransformedTable(
			[NotNull] TransformerConfiguration config,
			[NotNull] IOpenDataset datasetContext)
		{
			string key = config.Name; // Must stay as-is

			if (_transformerCache.TryGetValue(key, out IReadOnlyTable cached))
			{
				return cached;
			}

			TransformerFactory factory = InstanceFactoryUtils.CreateTransformerFactory(config);
			ITableTransformer transformer =
				(ITableTransformer) factory.Create(datasetContext, config);
			IReadOnlyTable result = (IReadOnlyTable) transformer.GetTransformed();

			_transformerCache[key] = result;
			return result;
		}

		public override Type InstanceType => GetType();

		[NotNull]
		protected T Create<T>([NotNull] InstanceConfiguration instanceConfiguration,
		                      [NotNull] IOpenDataset datasetContext,
		                      [NotNull] IList<TestParameter> testParameters,
		                      Func<object[], T> createFromArgs) where T : IInvolvesTables
		{
			Assert.ArgumentNotNull(instanceConfiguration, nameof(instanceConfiguration));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));
			Assert.ArgumentNotNull(testParameters, nameof(testParameters));

			try
			{
				_msg.VerboseDebug(() => $"Creating instance config {instanceConfiguration.Name}");

				IList<TestParameterValue> parameterValues = instanceConfiguration.ParameterValues;

				List<TableConstraint> sortedTableParameters;
				object[] constructorArguments =
					GetConstructorArgs(datasetContext, testParameters, parameterValues,
					                   out sortedTableParameters);

				T result = createFromArgs(constructorArguments);

				ApplyTableParameters(result, sortedTableParameters);

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

					{
						int preInvolvedTablesCount = result.InvolvedTables.Count;
						SetPropertyValue(result, parameter, value);
						SetNonConstructorConstraints(result, preInvolvedTablesCount,
						                             tableConstraints);
					}
				}

				if (result is ITableTransformer transformer)
				{
					transformer.TransformerName = instanceConfiguration.Name;
				}

				return result;
			}
			catch (Exception e)
			{
				StringBuilder sb =
					InstanceFactoryUtils.GetErrorMessageWithDetails(instanceConfiguration, e);
				throw new InvalidOperationException(sb.ToString(), e);
			}
		}

		protected static void ApplyTableParameters(
			[NotNull] IInvolvesTables instance,
			[NotNull] IList<TableConstraint> sortedTableConstraints)
		{
			int tableCount = sortedTableConstraints.Count;

			if (tableCount == 0)
			{
				// Geodatabase Topology / Geometric Network tests
			}
			else if (tableCount != instance.InvolvedTables.Count)
			{
				throw new InvalidOperationException(
					string.Format("Error in implementation of {0}:\n" +
					              " {0} instance contains {1} tables, expected are {2}",
					              instance.GetType(), instance.InvolvedTables.Count, tableCount));
			}

			if (tableCount > 0)
			{
				Assert.NotNull(instance.InvolvedTables, "involved tables is null");

				for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
				{
					TableConstraint tableConstraint = sortedTableConstraints[tableIndex];

					IReadOnlyTable table = tableConstraint.Table;

					if (! instance.InvolvedTables[tableIndex].Equals(table))
					{
						throw new InvalidOperationException(
							string.Format(
								"Error in implementation of {0}: table #{1} in instance is {2}, expected is {3}",
								instance.GetType(), tableIndex,
								instance.InvolvedTables[tableIndex].Name,
								table.Name));
					}

					if (StringUtils.IsNotEmpty(tableConstraint.FilterExpression))
					{
						instance.SetConstraint(tableIndex, tableConstraint.FilterExpression);
					}

					instance.SetSqlCaseSensitivity(tableIndex,
					                               tableConstraint.QaSqlIsCaseSensitive);

					if (instance is IFilterEditTest filterTest)
					{
						filterTest.SetRowFilters(tableIndex, tableConstraint.RowFiltersExpression,
						                         tableConstraint.RowFilters);
					}
				}
			}
		}

		protected virtual void SetPropertyValue([NotNull] object test,
		                                        [NotNull] TestParameter testParameter,
		                                        [CanBeNull] object value)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			Assert.ArgumentNotNull(testParameter, nameof(testParameter));

			Type testType = test.GetType();

			string propertyName = testParameter.Name;

			PropertyInfo propertyInfo = testType.GetProperty(propertyName);
			Assert.NotNull(propertyInfo,
			               "Property not found for type {0}: {1}",
			               testType.Name, propertyName);

			MethodInfo setMethod = propertyInfo.GetSetMethod();
			Assert.NotNull(setMethod,
			               "Set method not found for property {0} on test type {1}",
			               propertyName, testType.Name);

			setMethod.Invoke(test, new[] { value });
		}

		protected static void SetNonConstructorConstraints(
			[NotNull] IInvolvesTables test, int preInvolvedTablesCount,
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

		[NotNull]
		protected object[] GetConstructorArgs(
			[NotNull] IOpenDataset datasetContext,
			[NotNull] IList<TestParameter> testParameters,
			[CanBeNull] IList<TestParameterValue> parameterValues,
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
				if (! TryGetArgumentValue(parameter, parameterValues, datasetContext,
				                          tableParameters, out value))
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
			[CanBeNull] IList<TestParameterValue> parameterValues,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] out object value,
			[CanBeNull] out List<TableConstraint> tableConstraint)
		{
			tableConstraint = new List<TableConstraint>();

			bool success = TryGetArgumentValue(parameter, parameterValues, datasetContext,
			                                   tableConstraint, out value);

			return success;
		}

		protected bool TryGetArgumentValue(
			[NotNull] TestParameter parameter,
			[CanBeNull] IList<TestParameterValue> parameterValues,
			[NotNull] IOpenDataset datasetContext,
			[CanBeNull] ICollection<TableConstraint> tableConstraints,
			[CanBeNull] out object value)
		{
			if (parameterValues == null)
			{
				value = null;
				return false;
			}

			var valuesForParameter = new List<object>();

			var parameterValueList = new List<DatasetTestParameterValue>();

			foreach (TestParameterValue parameterValue in parameterValues)
			{
				if (! Equals(parameterValue.TestParameterName, parameter.Name))
				{
					continue;
				}

				_msg.VerboseDebug(
					() => $"Creating parameter value for {parameterValue.TestParameterName}");

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

			// if correct type, add to dataSetList
			if (tableConstraints != null &&
			    valuesForParameter.Count > 0 &&
			    valuesForParameter[0] is IReadOnlyTable)
			{
				for (int iValue = 0; iValue < valuesForParameter.Count; iValue++)
				{
					DatasetTestParameterValue datasetParameterValue = parameterValueList[iValue];

					Dataset dataset = datasetParameterValue.DatasetValue;

					var table = (IReadOnlyTable) valuesForParameter[iValue];

					DdxModel dataModel = dataset?.Model;

					bool useCaseSensitiveSql = dataModel != null &&
					                           ModelElementUtils.UseCaseSensitiveSql(
						                           table, dataModel.SqlCaseSensitivity);

					tableConstraints.Add(new TableConstraint(
						                     table, datasetParameterValue.FilterExpression,
						                     useCaseSensitiveSql));
				}
			}

			value = GetArgumentValue(parameter, valuesForParameter);

			return true;
		}

		[CanBeNull]
		private static object GetValue(
			[NotNull] TestParameterValue paramVal,
			[NotNull] TestParameter parameter,
			[NotNull] IOpenDataset datasetContext)
		{
			Assert.ArgumentNotNull(paramVal, nameof(paramVal));
			Assert.ArgumentNotNull(parameter, nameof(parameter));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			TransformerConfiguration transformerConfiguration = paramVal.ValueSource;

			if (transformerConfiguration != null)
			{
				if (! transformerConfiguration.HasCachedValue(datasetContext))
				{
					IReadOnlyTable transformedTable =
						GetOrCreateTransformedTable(transformerConfiguration, datasetContext);

					transformerConfiguration.CacheValue(transformedTable, datasetContext);
				}

				return transformerConfiguration.GetCachedValue();
			}

			if (paramVal is ScalarTestParameterValue scalarParameterValue)
			{
				if (scalarParameterValue.DataType == null)
				{
					scalarParameterValue.DataType = parameter.Type;
					_msg.VerboseDebug(() =>
						                  $"DataType of scalarParameterValue {scalarParameterValue.TestParameterName} needed to be initialized.");
				}

				return scalarParameterValue.GetValue();
			}

			if (paramVal is DatasetTestParameterValue datasetParameterValue)
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

		public static IReadOnlyTable CreateTransformedTable(
			[NotNull] TransformerConfiguration transformerConfiguration,
			[NotNull] IOpenDataset datasetContext)
		{
			try
			{
				TransformerFactory factory =
					InstanceFactoryUtils.CreateTransformerFactory(transformerConfiguration);

				if (factory == null)
				{
					throw new ArgumentException(
						$"Unable to create TransformerFactory for {transformerConfiguration}");
				}

				ITableTransformer tableTransformer =
					factory.Create(datasetContext, transformerConfiguration);

				return (IReadOnlyTable) tableTransformer.GetTransformed();
			}
			catch (Exception e)
			{
				StringBuilder sb =
					InstanceFactoryUtils.GetErrorMessageWithDetails(transformerConfiguration, e);

				throw new InvalidOperationException(sb.ToString(), e);
			}
		}

		[NotNull]
		private object GetArgumentValue([NotNull] TestParameter parameter,
		                                [NotNull] IList valueList)
		{
			int valueDimension = -1;
			if (parameter.ArrayDimension > 1)
			{
				valueDimension = 0;
				Type currentType = valueList.GetType();
				object currentValue = valueList;
				while (currentType.IsArray || (currentType.IsGenericType &&
				                               typeof(IEnumerable).IsAssignableFrom(currentType) &&
				                               currentType.GetGenericArguments().Length == 1))
				{
					currentType = typeof(object);
					foreach (object item in (IEnumerable) currentValue)
					{
						if (item != null)
						{
							currentType = item.GetType();
							currentValue = item;
							valueDimension++;
							break;
						}
					}
				}
			}

			if (parameter.ArrayDimension == 0 || valueDimension == parameter.ArrayDimension + 1)
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

			if (parameter.ArrayDimension == 1 || valueDimension == parameter.ArrayDimension)
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

		protected class TableConstraint
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ProSuite.DomainModel.AO.QA.InstanceFactory.TableConstraint"/> class.
			/// </summary>
			/// <param name="table">The table.</param>
			/// <param name="filterExpression">The filter expression.</param>
			/// <param name="qaSqlIsCaseSensitive">Indicates if SQL statements referring to this table should be treated as case-sensitive (only if evaluated by the QA sql engine)</param>
			/// <param name="rowFiltersExpression">condition of the non text based filters, formulated by AND/OR combinations of IRowFilter.Name </param>
			/// <param name="rowFilters">non text based filters</param>
			public TableConstraint([NotNull] IReadOnlyTable table,
			                       [CanBeNull] string filterExpression,
			                       bool qaSqlIsCaseSensitive,
			                       [CanBeNull] string rowFiltersExpression = null,
			                       [CanBeNull] IReadOnlyList<IRowFilter> rowFilters = null)
			{
				Assert.ArgumentNotNull(table, nameof(table));

				Table = table;
				FilterExpression = filterExpression;
				QaSqlIsCaseSensitive = qaSqlIsCaseSensitive;
				RowFiltersExpression = rowFiltersExpression;
				RowFilters = rowFilters;
			}

			[NotNull]
			public IReadOnlyTable Table { get; }

			[CanBeNull]
			public string FilterExpression { get; }

			public bool QaSqlIsCaseSensitive { get; }

			[CanBeNull]
			public string RowFiltersExpression { get; }

			[CanBeNull]
			public IReadOnlyList<IRowFilter> RowFilters { get; }
		}
	}
}
