using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Provider interface that allows implementors to provide a specific type
	/// of specification reference, such as XML files or DDX-based specifications.
	/// </summary>
	public interface IQualitySpecificationReferencesProvider
	{
		/// <summary>
		/// The display name of the backend server, such as the host name.
		/// </summary>
		string BackendDisplayName { get; }

		/// <summary>
		/// Whether or not this provider will be able to get specifications.
		/// </summary>
		/// <returns></returns>
		bool CanGetSpecifications();

		/// <summary>
		/// Get the available quality specifications.
		/// </summary>
		/// <returns></returns>
		Task<IList<IQualitySpecificationReference>> GetQualitySpecifications();
	}
}
