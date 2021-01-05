using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlTableWithNonUniqueKeys
	{
		[CanBeNull] private List<string> _nonUniqueKeys;

		[XmlAttribute("name")]
		public string TableName { get; set; }

		[XmlAttribute("workspace")]
		public string Workspace { get; set; }

		[XmlArray("NonUniqueKeys")]
		[XmlArrayItem("Key")]
		[UsedImplicitly]
		[CanBeNull]
		public List<string> NonUniqueKeys => _nonUniqueKeys;

		public void AddNonUniqueKey([CanBeNull] object key)
		{
			if (_nonUniqueKeys == null)
			{
				_nonUniqueKeys = new List<string>();
			}

			_nonUniqueKeys.Add(Format(key));
		}

		private static string Format([CanBeNull] object key)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}", key);
		}
	}
}
