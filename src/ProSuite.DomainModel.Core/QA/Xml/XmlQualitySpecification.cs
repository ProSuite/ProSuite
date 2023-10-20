using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlQualitySpecification : IXmlEntityMetadata
	{
		private const int _defaultListOrder = -1;
		private int _listOrder = _defaultListOrder;
		[CanBeNull] private string _description;
		[CanBeNull] private string _notes;

		[NotNull] private readonly List<XmlQualitySpecificationElement> _elements =
			new List<XmlQualitySpecificationElement>();

		[CanBeNull]
		[XmlAttribute("name")]
		public string Name { get; set; }

		[CanBeNull]
		[XmlElement("Description")]
		[DefaultValue(null)]
		public string Description
		{
			get => string.IsNullOrEmpty(_description)
				       ? null
				       : _description;
			set => _description = value;
		}

		[CanBeNull]
		[XmlElement("Notes")]
		[DefaultValue(null)]
		public string Notes
		{
			get => string.IsNullOrEmpty(_notes)
				       ? null
				       : _notes;
			set => _notes = value;
		}

		[CanBeNull]
		[XmlAttribute("uuid")]
		public string Uuid { get; set; }

		[XmlAttribute("listOrder")]
		[DefaultValue(_defaultListOrder)]
		public int ListOrder
		{
			get => _listOrder;
			set => _listOrder = value;
		}

		[XmlAttribute("tileSize")]
		[DefaultValue(0)]
		public double TileSize { get; set; }

		[XmlAttribute("issuesSpatialReferenceId")]
		[DefaultValue(0)]
		public int IssuesSpatialReferenceId { get; set; }


		[CanBeNull]
		[XmlAttribute("url")]
		public string Url { get; set; }

		[XmlAttribute("hidden")]
		[DefaultValue(false)]
		public bool Hidden { get; set; }

		[NotNull]
		[XmlArray("Elements")]
		[XmlArrayItem("Element")]
		public List<XmlQualitySpecificationElement> Elements => _elements;

		[XmlAttribute("createdDate")]
		public string CreatedDate { get; set; }

		[XmlAttribute("createdByUser")]
		public string CreatedByUser { get; set; }

		[XmlAttribute("lastChangedDate")]
		public string LastChangedDate { get; set; }

		[XmlAttribute("lastChangedByUser")]
		public string LastChangedByUser { get; set; }
	}
}
