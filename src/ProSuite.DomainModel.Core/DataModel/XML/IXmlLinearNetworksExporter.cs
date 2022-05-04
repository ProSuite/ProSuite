using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel.Xml
{
	public interface IXmlLinearNetworksExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] DdxModel model);
	}
}
