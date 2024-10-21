using System;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class ScalarTestParameterValueViewModel : ViewModelBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private bool? _parameterIsSqlExpression;

	public ScalarTestParameterValueViewModel([NotNull] TestParameter parameter,
	                                         [CanBeNull] object value,
	                                         [NotNull] IInstanceConfigurationViewModel observer,
	                                         bool required) :
		base(parameter, value, observer, required)
	{
		ComponentParameters.Add("ViewModel", this);

		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(DataType);

		switch (testParameterType)
		{
			case TestParameterType.String:
				ComponentType = typeof(StringValueBlazor);
				break;
			case TestParameterType.Integer:

				if (DataType.IsEnum)
				{
					ComponentParameters.Add("DataType", DataType);
					ComponentType = typeof(EnumTestParameterValueBlazor);
					break;
				}

				ComponentType = typeof(IntegerValueBlazor);
				break;
			case TestParameterType.Double:
				ComponentType = typeof(DoubleValueBlazor);
				break;
			// todo daro Blazor DateTime picker 
			case TestParameterType.DateTime:
			case TestParameterType.CustomScalar:
				throw new NotImplementedException($"{testParameterType} is not yet supported");
			case TestParameterType.Boolean:
				ComponentType = typeof(BooleanValueBlazor);
				break;
			default:
				throw new ArgumentOutOfRangeException($"Unkown {nameof(TestParameterType)}");
		}

		Validate();
	}

	protected override bool ValidateCore()
	{
		TestParameterType testParameterType = TestParameterTypeUtils.GetParameterType(DataType);
		switch (testParameterType)
		{
			case TestParameterType.String:
				return ! string.IsNullOrEmpty((string) Value) ||
				       ! string.IsNullOrWhiteSpace((string) Value);
			default:
				return base.ValidateCore();
		}
	}

	public bool ShowSqlExpressionBuilderButton =>
		Observer.SqlExpressionBuilder != null && ParameterIsSqlExpression;

	private bool ParameterIsSqlExpression
	{
		get
		{
			_parameterIsSqlExpression ??= ParameterHasSqlExpression();

			return _parameterIsSqlExpression.Value;
		}
	}

	public string ShowSqlExpressionBuilder()
	{
		string sqlExpressionValue = Value as string;

		// Refresh dataset parameter value, it might have been cleared in the mean while:
		Assert.True(TryGetQueriedDatasetParameter(out DatasetTestParameterValue datasetParameter),
		            "Queried dataset parameter does not exist");

		Dataset dataset = datasetParameter.DatasetValue;
		TransformerConfiguration transformerConfiguration = datasetParameter.ValueSource;

		ITableSchemaDef tableSchema = null;
		if (dataset != null)
		{
			tableSchema = dataset as ITableSchemaDef;

			if (tableSchema == null)
			{
				// Topologies, Rasters, etc
				_msg.WarnFormat("The dataset {0} does not support queries", dataset.Name);
			}
		}
		else if (transformerConfiguration != null)
		{
			tableSchema = GetTransformedTableSchemaDef(transformerConfiguration);
		}
		else
		{
			_msg.WarnFormat("Please select a dataset for parameter '{0}' first",
			                datasetParameter.TestParameterName);
			return null;
		}

		Assert.NotNull(tableSchema, "Dataset parameter is not of type table");

		return Assert.NotNull(Observer.SqlExpressionBuilder, "Expression builder not set")
		             .BuildSqlExpression(tableSchema, sqlExpressionValue);
	}

	/// <summary>
	/// Determines whether the parameter has a SQL expression by looking at a potential attribute on
	/// the TestDefinition's property with the same name as the test parameter. For an example, see
	/// QaConstraintDefinition.
	/// </summary>
	/// <returns></returns>
	private bool ParameterHasSqlExpression()
	{
		if (Parameter.Type != typeof(string))
		{
			return false;
		}

		return TryGetQueriedDatasetParameter(out DatasetTestParameterValue _);
	}

	private bool TryGetQueriedDatasetParameter(out DatasetTestParameterValue datasetParameter)
	{
		datasetParameter = null;

		InstanceConfiguration instanceConfiguration = Observer.GetEntity();
		InstanceDescriptor descriptor = instanceConfiguration.InstanceDescriptor;

		ClassDescriptor descriptorClass = descriptor?.Class;

		if (descriptorClass == null)
		{
			return false;
		}

		if (! InstanceDescriptorUtils.TryGetAlgorithmDefinitionType(
			    descriptorClass, out Type instanceType))
		{
			return false;
		}

		if (instanceType == null)
		{
			return false;
		}

		SqlExpressionAttribute sqlExpressionAttribute =
			TestParameterTypeUtils.GetSqlExpressionAttribute(instanceType, Parameter);

		string tableParameterName = sqlExpressionAttribute?.TableParameter;

		if (tableParameterName == null)
		{
			return false;
		}

		TestParameterValue tableParameterValue =
			instanceConfiguration.ParameterValues.FirstOrDefault(
				p => p.TestParameterName.Equals(tableParameterName,
				                                StringComparison.InvariantCultureIgnoreCase));

		Assert.NotNull(tableParameterValue,
		               $"No parameter found in {instanceType.Name} with name {tableParameterValue}");

		datasetParameter = tableParameterValue as DatasetTestParameterValue;

		Assert.True(datasetParameter != null,
		            $"Parameter {tableParameterName} is not a dataset parameter");
		return true;
	}
}
