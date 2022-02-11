using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public interface IFeatureProxy
	{
		[NotNull]
		IReadOnlyFeature Inner { get; }
	}
}
