using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DomainModel.AO.Processing
{
	public interface IXmlCartoProcessesExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] Model model);
	}
}