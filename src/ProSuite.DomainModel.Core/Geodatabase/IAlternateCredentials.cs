using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	/// <summary>
	/// Allows setting alternate username/password credentials for a connection provider.
	/// </summary>
	public interface IAlternateCredentials
	{
		void SetAlternateCredentials([NotNull] string userName,
		                             [NotNull] string password);

		void ClearAlternateCredentials();

		bool HasAlternateCredentials { get; }
	}
}
