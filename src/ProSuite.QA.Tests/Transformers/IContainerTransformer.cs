using ProSuite.Commons.AO.Geodatabase.TablesBased;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IContainerTransformer
	{
		bool IsGeneratedFrom(Involved involved, Involved source);

		bool HandlesContainer { get; }
	}
}
