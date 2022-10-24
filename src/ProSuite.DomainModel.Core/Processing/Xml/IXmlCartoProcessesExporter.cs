using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public interface IXmlCartoProcessesExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] DdxModel model);
	}
}
