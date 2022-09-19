using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	public interface IXmlSimpleTerrainsImporter
	{
		void Import([NotNull] string xmlFilePath);
	}
}
