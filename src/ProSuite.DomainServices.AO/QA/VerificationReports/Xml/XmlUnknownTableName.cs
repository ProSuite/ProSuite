using System.Collections.Generic;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.VerificationReports.Xml
{
	public class XmlUnknownTableName
	{
		[XmlAttribute("tableName")]
		public string TableName { get; set; }

		[XmlArray("ExceptionObjects")]
		[XmlArrayItem("ExceptionObject")]
		[UsedImplicitly]
		[CanBeNull]
		public List<XmlExceptionObject> ExceptionObjects { get; set; }

		public void AddExceptionObjects([NotNull] XmlExceptionObject exceptionObject)
		{
			if (ExceptionObjects == null)
			{
				ExceptionObjects = new List<XmlExceptionObject>();
			}

			ExceptionObjects.Add(exceptionObject);
		}
	}
}
