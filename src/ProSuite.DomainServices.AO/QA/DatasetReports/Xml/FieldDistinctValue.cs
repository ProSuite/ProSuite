using System;
using System.Collections;
using System.Globalization;
using System.Xml.Serialization;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Xml;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class FieldDistinctValue : IComparable<FieldDistinctValue>
	{
		private readonly object _rawValue;

		[UsedImplicitly]
		public FieldDistinctValue() { }

		public FieldDistinctValue([NotNull] DistinctValue<object> distinctValue)
		{
			Assert.ArgumentNotNull(distinctValue, nameof(distinctValue));

			Value = FormatValue(distinctValue);
			Count = distinctValue.Count;

			_rawValue = distinctValue.Value;
		}

		[XmlAttribute("value")]
		public string Value { get; set; }

		[XmlAttribute("count")]
		public int Count { get; set; }

		#region Implementation of IComparable<FieldDistinctValue>

		public int CompareTo(FieldDistinctValue other)
		{
			return _rawValue != null && other._rawValue != null
				       ? Comparer.DefaultInvariant.Compare(_rawValue, other._rawValue)
				       : string.Compare(Value, other.Value, StringComparison.InvariantCulture);
		}

		#endregion

		[CanBeNull]
		private static string FormatValue([NotNull] DistinctValue<object> distinctValue)
		{
			var stringValue = distinctValue.Value as string;

			if (stringValue != null)
			{
				return XmlUtils.EscapeInvalidCharacters(stringValue);
			}

			// not a string value --> format it using the invariant culture
			return string.Format(CultureInfo.InvariantCulture, "{0}",
			                     distinctValue.Value);
		}
	}
}
