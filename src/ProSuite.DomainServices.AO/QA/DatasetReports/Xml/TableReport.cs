using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	[XmlRoot("TableReport")]
	public class TableReport : ObjectClassReport
	{
		[XmlArray("Fields")]
		[XmlArrayItem("Field")]
		[NotNull]
		public List<FieldDescriptor> Fields { get; } = new List<FieldDescriptor>();

		public override void AddField(FieldDescriptor fieldDescriptor)
		{
			Assert.ArgumentNotNull(fieldDescriptor, nameof(fieldDescriptor));

			Fields.Add(fieldDescriptor);
		}
	}
}
