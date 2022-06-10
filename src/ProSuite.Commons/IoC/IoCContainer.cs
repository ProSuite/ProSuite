using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;

namespace ProSuite.Commons.IoC
{
	/// <summary>
	/// A basic 'Pure DI' container that allows registering objects with
	/// - single-instance-per-container lifecycle (<see cref="Register{T}(object, string)"/>)
	/// - transient lifecycle (creating a new instance when resolved, <see cref="Register{T}(System.Func{object})"/>
	/// Once only .net is supported, an implementation using IServiceCollection could be used.
	/// </summary>
	public class IoCContainer : IDisposable, IDependencyResolver
	{
		private readonly IDictionary<Type, Func<object>> _transientComponents =
			new Dictionary<Type, Func<object>>();

		private readonly IDictionary<Type, object> _singletonComponents =
			new Dictionary<Type, object>();

		private readonly IDictionary<string, object> _singletonComponentsByName =
			new Dictionary<string, object>();

		/// <summary>
		/// Returns the component instance of the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidConfigurationException"></exception>
		public T Resolve<T>(string key = null)
		{
			if (key != null)
			{
				if (_singletonComponentsByName.TryGetValue(key, out object namedComponent))
				{
					return (T) namedComponent;
				}
			}

			if (_transientComponents.TryGetValue(typeof(T),
			                                     out Func<object> factoryMethod))
			{
				return (T) factoryMethod();
			}

			if (_singletonComponents.TryGetValue(typeof(T), out object component))
			{
				return (T) component;
			}

			throw new InvalidConfigurationException(
				$"IoCContainer does not contain component or factory for {typeof(T)}");
		}

		/// <summary>
		/// Register a factory method that creates components with transient lifecycle.
		/// In case a transient component implements <see cref="IDisposable"/> it should be released
		/// using <see cref="Release{T}"/>. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="componentFactory"></param>
		public void Register<T>([NotNull] Func<object> componentFactory)
		{
			_transientComponents[typeof(T)] = componentFactory;
		}

		/// <summary>
		/// Register a component with singleton lifecycle (i.e. singleton per container). Hence
		/// make sure the provided component can be used from different threads!
		/// Calling <see cref="Release{T}"/> has no effect for singletons. They will be disposed
		/// when the container itself is disposed by calling <see cref="Dispose"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <param name="key"></param>
		public void Register<T>([NotNull] object component, string key = null)
		{
			if (key != null)
			{
				_singletonComponentsByName[key] = component;
			}
			else
			{
				_singletonComponents[typeof(T)] = component;
			}
		}

		/// <summary>
		/// Disposes the provided instance if it implements <see cref="IDisposable"/> and it has
		/// a transient lifecycle.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		public void Release<T>(object component)
		{
			if (_singletonComponents.ContainsKey(typeof(T)))
			{
				// Same behaviour as Castle: Singletons are not released (except in Dispose)
				return;
			}

			if (component is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		public void Install([NotNull] IComponentInstaller installer)
		{
			installer.Install(this);
		}

		/// <summary>
		/// Disposes the container with all its singleton components.
		/// </summary>
		public void Dispose()
		{
			foreach (object singleton in _singletonComponents.Values)
			{
				if (singleton is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			_singletonComponents.Clear();
			_transientComponents.Clear();
		}
	}
}
