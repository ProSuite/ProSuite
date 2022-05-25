using System;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor;

public class TestParameterViewModel : Observable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly TestParameterValue _paramValue;
	private readonly DatasetTestParameterValue _datasetTestParameterValue;

	public TestParameterViewModel(TestParameterValue paramValue)
	{
		Assert.ArgumentNotNull(paramValue, nameof(paramValue));

		_paramValue = paramValue;

		if (_paramValue is DatasetTestParameterValue datasetParameterValue)
		{
			_datasetTestParameterValue = datasetParameterValue;

			IsDataset = true;
			UsedAsReferenceData = datasetParameterValue.UsedAsReferenceData;

			Dataset dataset = datasetParameterValue.DatasetValue;

			if (dataset != null)
			{
				ImageSource = BlazorImageUtils.GetImageSource(dataset);

				Value = dataset;
				DisplayName = dataset.ToString();
				ModelName = dataset.Model?.Name;

				return;
			}
		}

		if (_paramValue is ScalarTestParameterValue scalarParameterValue)
		{
			IsDataset = false;
		}
	}

	public string DisplayName { get; }
	
	public string ParameterName => _paramValue.TestParameterName;

	public string ImageSource
	{
		get;
	}

	public Dataset Value { get; }
	
	public string ModelName { get; }

	public string FilterExpression
	{
		get => _datasetTestParameterValue?.FilterExpression;
		set
		{
			if (NotifyChanges(_datasetTestParameterValue.FilterExpression, value))
			{
				_datasetTestParameterValue.FilterExpression = value;
			}
		}
	}

	public bool IsDataset { get; }

	public bool UsedAsReferenceData { get; set; }
}
