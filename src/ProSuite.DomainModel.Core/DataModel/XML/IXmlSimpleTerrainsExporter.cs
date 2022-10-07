using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	public interface IXmlSimpleTerrainsExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] DdxModel model);
	}
}
