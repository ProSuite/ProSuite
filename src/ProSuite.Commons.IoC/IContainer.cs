using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.IoC
{
	public interface IContainer : IDisposable
	{
		void Register([NotNull] string key, [NotNull] Type classType);

		void Register([NotNull] string key,
		              [NotNull] Type serviceType,
		              [NotNull] Type classType);

		[NotNull]
		T Resolve<T>();

		[NotNull]
		T Resolve<T>([NotNull] string key);

		void Release([NotNull] object instance);
	}
}
