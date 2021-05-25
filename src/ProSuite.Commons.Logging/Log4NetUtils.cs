using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	public static class Log4NetUtils
	{
		private const string _repositoryName = "ProSuite.Commons";

		private static volatile RepositoryUsage _repositoryUsage;
		private static volatile bool _privateRepositoryCreated;
		private static readonly object _syncRoot = new object();

		private enum RepositoryUsage
		{
			Undefined,
			UsePrivateRepository,
			UseDefaultRepository
		}

		public static bool AddRootAppender([NotNull] IAppender appender)
		{
			Assert.ArgumentNotNull(appender, nameof(appender));

			ILoggerRepository loggerRepository = GetRepository();
			var hierarchy = (Hierarchy) loggerRepository;

			if (hierarchy.Root.Appenders.Contains(appender))
			{
				return false; // already exists
			}

			hierarchy.Root.AddAppender(appender);

			return true; // was added
		}

		public static bool RemoveRootAppender([NotNull] IAppender appender)
		{
			Assert.ArgumentNotNull(appender, nameof(appender));

			ILoggerRepository loggerRepository = GetRepository();
			var hierarchy = (Hierarchy) loggerRepository;

			if (! hierarchy.Root.Appenders.Contains(appender))
			{
				return false; // didn't exist, not removed
			}

			hierarchy.Root.RemoveAppender(appender);
			return true; // was removed
		}

		public static IDisposable TemporaryRootAppender([CanBeNull] IAppender appender)
		{
			if (appender == null)
			{
				return null;
			}

			AddRootAppender(appender);

			return new DisposableCallback(() => RemoveRootAppender(appender));
		}

		internal static bool UsePrivateRepository
		{
			get
			{
				if (_repositoryUsage == RepositoryUsage.Undefined)
				{
					lock (_syncRoot)
					{
						if (_repositoryUsage == RepositoryUsage.Undefined)
						{
							_repositoryUsage = UseDefaultRepository()
								                   ? RepositoryUsage.UseDefaultRepository
								                   : RepositoryUsage.UsePrivateRepository;
						}
					}
				}

				return _repositoryUsage == RepositoryUsage.UsePrivateRepository;
			}
			set
			{
				lock (_syncRoot)
				{
					if (! value && _privateRepositoryCreated)
					{
						LogManager.ShutdownRepository(_repositoryName);
						// however, the repository cannot be removed
						// TODO or should we use LogManager.ResetConfiguration(_repositoryName) instead?
					}

					_repositoryUsage = value
						                   ? RepositoryUsage.UsePrivateRepository
						                   : RepositoryUsage.UseDefaultRepository;
				}
			}
		}

		internal static bool Log4NetIsConfigured()
		{
			return GetRepository().Configured;
		}

		[NotNull]
		internal static ILog GetLogger([NotNull] Type type)
		{
			if (UsePrivateRepository)
			{
				EnsurePrivateRepository();

				return LogManager.GetLogger(_repositoryName, type);
			}

			return LogManager.GetLogger(type);
		}

		internal static void Configure([NotNull] FileInfo xmlFileInfo)
		{
			XmlConfigurator.Configure(GetRepository(), xmlFileInfo);
		}

		internal static void Configure()
		{
			ILoggerRepository loggerRepository = GetRepository();

			XmlConfigurator.Configure(loggerRepository);

			if (! loggerRepository.Configured)
			{
				BasicConfigurator.Configure(loggerRepository);
			}
		}

		internal static IEnumerable<IAppender> GetAppenders()
		{
			ILoggerRepository repository = GetRepository();

			foreach (IAppender appender in repository.GetAppenders())
			{
				yield return appender;
			}
		}

		private static void EnsurePrivateRepository()
		{
			if (_privateRepositoryCreated)
			{
				return;
			}

			lock (_syncRoot)
			{
				if (! _privateRepositoryCreated)
				{
					try
					{
						LogManager.CreateRepository(_repositoryName);
					}
					catch (LogException)
					{
						// ignore
						// for reasons not yet understood, this exception (repository already defined) may occur within the winforms designer
					}

					_privateRepositoryCreated = true;
				}
			}
		}

		[NotNull]
		private static ILoggerRepository GetRepository()
		{
			return UsePrivateRepository
				       ? LogManager.GetRepository(_repositoryName)
				       : LogManager.GetRepository();
		}

		private static bool UseDefaultRepository()
		{
			string value = Environment.GetEnvironmentVariable(
				"PROSUITE_LOG4NET_USE_DEFAULT_REPOSITORY");

			return ! string.IsNullOrEmpty(value) &&
			       string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}

		public static LogType MapLogLevelToLogType(Level level)
		{
			if (level == Level.Debug)
				return LogType.Debug;

			if (level == Level.Info)
				return LogType.Info;

			if (level == Level.Warn)
				return LogType.Warn;

			if (level == Level.Error)
				return LogType.Error;

			return LogType.Other;
		}
	}
}
