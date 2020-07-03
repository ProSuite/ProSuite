using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public interface IDetachedState
	{
		/// <summary>
		/// Reattaches any detached state that is held by the implementing instance.
		/// </summary>
		/// <param name="unitOfWork">The unit of work.</param>
		void ReattachState([NotNull] IUnitOfWork unitOfWork);
	}
}