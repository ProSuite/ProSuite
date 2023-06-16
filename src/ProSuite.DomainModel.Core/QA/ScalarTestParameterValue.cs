using System;
using System.Globalization;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public class ScalarTestParameterValue : TestParameterValue
	{
		private const string _format = "{0}";
		private const string _typeString = "S";

		private static readonly CultureInfo _persistedCulture = CultureInfo.InvariantCulture;

		[UsedImplicitly] private string _stringValue;

		private string _formattedStringValue;
		private CultureInfo _formattedStringValueCulture;

		#region Constructors

		/// <summary>
		/// 	Initializes a new instance of the <see cref = "ScalarTestParameterValue" /> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected ScalarTestParameterValue() { }

		/// <summary>
		/// 	Initializes a new instance of the <see cref = "ScalarTestParameterValue" /> class.
		/// </summary>
		/// <param name = "testParameter">The test parameter.</param>
		/// <param name = "value">The string parameter value in the current culture.</param>
		public ScalarTestParameterValue([NotNull] TestParameter testParameter, string value)
			: base(testParameter)
		{
			StringValue = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarTestParameterValue"/> class.
		/// </summary>
		/// <param name="testParameter">The test parameter.</param>
		/// <param name="value">The parameter value.</param>
		public ScalarTestParameterValue([NotNull] TestParameter testParameter, object value)
			: base(testParameter)
		{
			// StringValue must be set based on the CURRENT culture, not the persisted culture
			// Incorrect with german culture: StringValue = stringValue ?? GetStringValue(value);

			if (value is string stringValue)
			{
				// format using current culture
				StringValue = stringValue;
			}
			else
			{
				SetValue(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarTestParameterValue"/> class
		/// without initializing the actual value.
		/// </summary>
		/// <param name="testParameterName">The test parameter's name.</param>
		/// <param name="dataType">Type of the test parameter.</param>
		public ScalarTestParameterValue([NotNull] string testParameterName, Type dataType)
			: base(testParameterName, dataType) { }

		#endregion

		/// <summary>
		/// Gets or sets the string value.
		/// </summary>
		/// <value>
		/// The string value.
		/// </value>
		/// <remarks>Note: The string value is returned, and expected to be set, in the current culture
		/// of the thread. This property is used for data binding in the DDX.</remarks>
		public sealed override string StringValue
		{
			get
			{
				if (string.IsNullOrEmpty(_stringValue))
				{
					// DataType might not be initialized if
					// - A new (optional) parameter was added to the condition but this version of
					//   the software is not yet aware of the parameter (forward compatibility).
					// - A parameter has been removed and there is still a value for it (fails unless null)
					return string.Empty;
				}

				Assert.NotNull(DataType, "Parameter data type not defined");

				// return the string value in the current culture
				return GetFormattedStringValue(CultureInfo.CurrentCulture, DataType, true);
			}
			set { SetStringValue(value, CultureInfo.CurrentCulture); }
		}

		/// <summary>
		/// Note: public for unit tests
		/// </summary>
		public string PersistedStringValue
		{
			get { return _stringValue; }
		}

		internal static string ScalarTypeString => _typeString;

		internal override string TypeString => _typeString;

		public override TestParameterValue Clone()
		{
			var result = new ScalarTestParameterValue(TestParameterName, DataType)
			             {
				             _stringValue = _stringValue
			             };

			return result;
		}

		public override bool UpdateFrom(TestParameterValue updateValue)
		{
			var scalarUpdate = (ScalarTestParameterValue) updateValue;

			var hasUpdates = false;
			if (_stringValue != scalarUpdate._stringValue)
			{
				_stringValue = scalarUpdate._stringValue;
				_formattedStringValue = null;
				hasUpdates = true;
			}

			return hasUpdates;
		}

		public override bool Equals(TestParameterValue other)
		{
			var o = other as ScalarTestParameterValue;

			if (o == null)
			{
				return false;
			}

			bool equal = Equals(TestParameterName, o.TestParameterName) &&
			             Equals(_stringValue, o._stringValue);

			return equal;
		}

		/// <summary>
		/// Attempts to get a displayable string for the current culture. If the data type is
		/// not specified and the DataType property is not initialized and the data type cannot be
		/// inferred from the persisted string value, the <see cref="PersistedStringValue"/> is
		/// returned. 
		/// </summary>
		/// <param name="dataType">The known data type of the parameter.</param>
		/// <returns></returns>
		public string GetDisplayValue([CanBeNull] Type dataType = null)
		{
			CultureInfo culture = CultureInfo.CurrentCulture;

			if (string.IsNullOrEmpty(_stringValue))
			{
				return string.Empty;
			}

			if (dataType == null)
			{
				dataType = DataType;
			}

			if (dataType == null)
			{
				// TODO: Consider encoding the type into the PersistedString, e.g.:
				//       |double|12.345|
				//       |bool|False|
				//       |LengthUnit|
				if (! TryDetermineTypeFromPersistedValue(out dataType))
				{
					return PersistedStringValue;
				}
			}

			// TODO: Measure performance benefit of cache and consider removing altogether
			// Only cache the result if we are certain of the data type:
			bool allowResultCaching = DataType != null;

			return GetFormattedStringValue(culture, dataType, allowResultCaching);
		}

		/// <summary>
		/// Note: public for unit tests
		/// </summary>
		public void SetStringValue([CanBeNull] string value,
		                           [CanBeNull] CultureInfo cultureInfo = null)
		{
			if (cultureInfo == null)
			{
				cultureInfo = CultureInfo.CurrentCulture;
			}

			if (Equals(value, _formattedStringValue) &&
			    Equals(cultureInfo, _formattedStringValueCulture))
			{
				return;
			}

			Assert.NotNull(DataType, "Parameter data type not defined");

			// verify that the string value can be cast to the correct data type, assuming it is in 
			// the current culture.
			_formattedStringValueCulture = cultureInfo;

			object castValue;
			if (! ConversionUtils.TryParseTo(DataType, value,
			                                 _formattedStringValueCulture, out castValue))
			{
				throw new ArgumentException(
					$"Invalid parameter value for {TestParameterName}: {value}");
			}

			_formattedStringValue = value;
			_stringValue = GetStringValueInPersistedCulture(castValue);
		}

		/// <summary>
		/// Returns the value object in the specified known data type or, if not specified,
		/// in the <see cref="TestParameterValue.DataType"/>.
		/// </summary>
		/// <param name="type">The parameter's known data type.</param>
		/// <returns></returns>
		[CanBeNull]
		public object GetValue([CanBeNull] Type type = null)
		{
			if (_stringValue == null)
			{
				return null;
			}

			if (type == null)
			{
				type = Assert.NotNull(DataType, "Parameter data type not defined");
			}

			object castValue;
			ConversionUtils.ParseTo(type, _stringValue, _persistedCulture, out castValue);

			return castValue;
		}

		public void SetValue(object value)
		{
			_formattedStringValue = null;
			_stringValue = GetStringValueInPersistedCulture(value);
		}

		[NotNull]
		private static string GetStringValueInPersistedCulture(object value)
		{
			return string.Format(_persistedCulture, _format, value);
		}

		private string GetFormattedStringValue([NotNull] CultureInfo culture,
		                                       [NotNull] Type dataType,
		                                       bool allowResultCaching)
		{
			if (! allowResultCaching)
			{
				return string.Format(_formattedStringValueCulture, _format,
				                     GetValue(dataType));
			}

			if (_formattedStringValue == null ||
			    ! Equals(culture, _formattedStringValueCulture))
			{
				_formattedStringValueCulture = culture;

				_formattedStringValue = string.Format(_formattedStringValueCulture, _format,
				                                      GetValue(dataType));
			}

			return _formattedStringValue;
		}

		[ContractAnnotation("=>true, type:notnull; =>false, type:canbenull")]
		private bool TryDetermineTypeFromPersistedValue([CanBeNull] out Type type)
		{
			type = null;

			if (string.IsNullOrEmpty(PersistedStringValue))
			{
				return false;
			}

			var culture = _persistedCulture;

			if (bool.TryParse(PersistedStringValue, out bool _))
			{
				type = typeof(bool);
			}
			else if (int.TryParse(PersistedStringValue, NumberStyles.Any, culture, out int _))
			{
				type = typeof(int);
			}
			else if (double.TryParse(PersistedStringValue, NumberStyles.Any, culture, out double _))
			{
				type = typeof(double);
			}
			else if (DateTime.TryParse(PersistedStringValue, culture,
			                           DateTimeStyles.None, out DateTime _))
			{
				type = typeof(DateTime);
			}

			return type != null;
		}
	}
}
