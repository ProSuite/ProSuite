using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	public interface IXmlLinearNetworksImporter
	{
		void Import([NotNull] string xmlFilePath);
	}
}
