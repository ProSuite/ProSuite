using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public interface IXmlAttributeDependenciesImporter
	{
		void Import([NotNull] string xmlFilePath);
	}
}
