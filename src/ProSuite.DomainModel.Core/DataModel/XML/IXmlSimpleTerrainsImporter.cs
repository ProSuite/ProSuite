using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.XML
{
	public interface IXmlSimpleTerrainsImporter
	{
		void Import([NotNull] string xmlFilePath);
	}
}
