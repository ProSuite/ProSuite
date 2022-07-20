using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;

namespace ProSuite.Commons.IoC
{
	public interface IDependencyResolver
	{
		/// <summary>
		/// Returns the component instance of the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidConfigurationException"></exception>
		T Resolve<T>([CanBeNull] string key = null);

		/// <summary>
		/// Disposes the provided instance if it implements <see cref="IDisposable"/> and it has
		/// a transient lifecycle.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		void Release<T>(object component);
	}
}
