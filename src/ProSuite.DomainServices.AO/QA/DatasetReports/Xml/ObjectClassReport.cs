using System.Xml.Serialization;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public abstract class ObjectClassReport
	{
		[XmlElement("CatalogName")]
		public string CatalogName { get; set; }

		[XmlElement("Name")]
		public string Name { get; set; }

		[XmlElement("RowCount")]
		public int RowCount { get; set; }

		[XmlElement("SubtypeField")]
		[CanBeNull]
		public string SubtypeField { get; set; }

		[XmlElement("IsWorkspaceVersioned")]
		public bool IsWorkspaceVersioned { get; set; }

		[XmlElement("IsRegisteredAsVersioned")]
		public bool IsRegisteredAsVersioned { get; set; }

		[XmlElement("VersionName")]
		[CanBeNull]
		public string VersionName { get; set; }

		[XmlElement("GeodatabaseRelease")]
		public string GeodatabaseRelease { get; set; }

		[XmlElement("IsCurrentGeodatabaseRelease")]
		public bool IsCurrentGeodatabaseRelease { get; set; }

		[XmlElement("AliasName")]
		[CanBeNull]
		public string AliasName { get; set; }

		public abstract void AddField([NotNull] FieldDescriptor fieldDescriptor);

		public void AddRow([NotNull] IRow row)
		{
			RowCount++;

			AddRowCore(row);
		}

		protected virtual void AddRowCore([NotNull] IRow row) { }
	}
}
