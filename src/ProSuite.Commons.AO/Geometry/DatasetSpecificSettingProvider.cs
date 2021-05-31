using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AO.Geometry
{
	public class DatasetSpecificSettingProvider<T> : IFlexibleSettingProvider<T>
		where T : struct
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

		public T GetValue(IObject forObject, out string notification)
		{
			notification = null;

			if (_datasetSpecificValues == null)
			{
				return _fallback;
			}

			// TODO: Consider pattern matching like in DatasetMatchCriterion

			string className = DatasetUtils.GetName(forObject.Class);

			foreach (var datasetSpecificValue in _datasetSpecificValues)
			{
				if (className.EndsWith(datasetSpecificValue.Dataset,
				                       StringComparison.InvariantCultureIgnoreCase))
				{
					notification =
						$"Option '{_optionName}' is overridden for dataset {className}: {datasetSpecificValue.Value} (override provided by central configuration).";

					return datasetSpecificValue.Value;
				}
			}

			return _fallback;
		}
	}
}
