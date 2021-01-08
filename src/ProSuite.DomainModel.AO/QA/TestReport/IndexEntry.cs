using System.Xml;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	internal abstract class IndexEntry
	{
		public abstract void Render([NotNull] XmlDocument xmlDocument,
		                            [NotNull] XmlElement context);
	}
}