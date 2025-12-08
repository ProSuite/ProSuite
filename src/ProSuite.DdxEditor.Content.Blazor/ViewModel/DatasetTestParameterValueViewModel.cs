using Microsoft.AspNetCore.Components;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Notifications;
using ProSuite.DdxEditor.Content.Blazor.View;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel;

public class DatasetTestParameterValueViewModel : ViewModelBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull] private string _filterExpression;

	private bool _usedAsReferenceData;

	private DatasetTestParameterValueViewModel(
		[NotNull] TestParameter parameter,
		[CanBeNull] object value,
		[CanBeNull] string imageSource,
		[CanBeNull] string modelName,
		[CanBeNull] string filterExpression,
		bool usedAsReferenceData,
		[NotNull] IInstanceConfigurationViewModel observer,
		bool required) :
		base(parameter, value, observer, required, "Dataset not set")
	{
		_filterExpression = filterExpression;
		_usedAsReferenceData = usedAsReferenceData;

		ImageSource = imageSource;
		ModelName = modelName;

		ComponentType = typeof(DatasetTestParameterValueBlazor);
		ComponentParameters.Add("ViewModel", this);

		Validate();
	}

	[CanBeNull]
	public Either<Dataset, TransformerConfiguration> DatasetSource =>
		(Either<Dataset, TransformerConfiguration>) Value;

	[CanBeNull]
	[UsedImplicitly]
	public string ModelName { get; set; }

	[CanBeNull]
	[UsedImplicitly]
	public string FilterExpression
	{
		get => _filterExpression;
		set => SetProperty(ref _filterExpression, value);
	}

	[UsedImplicitly]
	public bool UsedAsReferenceData
	{
		get => _usedAsReferenceData;
		set => SetProperty(ref _usedAsReferenceData, value);
	}

	[CanBeNull]
	public string ImageSource { get; private set; }

	public string DisplayValue => GetDisplayName();

	public bool? IsDataset => DatasetSource?.Match(dataset => dataset != null, _ => false);

	public void GoTo()
	{
		TransformerConfiguration match = DatasetSource?.Match(d => null, t => t);

		if (match != null)
		{
			Observer.ItemNavigation.GoToItem(match);
		}
	}

	public void OnInput(ChangeEventArgs args)
	{
		FilterExpression = args.Value?.ToString();
	}

	public void FindDatasetClicked()
	{
		DatasetFinderItem selectedItem = Observer.FindDatasetClicked(Parameter);

		if (selectedItem == null)
		{
			return;
		}

		Either<Dataset, TransformerConfiguration> source = selectedItem.Source;

		ModelName = source.Match(d => d?.Model?.Name,
		                         InstanceConfigurationUtils.GetDatasetModelName);

		ImageSource =
			source.Match(BlazorImageUtils.GetImageSource, BlazorImageUtils.GetImageSource);

		FilterExpression = null;
		UsedAsReferenceData = false;

		// triggers OnPropertyChanged and updates the entity
		Value = source;
	}

	protected override bool ValidateCore()
	{
		if (DatasetSource == null)
		{
			return false;
		}

		bool valid = DatasetSource.Match(dataset => dataset != null, newConfiguration =>
		{
			InstanceConfiguration current = Observer.GetEntity();

			var configurationNames = new NotificationCollection();

			NotificationUtils.Add(configurationNames, current.Name);

			if (! TestParameterValueUtils.CheckCircularReferencesInGraph(
				    current, newConfiguration, configurationNames))
			{
				return true;
			}

			_msg.Warn(
				$"Not allowed circular {current.GetType().Name} references: {NotificationUtils.Concatenate(configurationNames, " -> ")}. Value is reset.");

			return false;
		});

		if (valid)
		{
			return true;
		}

		// don't set on property because this
		// triggers validation again.
		Value = null;
		ModelName = null;
		ImageSource = null;
		FilterExpression = null;
		UsedAsReferenceData = false;

		return false;
	}

	[NotNull]
	public static DatasetTestParameterValueViewModel CreateInstance(
		[NotNull] TestParameter parameter,
		[NotNull] DatasetTestParameterValue datasetValue,
		[NotNull] IInstanceConfigurationViewModel observer)
	{
		Assert.ArgumentNotNull(parameter, nameof(parameter));
		Assert.ArgumentNotNull(datasetValue, nameof(datasetValue));
		Assert.ArgumentNotNull(observer, nameof(observer));

		Either<Dataset, TransformerConfiguration> source = null;

		if (datasetValue.ValueSource != null)
		{
			source = new Either<Dataset, TransformerConfiguration>(datasetValue.ValueSource);
		}
		else if (datasetValue.DatasetValue != null)
		{
			source = new Either<Dataset, TransformerConfiguration>(datasetValue.DatasetValue);
		}

		string modelName =
			source?.Match(d => d?.Model?.Name, InstanceConfigurationUtils.GetDatasetModelName);

		string imageSource =
			source?.Match(BlazorImageUtils.GetImageSource, BlazorImageUtils.GetImageSource);

		string filterExpression = datasetValue.FilterExpression;
		bool usedAsReferenceData = datasetValue.UsedAsReferenceData;

		return new DatasetTestParameterValueViewModel(parameter, source, imageSource, modelName,
		                                              filterExpression, usedAsReferenceData,
		                                              observer,
		                                              parameter.IsConstructorParameter);
	}

	public string GetDisplayName(bool qualified = true)
	{
		return DatasetSource?.Match(dataset => GetDisplayName(dataset, qualified), t => t?.Name);
	}

	private string GetDisplayName([CanBeNull] IModelElement dataset, bool qualified = true)
	{
		string name = dataset == null ? null : $"{dataset.DisplayName ?? dataset.Name}";

		if (string.IsNullOrEmpty(name))
		{
			return null;
		}

		return qualified ? $"{name} [{ModelName}]" : name;
	}

	public void FilterExpressionChanged(string expression)
	{
		FilterExpression = expression;
	}

	public override string ToString()
	{
		string value = DisplayValue == null ? "<null>" : $"{DisplayValue}";

		return $"{GetType().Name}: {value} ({ParameterName}, {DataType.Name})";
	}

	public string ShowFilterExpressionBuilder()
	{
		if (Value == null)
		{
			_msg.Warn("Please select a dataset first");
			return null;
		}

		Either<Dataset, TransformerConfiguration> parameterValue =
			Value as Either<Dataset, TransformerConfiguration>;

		Dataset dataset = null;
		TransformerConfiguration transformerConfiguration = null;
		parameterValue?.Match<object>(d => dataset = d,
		                              t => transformerConfiguration = t);

		ISqlExpressionBuilder expressionBuilder =
			Assert.NotNull(Observer.SqlExpressionBuilder, "SQL Expression builder not set");

		ITableSchemaDef layerSchema = null;
		if (dataset != null)
		{
			layerSchema = dataset as ITableSchemaDef;

			if (layerSchema == null)
			{
				// Topologies, Rasters, etc
				_msg.WarnFormat("The dataset {0} does not support queries", dataset.Name);
			}
		}
		else if (transformerConfiguration != null)
		{
			layerSchema = GetTransformedTableSchemaDef(transformerConfiguration);
		}

		if (layerSchema != null)
		{
			string result = expressionBuilder.BuildSqlExpression(layerSchema, _filterExpression);

			if (result == null)
			{
				return null;
			}

			FilterExpression = result;
		}

		return FilterExpression;
	}
}
