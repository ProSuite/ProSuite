using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.XML
{
	public interface IXmlLinearNetworksImporter
	{
		void Import([NotNull] string xmlFilePath);
	}
}
