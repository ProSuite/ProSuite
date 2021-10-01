using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public class FieldDistinctValues
	{
		private List<FieldDistinctValue> _distinctValues;

		[XmlAttribute("distinctValueCount")]
		public int DistinctValueCount { get; set; }

		[XmlAttribute("uniqueValuesCount")]
		public int UniqueValuesCount { get; set; }

		[XmlAttribute("uniqueValuesExcluded")]
		[DefaultValue(false)]
		public bool UniqueValuesExcluded { get; set; }

		[XmlAttribute("maximumReportedValueCountExceeded")]
		[DefaultValue(false)]
		public bool MaximumReportedValueCountExceeded { get; set; }

		[XmlArray("DistinctValues")]
		[XmlArrayItem("DistinctValue")]
		[CanBeNull]
		public List<FieldDistinctValue> DistinctValues => _distinctValues;

		public void SortDistinctValues()
		{
			_distinctValues?.Sort();
		}

		public void Add([NotNull] FieldDistinctValue fieldDistinctValue)
		{
			if (_distinctValues == null)
			{
				_distinctValues = new List<FieldDistinctValue>();
			}

			_distinctValues.Add(fieldDistinctValue);
		}
	}
}
