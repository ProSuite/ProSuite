using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public class DatasetTestParameterValue : TestParameterValue
	{
		private const string _typeString = "D";
		[UsedImplicitly] private Dataset _datasetValue;
		[UsedImplicitly] private string _filterExpression;
		[UsedImplicitly] private bool _usedAsReferenceData;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetTestParameterValue"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected DatasetTestParameterValue() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetTestParameterValue"/> class.
		/// </summary>
		/// <param name="testParameter">The test parameter.</param>
		/// <param name="dataset">The dataset.</param>
		/// <param name="filterExpression">The filter expression.</param>
		/// <param name="usedAsReferenceData">Indicates that this dataset is used as valid reference data for 
		/// the quality condition. if only reference datasets are loaded in a work context for a given 
		/// quality condition, the quality condition is not applied</param>
		public DatasetTestParameterValue([NotNull] TestParameter testParameter,
		                                 [CanBeNull] Dataset dataset = null,
		                                 [CanBeNull] string filterExpression = null,
		                                 bool usedAsReferenceData = false)
			: base(testParameter)
		{
			_datasetValue = dataset;
			_filterExpression = filterExpression;
			_usedAsReferenceData = usedAsReferenceData;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetTestParameterValue"/> class
		/// with the expected properties but without initializing the actual values that are
		/// allowed to be null.
		/// </summary>
		/// <param name="testParameterName">The test parameter's name.</param>
		/// <param name="dataType">Type of the test parameter.</param>
		public DatasetTestParameterValue([NotNull] string testParameterName, Type dataType)
			: base(testParameterName, dataType) { }

		#endregion

		public override string StringValue
		{
			get
			{
				string result;
				if (_datasetValue != null)
				{
					result = _datasetValue.AliasName;
				}
				else if (ValueSource != null)
				{
					result = ValueSource.Name;
				}
				else
				{
					return null;
				}

				if (! string.IsNullOrEmpty(_filterExpression))
				{
					result = result + " ; " + _filterExpression;
				}

				return result;
			}
			set
			{
				if (value == null)
				{
					value = string.Empty;
				}

				string[] tokens = value.Split(';');
				string datasetName = tokens[0].Trim();

				if (_datasetValue != null && datasetName != _datasetValue.Name)
				{
					throw new InvalidOperationException(
						"Cannot set Value of DatasetValue, except filter expression");
				}

				if (ValueSource != null && datasetName != ValueSource.Name)
				{
					throw new InvalidOperationException(
						"Cannot set Value of Transformer, except filter expression");
				}

				_filterExpression = tokens.Length > 1
					                    ? value.Substring(value.IndexOf(';') + 1).Trim()
					                    : null;
			}
		}

		/// <summary>
		/// 	Alias of DatasetTestParameter Value
		/// 	get: 
		/// 	The value equals to [_datasetValue.AliasName]; [filterExpression]
		/// 	set: 
		/// 	Only the [filterExpression] part of the alias must be edited. This will adapt the filter expression
		/// </summary>
		public string Alias
		{
			get
			{
				string result;
				if (_datasetValue != null)
				{
					result = _datasetValue.AliasName;
				}
				else if (ValueSource != null)
				{
					result = ValueSource.Name;
				}
				else
				{
					result = "<not defined>";
				}

				if (! string.IsNullOrEmpty(_filterExpression))
				{
					result = result + " ; " + _filterExpression;
				}

				return result;
			}
		}

		[CanBeNull]
		public Dataset DatasetValue
		{
			get { return _datasetValue; }
			set { _datasetValue = value; }
		}

		[CanBeNull]
		public string FilterExpression
		{
			get { return _filterExpression; }
			set { _filterExpression = value; }
		}

		public bool UsedAsReferenceData
		{
			get { return _usedAsReferenceData; }
			set { _usedAsReferenceData = value; }
		}

		/// <summary>
		/// Gets the name of the referenced dataset or the name of the referenced transformer.
		/// </summary>
		/// <returns></returns>
		public string GetName()
		{
			if (DatasetValue != null)
			{
				return DatasetValue.Name;
			}

			if (ValueSource != null)
			{
				return ValueSource.Name;
			}

			return null;
		}

		/// <summary>
		/// Gets the source dataset(s) referenced either directly by this parameter or recursively
		/// through the referenced transformer. Datasets only referenced by issue-filters are not
		/// returned.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Dataset> GetAllSourceDatasets(bool excludeReferenceDatasets = false)
		{
			if (excludeReferenceDatasets && UsedAsReferenceData)
			{
				yield break;
			}

			if (DatasetValue != null)
			{
				yield return DatasetValue;
			}
			else if (ValueSource != null)
			{
				foreach (Dataset referencedDataset in ValueSource.GetDatasetParameterValues(
					         includeSourceDatasets: true,
					         excludeReferenceDatasets: excludeReferenceDatasets))
				{
					yield return referencedDataset;
				}
			}
		}

		public override TestParameterValue Clone()
		{
			var result = new DatasetTestParameterValue(TestParameterName, DataType)
			             {
				             _datasetValue = _datasetValue,
				             _filterExpression = _filterExpression,
				             _usedAsReferenceData = _usedAsReferenceData,
				             ValueSource = ValueSource
			             };

			return result;
		}

		public override bool UpdateFrom(TestParameterValue updateValue)
		{
			var hasUpdates = false;
			var datasetUpdateValue = (DatasetTestParameterValue) updateValue;

			if (DatasetValue != datasetUpdateValue.DatasetValue)
			{
				DatasetValue = datasetUpdateValue.DatasetValue;
				hasUpdates = true;
			}

			if (ValueSource != datasetUpdateValue.ValueSource)
			{
				ValueSource = datasetUpdateValue.ValueSource;
				hasUpdates = true;
			}

			if (FilterExpression != datasetUpdateValue.FilterExpression)
			{
				FilterExpression = datasetUpdateValue.FilterExpression;
				hasUpdates = true;
			}

			if (UsedAsReferenceData != datasetUpdateValue.UsedAsReferenceData)
			{
				UsedAsReferenceData = datasetUpdateValue.UsedAsReferenceData;
				hasUpdates = true;
			}

			return hasUpdates;
		}

		public override bool Equals(TestParameterValue other)
		{
			var o = other as DatasetTestParameterValue;

			if (o == null)
			{
				return false;
			}

			bool equal = TestParameterName == o.TestParameterName &&
			             DatasetValue == o.DatasetValue &&
			             ValueSource == o.ValueSource &&
			             FilterExpression == o.FilterExpression &&
			             UsedAsReferenceData == o.UsedAsReferenceData;

			return equal;
		}

		internal override string TypeString => _typeString;
		internal static string DatasetTypeString => _typeString;
	}
}
