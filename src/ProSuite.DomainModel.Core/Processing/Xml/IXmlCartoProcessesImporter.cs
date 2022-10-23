using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public interface IXmlCartoProcessesImporter
	{
		void Import([NotNull] string xmlFilePath);

		void ImportProcessTypes([NotNull] string xmlFilePath);
	}
}
