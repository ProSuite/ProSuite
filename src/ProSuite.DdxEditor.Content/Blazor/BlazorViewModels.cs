using ProSuite.Commons.DomainModels;

namespace ProSuite.DdxEditor.Content.Blazor
{
	public class BlazorViewModels : VersionedEntityWithMetadata, IDetachedState, INamed, IAnnotated
	{
		public void ReattachState(IUnitOfWork unitOfWork)
		{
			throw new System.NotImplementedException();
		}

		public string Name { get; }
		public string Description { get; }
	}
}
