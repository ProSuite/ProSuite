using Microsoft.Extensions.DependencyInjection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content;

public static class DI
{
	private static IServiceCollection _serviceCollection;

	public static void Configure([CanBeNull] IServiceCollection serviceCollection)
	{
		if (serviceCollection == null)
		{
			_serviceCollection = new ServiceCollection();
		}

		_serviceCollection = serviceCollection;
	}

	public static ServiceProvider Provider { get; private set; }

	public static ServiceProvider Build()
	{
		Provider = _serviceCollection.BuildServiceProvider();

		return Provider;
	}

	[CanBeNull]
	public static T Get<T>()
	{
		return (T) Provider.GetService(typeof(T));
	}
}
