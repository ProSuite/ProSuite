using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public interface IDataReference
	{
		// TODO provide QueryExtent method also to avoid cocreation of envelope on each access?

		[NotNull]
		IEnvelope Extent { get; }

		[NotNull]
		string DatasetName { get; }

		[NotNull]
		string GetDescription();

		[NotNull]
		string GetLongDescription();

		int Execute([NotNull] ContainerTest containerTest,
		            int occurance,
		            out bool applicable);
	}
}
