using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.IoC
{
	/// <summary>
	/// Before using them, the components must be registered with the <see cref="IoCContainer"/>. 
	/// Installers encapsulate and partition the registration logic if needed.
	/// The installer terminology is borrowed from Castle Windsor.
	/// </summary>
	public interface IComponentInstaller
	{
		void Install([NotNull] IoCContainer ioCContainer);
	}
}
