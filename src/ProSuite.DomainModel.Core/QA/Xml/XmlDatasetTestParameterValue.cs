using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlDatasetTestParameterValue : XmlTestParameterValue
	{
		[XmlAttribute("where")]
		[DefaultValue(null)]
		public string WhereClause { get; set; }

		[XmlAttribute("usedAsReferenceData")]
		[DefaultValue(false)]
		public bool UsedAsReferenceData { get; set; }

		[XmlAttribute("workspace")]
		[DefaultValue(null)]
		public string WorkspaceId { get; set; }

		/// <summary>
		/// Optional attribute-role assignments for fields of the referenced dataset. Only needed
		/// where there is no DDX to carry them, i.e. in a standalone condition list over a
		/// harvested model. See <see cref="XmlDatasetFieldRole"/>.
		/// </summary>
		[XmlArray("Fields")]
		[XmlArrayItem("Field")]
		[DefaultValue(null)]
		public List<XmlDatasetFieldRole> FieldRoles { get; set; }

		public bool IsEmpty()
		{
			return WorkspaceId == null &&
			       string.IsNullOrWhiteSpace(TransformerName) &&
			       (FieldRoles == null || FieldRoles.Count == 0);
		}
	}
}
