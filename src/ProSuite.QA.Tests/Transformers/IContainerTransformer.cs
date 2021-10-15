using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IContainerTransformer
	{
		bool IsGeneratedFrom(Involved involved, Involved source);

		bool HandlesContainer { get; }
	}
}
