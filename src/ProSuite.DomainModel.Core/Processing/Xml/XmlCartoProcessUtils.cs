using System.IO;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public static class XmlCartoProcessUtils
	{
		public static XmlCartoProcessesDocument ReadFile([NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentCondition(File.Exists(xmlFilePath),
			                         "File does not exist: {0}", xmlFilePath);

			using (var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read))
			{
				var serializer = new XmlSerializer(typeof(XmlCartoProcessesDocument));

				return (XmlCartoProcessesDocument) serializer.Deserialize(stream);
			}
		}
	}
}
