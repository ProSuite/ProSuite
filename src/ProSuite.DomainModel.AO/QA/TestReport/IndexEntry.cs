using System.Xml;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace EsriDE.ProSuite.DomainModel.QA.TestReport
{
	internal abstract class IndexEntry
	{
		public abstract void Render([NotNull] XmlDocument xmlDocument,
		                            [NotNull] XmlElement context);
	}
}