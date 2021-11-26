using System;
using log4net;
using NUnit.Framework;

namespace ProSuite.Commons.Logging.Test
{
	[TestFixture]
	public class LoggingTest
	{
		[Test]
		public void CanConfigure()
		{
			const string fileName = "NoSuchFileHere";
			var searchDirs = Array.Empty<string>();
			const bool useDefaultConfiguration = true;

			// Must not throw because a log config file is considered optional!
			LoggingConfigurator.Configure(fileName, searchDirs, useDefaultConfiguration);

			// Since we used the default config, logging must still be configured!
			Assert.True(LoggingConfigurator.IsConfigured());
		}

		[Test]
		public void CanUsePrivateConfiguration()
		{
			LoggingConfigurator.Configure("NoSuchFileHere");

			LoggingConfigurator.UsePrivateConfiguration = true;

			// There must be at least one repo, our private one:
			var repos = LogManager.GetAllRepositories();
			Assert.NotNull(repos);
			Assert.True(repos.Length > 0);

			const string repoName = "ProSuite.Commons";

			var repo = LogManager.GetRepository(repoName);
			Assert.NotNull(repo, $"Could not find our private repo: {repoName}");

			LoggingConfigurator.UsePrivateConfiguration = false;

			// Repo cannot be removed, but should have been shut down:
			Assert.NotNull(LogManager.GetRepository(repoName));

			// While at it: GetRepository() throws if given unknown name:
			Assert.Catch(() => LogManager.GetRepository("NoSuchLogRepo"));
		}
	}
}
