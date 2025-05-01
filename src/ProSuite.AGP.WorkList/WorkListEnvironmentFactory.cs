using System;
using System.Collections.Generic;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class WorkListEnvironmentFactory
	{
		private WorkListEnvironmentFactory() { }

		public static WorkListEnvironmentFactory Instance { get; } =
			new WorkListEnvironmentFactory();

		public static IWorkListOpener IssueWorkListOpener { get; set; }

		private readonly IDictionary<string, Func<string, WorkEnvironmentBase>>
			_factoryMethodsByExtension =
				new Dictionary<string, Func<string, WorkEnvironmentBase>>();

		private Func<WorkEnvironmentBase> _createModelBasedEnvironment;

		public Func<WorkEnvironmentBase> CreateModelBasedEnvironment => _createModelBasedEnvironment;

		public WorkEnvironmentBase CreateWorkEnvironment()
		{
			WorkEnvironmentBase environment = _createModelBasedEnvironment();
			return environment;
		}

		public WorkEnvironmentBase CreateWorkEnvironment(string path)
		{
			string extension = Path.GetExtension(path).Replace(".", string.Empty);

			Func<string, WorkEnvironmentBase> factoryMethod;
			if (! _factoryMethodsByExtension.TryGetValue(extension, out factoryMethod))
			{
				throw new InvalidOperationException(
					$"No work environment for {extension}-files has been registered yet.");
			}

			return factoryMethod(path);
		}

		public void RegisterEnvironment(
			[NotNull] Func<WorkEnvironmentBase> createEnvironment)
		{
			Assert.ArgumentNotNull(createEnvironment, nameof(createEnvironment));

			_createModelBasedEnvironment = createEnvironment;
		}

		public void RegisterEnvironment(
			[NotNull] string fileExtension,
			[NotNull] Func<string, WorkEnvironmentBase> createEnvironment)
		{
			Assert.ArgumentNotNullOrEmpty(fileExtension, nameof(fileExtension));
			Assert.ArgumentNotNull(createEnvironment, nameof(createEnvironment));

			_factoryMethodsByExtension[fileExtension] = createEnvironment;
		}
	}
}
