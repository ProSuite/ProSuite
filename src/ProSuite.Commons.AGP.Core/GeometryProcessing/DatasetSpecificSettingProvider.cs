using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing;

public class DatasetSpecificSettingProvider<T> : IFlexibleSettingProvider<T> where T : struct
{
	private readonly string _optionName;
	private readonly List<DatasetSpecificValue<T>> _datasetSpecificValues;
	private readonly T _fallback;

	public DatasetSpecificSettingProvider(
		string optionName,
		T fallback,
		[CanBeNull] List<DatasetSpecificValue<T>> datasetSpecificValues = null)
	{
		_optionName = optionName;
		_datasetSpecificValues = datasetSpecificValues;
		_fallback = fallback;
	}

	public IList<DatasetSpecificValue<T>> DatasetSpecificValues => _datasetSpecificValues;

	public T GetValue(string tableName, out string notification)
	{
		notification = null;

		if (_datasetSpecificValues == null)
		{
			return _fallback;
		}

		// TODO: Consider pattern matching like in DatasetMatchCriterion

		if (tableName == null)
		{
			return _fallback;
		}

		foreach (var datasetSpecificValue in _datasetSpecificValues)
		{
			if (tableName.EndsWith(datasetSpecificValue.Dataset,
			                       StringComparison.InvariantCultureIgnoreCase))
			{
				notification =
					$"Option '{_optionName}' is overridden for dataset {tableName}: {datasetSpecificValue.Value} (override provided by central configuration).";

				return datasetSpecificValue.Value;
			}
		}

		return _fallback;
	}
}
