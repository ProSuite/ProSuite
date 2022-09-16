using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.AttributeDependencies.Xml
{
	public interface IXmlAttributeDependenciesExporter
	{
		void Export([NotNull] string xmlFilePath, [CanBeNull] DdxModel model);

		void Export([NotNull] string xmlFilePath, [NotNull] AttributeDependency entity);
	}
}
