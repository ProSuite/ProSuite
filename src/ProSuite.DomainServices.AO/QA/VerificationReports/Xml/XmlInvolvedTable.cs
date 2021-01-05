using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlInvolvedTable
	{
		[UsedImplicitly]
		public XmlInvolvedTable() : this("<unknown>") { }

		public XmlInvolvedTable([NotNull] string tableName,
		                        [CanBeNull] string keyField = null,
		                        [CanBeNull] IEnumerable<RowReference> rowReferences = null)
		{
			Assert.ArgumentNotNull(tableName, nameof(tableName));

			TableName = tableName;
			KeyField = keyField;

			if (rowReferences != null)
			{
				Ids = GetIds(rowReferences);
			}
		}

		[NotNull]
		private static List<string> GetIds(
			[NotNull] IEnumerable<RowReference> rowReferences)
		{
			IFormatProvider formatProvider = CultureInfo.CurrentCulture;

			return rowReferences.Select(rowref => string.Format(formatProvider,
			                                                    "{0}", rowref.Key))
			                    .ToList();
		}

		[XmlAttribute("table")]
		[NotNull]
		public string TableName { get; set; }

		[XmlAttribute("keyField")]
		[CanBeNull]
		public string KeyField { get; set; }

		[XmlElement(ElementName = "Id")]
		[CanBeNull]
		public List<string> Ids { get; set; }
	}
}
