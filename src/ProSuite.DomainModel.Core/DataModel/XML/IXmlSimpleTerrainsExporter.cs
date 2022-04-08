using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.XML
{
	public interface IXmlSimpleTerrainsExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] DdxModel model);

	}
}
