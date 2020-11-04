using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Helper to do some specific type of key conversions occasionally needed when comparing foreign keys and primary keys
	/// (int vs. single, double)
	/// </summary>
	internal class KeyValueConverter
	{
		private readonly esriFieldType _sourceKeyType;
		private readonly esriFieldType _relatedKeyType;
		private readonly Func<object, object> _convertSource;
		private readonly Func<object, object> _convertRelated;

		/// <summary>
		/// Initializes a new instance of the <see cref="KeyValueConverter"/> class.
		/// </summary>
		/// <param name="sourceKeyType">Type of the source key.</param>
		/// <param name="relatedKeyType">Type of the related key.</param>
		public KeyValueConverter(esriFieldType sourceKeyType, esriFieldType relatedKeyType)
		{
			_sourceKeyType = sourceKeyType;
			_relatedKeyType = relatedKeyType;

			if (sourceKeyType == relatedKeyType)
			{
				// types are equal 

				if (sourceKeyType == esriFieldType.esriFieldTypeString)
				{
					// both are strings: The relationship class query methods TRIM the strings,
					// i.e. they find related objects even if they have different sets of trailing/leading blanks
					// --> we need to do this here also, otherwise the dictionary lookups will fail
					//
					// fix for https://issuetracker02.eggits.net/browse/COM-117
					_convertSource = TrimTrailingBlanks;
					_convertRelated = TrimTrailingBlanks;
				}

				return;
			}

			// field types are NOT equal

			switch (sourceKeyType)
			{
				case esriFieldType.esriFieldTypeDouble:
					_convertRelated = ToDouble;
					break;

				case esriFieldType.esriFieldTypeSingle:
					_convertSource = ToDouble;
					_convertRelated = ToDouble;
					break;

				case esriFieldType.esriFieldTypeInteger:
					_convertRelated = ToInt32;
					break;

				case esriFieldType.esriFieldTypeSmallInteger:
					_convertSource = ToInt32;
					_convertRelated = ToInt32;
					break;
			}

			switch (relatedKeyType)
			{
				case esriFieldType.esriFieldTypeDouble:
					_convertSource = ToDouble;
					break;

				case esriFieldType.esriFieldTypeSingle:
					_convertSource = ToDouble;
					_convertRelated = ToDouble;
					break;

				case esriFieldType.esriFieldTypeInteger:
					_convertSource = ToInt32;
					break;

				case esriFieldType.esriFieldTypeSmallInteger:
					_convertSource = ToInt32;
					_convertRelated = ToInt32;
					break;
			}
		}

		public object GetSourceKey([CanBeNull] object sourceKey)
		{
			if (_convertSource == null || sourceKey == null || sourceKey is DBNull)
			{
				return sourceKey;
			}

			try
			{
				return _convertSource(sourceKey);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					string.Format(
						"Unable to convert source key: {0}; " +
						"source key field type: {1}; related key field type: {2}",
						sourceKey, _sourceKeyType, _relatedKeyType),
					e);
			}
		}

		public object GetRelatedKey(object relatedKey)
		{
			if (_convertRelated == null || relatedKey == null || relatedKey is DBNull)
			{
				return relatedKey;
			}

			try
			{
				return _convertRelated(relatedKey);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					string.Format(
						"Unable to convert related key: {0}; " +
						"source key field type: {1}; related key field type: {2}",
						relatedKey, _sourceKeyType, _relatedKeyType),
					e);
			}
		}

		[NotNull]
		private static object ToDouble([NotNull] object value)
		{
			return Convert.ToDouble(value);
		}

		[NotNull]
		private static object ToInt32([NotNull] object value)
		{
			return Convert.ToInt32(value);
		}

		[NotNull]
		private static object TrimTrailingBlanks([NotNull] object value)
		{
			var s = value as string;

			if (s == null)
			{
				return value;
			}

			const string blank = " ";

			return s.EndsWith(blank, StringComparison.OrdinalIgnoreCase)
				       ? s.TrimEnd()
				       : s;
		}
	}
}
