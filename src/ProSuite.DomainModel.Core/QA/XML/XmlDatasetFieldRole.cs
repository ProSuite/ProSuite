using System.Xml.Serialization;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	/// <summary>
	/// The assignment of an attribute role to a field, as carried by a dataset test parameter
	/// value in a standalone condition list:
	/// <code>
	/// &lt;Dataset parameter="dtm" value="RAS_DTM_TILES" workspace="ws1"&gt;
	///   &lt;Fields&gt;
	///     &lt;Field role="FilePath" name="CURRENT_FILE_PATH" /&gt;
	///   &lt;/Fields&gt;
	/// &lt;/Dataset&gt;
	/// </code>
	/// </summary>
	public class XmlDatasetFieldRole
	{
		/// <summary>
		/// The name of the attribute role, e.g. "FilePath". The name is used rather than the
		/// role's numeric id, which is an implementation detail of the role registry.
		/// </summary>
		[XmlAttribute("role")]
		public string Role { get; set; }

		/// <summary>
		/// The name of the field in the geodatabase that plays the role.
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }
	}
}
