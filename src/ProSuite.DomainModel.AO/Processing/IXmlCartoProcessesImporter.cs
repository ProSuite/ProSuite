using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.Processing
{
	public interface IXmlCartoProcessesImporter
	{
		void Import([NotNull] string xmlFilePath);

		void ImportProcessTypes([NotNull] string xmlFilePath);
	}
}