using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Properties;
using ProSuite.Commons.Com;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
#if Server
using ESRI.ArcGIS.DatasourcesGDB;

#else
using ESRI.ArcGIS.DataSourcesGDB;
#endif

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class WorkspaceUtils
	{
		private const string _defaultRepositoryName = "SDE";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Determines whether the specified path is a shapefile workspace, i.e. it contains
		/// at least a shp or dbf file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool ShapefileWorkspaceExists([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			if (! Directory.Exists(path))
			{
				return false;
			}

			IWorkspaceFactory shapefileWorkspaceFactory = GetShapefileWorkspaceFactory();

			return shapefileWorkspaceFactory.IsWorkspace(path);
		}

		/// <summary>
		/// Determines whether the specified path is a file GDB workspace.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool FileGdbWorkspaceExists([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			if (! path.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				path = string.Concat(path, ".gdb");
			}

			IWorkspaceFactory fileGdbWorkspaceFactory = GetFileGdbWorkspaceFactory();

			return fileGdbWorkspaceFactory.IsWorkspace(path);
		}

		/// <summary>
		/// Creates a shapefile workspace in a given directory, under a given name,
		/// and returns the workspace name for it.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspaceName CreateShapefileWorkspace([NotNull] string directory,
		                                                      [NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			if (! Directory.Exists(directory))
			{
				throw new DirectoryNotFoundException($"Directory not found: {directory}");
			}

			// Instantiate an shapefile workspace factory and create a new shapefile workspace.
			// The Create method returns a workspace name object.
			IWorkspaceFactory workspaceFactory = GetShapefileWorkspaceFactory();

			return workspaceFactory.Create(directory, name, null, 0);
		}

		/// <summary>
		/// Creates a file geodatabase in a given directory, under a given name,
		/// and returns the workspace name for it.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspaceName CreateFileGdbWorkspace([NotNull] string directory,
		                                                    [NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			if (! Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Instantiate an FileGdb workspace factory and create a new file geodatabase.
			// The Create method returns a workspace name object.
			IWorkspaceFactory workspaceFactory = GetFileGdbWorkspaceFactory();

			IWorkspaceName result;

			try
			{
				result = workspaceFactory.Create(directory, name, null, 0);
			}
			catch (COMException e)
			{
				if (e.ErrorCode == -2147220902)
				{
					throw new IOException(
						$"File Geodatabase {name} already exists in {directory}.");
				}

				throw;
			}

			return result;
		}

		[NotNull]
		public static IWorkspaceName CreateInMemoryWorkspace([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();

			// Create a new in-memory workspace. This returns a name object.
			return workspaceFactory.Create(null, name, null, 0);
		}

		/// <summary>
		/// Returns a scratch File Geodatabase workspace.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace CreateScratchWorkspace()
		{
			IScratchWorkspaceFactory2 scratchWsFactory =
				new FileGDBScratchWorkspaceFactoryClass();

			return scratchWsFactory.CreateNewScratchWorkspace();
		}

		[NotNull]
		public static IWorkspace OpenOleDbWorkspace(
			[NotNull] string connectionString, int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			IWorkspaceFactory factory = GetOleDbWorkspaceFactory();

			const string requiredPrefix = "CONNECTSTRING=";

			string workspaceConnectionString =
				connectionString.StartsWith(requiredPrefix,
				                            StringComparison.OrdinalIgnoreCase)
					? connectionString
					: string.Format("{0}{1}", requiredPrefix, connectionString);

			try
			{
				return ((IWorkspaceFactory2) factory).OpenFromString(
					workspaceConnectionString,
					hWnd);
			}
			catch (COMException exception)
			{
				if (exception.ErrorCode == (decimal) fdoError.FDO_E_USER_INVALID)
				{
					throw new AuthenticationException(
						LocalizableStrings
							.WorkspaceUtils_Exception_InvalidUserNameOrPassword,
						exception);
				}

				if (exception.ErrorCode == (decimal) fdoError.FDO_E_CONNECTION_CANCELLED)
				{
					string oleDbConnectString = workspaceConnectionString.Replace(
						requiredPrefix,
						string.Empty);
					((IOleDBConnectionInfo) factory).ClearParameters(oleDbConnectString);
				}

				throw;
			}
		}

		[NotNull]
		public static IWorkspace OpenOleDbWorkspaceFromFile(
			[NotNull] string odcFilePath, int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(odcFilePath, nameof(odcFilePath));

			IWorkspaceFactory factory = GetOleDbWorkspaceFactory();

			try
			{
				return factory.OpenFromFile(odcFilePath, hWnd);
			}
			catch (COMException exception)
			{
				if (exception.ErrorCode == (decimal) fdoError.FDO_E_USER_INVALID)
				{
					throw new AuthenticationException(
						LocalizableStrings
							.WorkspaceUtils_Exception_InvalidUserNameOrPassword,
						exception);
				}

				if (exception.ErrorCode == (decimal) fdoError.FDO_E_CONNECTION_CANCELLED)
				{
					// clear the parameters for this instance (to allow re-authentication on next try)
					IPropertySet props =
						factory.ReadConnectionPropertiesFromFile(odcFilePath);

					IDictionary<string, object> dictionary =
						PropertySetUtils.GetDictionary(props);

					object connectString;
					if (dictionary.TryGetValue("CONNECTSTRING", out connectString))
					{
						((IOleDBConnectionInfo) factory).ClearParameters(
							(string) connectString);
					}
				}

				throw;
			}
		}

		[NotNull]
		public static IWorkspace OpenFileGdbWorkspace([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			if (! path.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				path = string.Concat(path, ".gdb");
			}

			string connectionString = $"DATABASE={path}";

			return OpenFileGdbWorkspaceFromString(connectionString);
		}

		[NotNull]
		public static IWorkspace OpenFileGdbWorkspaceFromString(
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			var workspaceFactory = (IWorkspaceFactory2) GetFileGdbWorkspaceFactory();

			_msg.VerboseDebug(
				() =>
					$"Opening file geodatabase workspace using connection string {connectionString}");

			return OpenWorkspace(workspaceFactory, connectionString);
		}

		[NotNull]
		public static IFeatureWorkspace OpenFileGdbFeatureWorkspace([NotNull] string path)
		{
			return (IFeatureWorkspace) OpenFileGdbWorkspace(path);
		}

		/// <summary>
		/// Creates a personal geodatabase in a given directory, under a given name,
		/// and returns the feature workspace for it.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		[NotNull]
		public static IFeatureWorkspace CreatePgdbWorkspace([NotNull] string directory,
		                                                    [NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			if (! Directory.Exists(directory))
			{
				throw new DirectoryNotFoundException($"Directory not found: {directory}");
			}

			// Instantiate an Access workspace factory and create a new personal geodatabase.
			// The Create method returns a workspace name object.
			IWorkspaceFactory workspaceFactory = GetAccessWorkspaceFactory();
			IWorkspaceName workspaceName =
				workspaceFactory.Create(directory, name, null, 0);

			// Cast the workspace name object to the IName interface and open the workspace.
			return (IFeatureWorkspace) OpenWorkspace(workspaceName);
		}

		/// <summary>
		/// Opens a personal geodatabase workspace.
		/// </summary>
		/// <param name="path">The path to the mdb file.</param>
		/// <remarks>Relative paths won't be treated as equivalent to a workspace that was previously 
		/// opened with an absolute path. A new workspace instance for the same mdb file will be 
		/// returned in this case.</remarks>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenPgdbWorkspace([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			string connectionString = string.Format("DATABASE={0}", path);
			return OpenPgdbWorkspaceFromString(connectionString);
		}

		[NotNull]
		public static IWorkspace OpenPgdbWorkspaceFromString(
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			var factory = (IWorkspaceFactory2) GetAccessWorkspaceFactory();

			_msg.VerboseDebug(
				() =>
					$"Opening personal geodatabase workspace using connection string {connectionString}");

			return OpenWorkspace(factory, connectionString);
		}

		[NotNull]
		public static IFeatureWorkspace OpenPgdbFeatureWorkspace([NotNull] string path)
		{
			return (IFeatureWorkspace) OpenPgdbWorkspace(path);
		}

		/// <summary>
		/// Opens a mobile geodatabase workspace.
		/// </summary>
		/// <param name="path">The path to the mobile geodatabase file (*.geodatabase).</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenMobileGdbWorkspace([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			string connectionString = string.Format("DATABASE={0}", path);
			return OpenMobileGdbWorkspaceFromString(connectionString);
		}

		[NotNull]
		public static IWorkspace OpenMobileGdbWorkspaceFromString(
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			var factory = (IWorkspaceFactory2) GetSqliteWorkspaceFactory();

			_msg.VerboseDebug(
				() =>
					$"Opening mobile geodatabase workspace using connection string {connectionString}");

			return OpenWorkspace(factory, connectionString);
		}

		[NotNull]
		public static IFeatureWorkspace OpenMobileGdbFeatureWorkspace([NotNull] string path)
		{
			return (IFeatureWorkspace) OpenMobileGdbWorkspace(path);
		}

		[NotNull]
		public static IWorkspace OpenSDEWorkspace([NotNull] string repositoryName,
		                                          DirectConnectDriver driver,
		                                          [NotNull] string databaseServerName,
		                                          [NotNull] string user,
		                                          [NotNull] string password,
		                                          [CanBeNull] string versionName = null,
		                                          int hWnd = 0)
		{
			Assert.ArgumentNotNull(repositoryName, nameof(repositoryName));
			Assert.ArgumentNotNullOrEmpty(databaseServerName, nameof(databaseServerName));
			Assert.ArgumentNotNullOrEmpty(user, nameof(user));
			Assert.ArgumentNotNullOrEmpty(password, nameof(password));

			IWorkspaceFactory factory = GetSdeWorkspaceFactory();

			IPropertySet props = new PropertySetClass();

			// direct connect, user/password
			switch (driver)
			{
				case DirectConnectDriver.Oracle:
				case DirectConnectDriver.Oracle9i:
				case DirectConnectDriver.Oracle10g:
				case DirectConnectDriver.Oracle11g:
					props.SetProperty("INSTANCE",
					                  GetSdeInstanceOra(driver, databaseServerName,
					                                    repositoryName));
					props.SetProperty("PASSWORD", password);
					if (StringUtils.IsNullOrEmptyOrBlank(versionName))
					{
						versionName = GetDefaultVersionName(repositoryName);
					}

					// GetPasswordStringOra(databaseServerName, password));
					break;

				case DirectConnectDriver.SqlServer:
					props.SetProperty("INSTANCE",
					                  GetSdeInstanceSqlServer(
						                  driver, databaseServerName));
					props.SetProperty("DATABASE", repositoryName);
					props.SetProperty("PASSWORD", password);
					break;

				case DirectConnectDriver.PostgreSQL:
					props.SetProperty("INSTANCE",
					                  GetSdeInstancePostgreSQL(
						                  driver, databaseServerName));
					props.SetProperty("DATABASE", repositoryName);
					props.SetProperty("PASSWORD", password);
					if (StringUtils.IsNullOrEmptyOrBlank(versionName))
					{
						versionName = GetDefaultVersionName("SDE");
					}

					break;
			}

			props.SetProperty("SERVER", string.Empty);
			props.SetProperty("USER", user);

			if (StringUtils.IsNullOrEmptyOrBlank(versionName))
			{
				versionName = GetDefaultVersionName(factory,
				                                    PropertySetUtils.Clone(props),
				                                    hWnd);
			}

			if (versionName != null)
			{
				props.SetProperty("VERSION", versionName);
			}

			return OpenWorkspace(factory, props, hWnd);
		}

		[NotNull]
		public static IWorkspace OpenSDEWorkspace(DirectConnectDriver driver,
		                                          [NotNull] string databaseServerName,
		                                          [NotNull] string user,
		                                          [NotNull] string password)
		{
			return OpenSDEWorkspace(_defaultRepositoryName, driver,
			                        databaseServerName, user, password);
		}

		[NotNull]
		public static IWorkspace OpenSDEWorkspace(DirectConnectDriver driver,
		                                          [NotNull] string databaseServerName,
		                                          [NotNull] string versionName)
		{
			return OpenSDEWorkspace(_defaultRepositoryName, driver, databaseServerName,
			                        versionName);
		}

		/// <summary>
		/// Opens an SDE workspace
		/// </summary>
		/// <param name="repositoryName">Name of the SDE repository. In case of sql server, the 
		/// name of the database that contains the sde repository. In case of oracle, the name 
		/// of the schema that contains the sde repository (normally "SDE").</param>
		/// <param name="driver">The direct connect driver.</param>
		/// <param name="databaseServerName">The oracle database name, or sql server name</param>
		/// <param name="versionName">The optional version name. If not specified, the DEFAULT version is opened</param>
		/// <param name="hWnd">The window handle of the parent window.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenSDEWorkspace([NotNull] string repositoryName,
		                                          DirectConnectDriver driver,
		                                          [NotNull] string databaseServerName,
		                                          [CanBeNull] string versionName = null,
		                                          int hWnd = 0)
		{
			Assert.ArgumentNotNull(repositoryName, nameof(repositoryName));
			Assert.ArgumentNotNullOrEmpty(databaseServerName, nameof(databaseServerName));

			IWorkspaceFactory factory = GetSdeWorkspaceFactory();

			IPropertySet props = new PropertySetClass();

			string instance;
			switch (driver)
			{
				case DirectConnectDriver.Oracle:
				case DirectConnectDriver.Oracle9i:
				case DirectConnectDriver.Oracle10g:
				case DirectConnectDriver.Oracle11g:
					instance = GetSdeInstanceOra(driver, databaseServerName,
					                             repositoryName);
					if (StringUtils.IsNullOrEmptyOrBlank(versionName))
					{
						versionName = GetDefaultVersionName(repositoryName);
					}

					break;

				case DirectConnectDriver.SqlServer:
					instance = GetSdeInstanceSqlServer(driver, databaseServerName);
					props.SetProperty("DATABASE", repositoryName);
					break;

				case DirectConnectDriver.PostgreSQL:
					instance = GetSdeInstancePostgreSQL(driver, databaseServerName);
					props.SetProperty("DATABASE", repositoryName);
					if (StringUtils.IsNullOrEmptyOrBlank(versionName))
					{
						versionName = GetDefaultVersionName("SDE");
					}

					break;

				default:
					throw new NotSupportedException(
						string.Format("Unsupported driver: {0}", driver));
			}

			// direct connect, OSA
			props.SetProperty("INSTANCE", instance);
			props.SetProperty("SERVER", string.Empty);
			props.SetProperty("AUTHENTICATION_MODE", "OSA");

			if (StringUtils.IsNullOrEmptyOrBlank(versionName))
			{
				versionName = GetDefaultVersionName(factory,
				                                    PropertySetUtils.Clone(props),
				                                    hWnd);
			}

			if (versionName != null)
			{
				props.SetProperty("VERSION", versionName);
			}

			return OpenWorkspace(factory, props, hWnd);
		}

		[CanBeNull]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static string GetDefaultVersionName([NotNull] IWorkspaceFactory factory,
		                                            [NotNull] IPropertySet props,
		                                            int hWnd)
		{
			IWorkspace workspace = OpenWorkspace(factory, props, hWnd);

			var versionedWorkspace = workspace as IVersionedWorkspace;

			if (versionedWorkspace == null)
			{
				return null;
			}

			try
			{
				return versionedWorkspace.DefaultVersion.VersionName;
			}
			finally
			{
				// release workspace to avoid a workspace with incomplete connection properties remaining
				// in the workspace factory cache
				Marshal.ReleaseComObject(workspace);
			}
		}

		[NotNull]
		private static IWorkspace OpenDefaultVersion([NotNull] IWorkspace workspace)
		{
			Stopwatch watch = _msg.IsVerboseDebugEnabled
				                  ? _msg.DebugStartTiming()
				                  : null;

			try
			{
				var versionedWorkspace = workspace as IVersionedWorkspace;
				return versionedWorkspace != null
					       ? (IWorkspace) versionedWorkspace.DefaultVersion
					       : workspace;
			}
			finally
			{
				_msg.DebugStopTiming(watch, "getting default version");
			}
		}

		[NotNull]
		public static IWorkspace OpenSDEWorkspace(DirectConnectDriver driver,
		                                          [NotNull] string databaseServerName,
		                                          [NotNull] string user,
		                                          [NotNull] string password,
		                                          [NotNull] string versionName)
		{
			return OpenSDEWorkspace(_defaultRepositoryName, driver,
			                        databaseServerName, user, password,
			                        versionName);
		}

		[NotNull]
		public static IWorkspace OpenSDEWorkspace([NotNull] string server,
		                                          [NotNull] string instance,
		                                          [CanBeNull] string user,
		                                          [CanBeNull] string password,
		                                          [CanBeNull] string repositoryName =
			                                          null,
		                                          [CanBeNull] string versionName = null,
		                                          int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(server, nameof(server));
			Assert.ArgumentNotNullOrEmpty(instance, nameof(instance));
			Assert.ArgumentCondition(
				! (string.IsNullOrEmpty(password) && ! string.IsNullOrEmpty(user)),
				"User name is defined, but password is not");
			Assert.ArgumentCondition(
				! (string.IsNullOrEmpty(user) && ! string.IsNullOrEmpty(password)),
				"User name is undefined, but password is defined");

			if (string.IsNullOrEmpty(repositoryName))
			{
				repositoryName = _defaultRepositoryName;
			}

			bool versionNameSpecified = StringUtils.IsNotEmpty(versionName);

			// workaround for different behavior in 10.0 (dialog pops up if no version specified)
			// Starting with 10.1, the default version is opened if no version name is specified.
			if (! versionNameSpecified && RuntimeUtils.Is10_0)
			{
				// NOTE: does not work for Sql Server repository stored in DBO, and for PostgreSQL 
				// --> specify default version name explicitly in these cases
				versionName = GetDefaultVersionName(repositoryName);
				versionNameSpecified = true;
			}

			IWorkspaceFactory factory = GetSdeWorkspaceFactory();

			IPropertySet props = new PropertySetClass();

			// app server connect
			props.SetProperty("SERVER", server);
			props.SetProperty("INSTANCE",
			                  string.Format("{0}:{1}", instance, repositoryName));

			if (versionNameSpecified)
			{
				props.SetProperty("VERSION", versionName);
			}

			if (user == null)
			{
				props.SetProperty("AUTHENTICATION_MODE", "OSA");
			}
			else
			{
				props.SetProperty("USER", user);
				props.SetProperty("PASSWORD", password);
			}

			IWorkspace workspace = OpenWorkspace(factory, props, hWnd);

			return versionNameSpecified
				       ? workspace
				       : OpenDefaultVersion(workspace);
		}

		/// <summary>
		/// Opens an SDE workspace based on a connection file path.
		/// </summary>
		/// <param name="connectionFilePath">The full path to a sde connection file.</param>
		/// <param name="hWnd">The window handle of the parent window.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenSDEWorkspace([NotNull] string connectionFilePath,
		                                          int hWnd = 0)
		{
			Assert.ArgumentNotNullOrEmpty(connectionFilePath, nameof(connectionFilePath));

			IWorkspaceFactory factory = GetSdeWorkspaceFactory();

			try
			{
				return factory.OpenFromFile(connectionFilePath, hWnd);
			}
			catch (COMException exception)
			{
				_msg.Debug($"Error opening workspace using catalog path {connectionFilePath}",
				           exception);

				if (exception.ErrorCode == (decimal) fdoError.FDO_E_CONNECTION_CANCELLED)
				{
					// clear the parameters for this instance (to allow re-authentication on next try)
					IPropertySet props =
						factory.ReadConnectionPropertiesFromFile(connectionFilePath);

					IDictionary<string, object> dictionary =
						PropertySetUtils.GetDictionary(props);

					object server;
					object instance;
					if (dictionary.TryGetValue("SERVER", out server) &&
					    dictionary.TryGetValue("INSTANCE", out instance))
					{
						((ISetDefaultConnectionInfo2) factory).ClearParameters(
							(string) server, (string) instance);
					}
				}

				throw;
			}
		}

		/// <summary>
		/// Opens an SDE workspace based on a connection string.
		/// </summary>
		/// <param name="connectionString">The full path to a sde connection file.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenSDEWorkspaceFromString(
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			var factory = (IWorkspaceFactory2) GetSdeWorkspaceFactory();

			return OpenWorkspace(factory, connectionString);
		}

		/// <summary>
		/// Opens an SDE workspace based on a connection file path.
		/// </summary>
		/// <param name="connectionFilePath">The full path to a sde connection file.</param>
		/// <param name="versionName">The version name to open</param>
		/// <param name="hWnd">The window handle of the parent window.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenSDEWorkspace([NotNull] string connectionFilePath,
		                                          [CanBeNull] string versionName,
		                                          int hWnd = 0)
		{
			IWorkspace workspace = OpenSDEWorkspace(connectionFilePath, hWnd);

			return StringUtils.IsNotEmpty(versionName)
				       ? OpenWorkspaceVersion(workspace, versionName)
				       : workspace;
		}

		/// <summary>
		/// Opens the workspace referenced in a dataset name
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenWorkspace([NotNull] IDatasetName datasetName)
		{
			const bool defaultVersion = false;
			return OpenWorkspace(datasetName, defaultVersion);
		}

		[NotNull]
		public static IWorkspace OpenWorkspace([NotNull] string connectionString,
		                                       [NotNull] string factoryProgID)
		{
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
			Assert.ArgumentNotNullOrEmpty(factoryProgID, nameof(factoryProgID));

			try
			{
				var wsName = new WorkspaceNameClass
				             {
					             WorkspaceFactoryProgID = factoryProgID,
					             ConnectionString = connectionString
				             };

				return (IWorkspace) ((IName) wsName).Open();
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					string.Format(
						"Error opening workspace for connection string '{0}' (factory {1}): {2}",
						ReplacePassword(connectionString),
						factoryProgID,
						e.Message),
					e);
			}
		}

		/// <summary>
		/// Opens the workspace referenced in a dataset name, with the option to 
		/// return the workspace for the default version.
		/// </summary>
		/// <param name="datasetName">Name of the dataset.</param>
		/// <param name="defaultVersion">if set to <c>true</c> the workspace for the default version is returned. 
		/// Otherwise, the specific version that the dataset name points to is returned.</param>
		/// <returns></returns>
		[NotNull]
		public static IWorkspace OpenWorkspace([NotNull] IDatasetName datasetName,
		                                       bool defaultVersion)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));

			var workspaceName = (IWorkspaceName2) datasetName.WorkspaceName;

			try
			{
				if (defaultVersion)
				{
					IWorkspaceName defaultWsName =
						GetWorkspaceNameForDefault(workspaceName);

					IWorkspace workspace = OpenWorkspace(defaultWsName);

					var version = workspace as IVersion;

					if (version == null)
					{
						// not a versioned workspace
						return workspace;
					}

					if (! version.HasParent())
					{
						// no parent -> this is DEFAULT
						return workspace;
					}

					// This is not default. 
					// IMPORTANT: if no VERSION is specified in the ws name, ANY matching 
					//            open workspace is returned when opening the name!
					return OpenWorkspaceDefaultVersion(workspace);
				}

				return OpenWorkspace(workspaceName);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					string.Format("Unable to open workspace {0} for dataset {1}",
					              WorkspaceToString(workspaceName),
					              datasetName.Name),
					e);
			}
		}

		[CanBeNull]
		public static IWorkspace TryOpenWorkspace(
			[NotNull] string workspaceCatalogPath,
			out string message)
		{
			IWorkspace workspace = null;
			message = null;

			try
			{
				workspace = OpenWorkspace(workspaceCatalogPath);
			}
			catch (COMException comException)
			{
				string error = Enum.GetName(typeof(fdoError), comException.ErrorCode);

				message = $"Cannot open workspace {workspaceCatalogPath}: {error}";
				_msg.Debug(message, comException);
			}
			catch (Exception e)
			{
				message = $"Cannot open workspace {workspaceCatalogPath}";
				_msg.Debug(message, e);
			}

			return workspace;
		}

		public static IWorkspace OpenWorkspace([NotNull] string catalogPath)
		{
			Assert.ArgumentNotNullOrEmpty(catalogPath, nameof(catalogPath));

			// NOTE: Avoid using GPUtils.OpenWorkspaceFromCatalogPath because in case of an
			//       SDE workspace, the process hangs at the end: https://community.esri.com/thread/75664
			try
			{
				if (catalogPath.EndsWith(".sde",
				                         StringComparison.InvariantCultureIgnoreCase))
				{
					return OpenSDEWorkspace(catalogPath);
				}

				if (catalogPath.EndsWith(".gdb",
				                         StringComparison.InvariantCultureIgnoreCase))
				{
					return OpenFileGdbWorkspace(catalogPath);
				}

				if (catalogPath.EndsWith(".mdb",
				                         StringComparison.InvariantCultureIgnoreCase))
				{
					return OpenPgdbWorkspace(catalogPath);
				}

				if (catalogPath.EndsWith(".geodatabase",
				                         StringComparison.InvariantCultureIgnoreCase))
				{
					return OpenMobileGdbWorkspace(catalogPath);
				}

				if (Directory.Exists(catalogPath))
				{
					return (IWorkspace) OpenShapefileWorkspace(catalogPath);
				}

				throw new ArgumentOutOfRangeException(nameof(catalogPath),
				                                      $"Could not detect workspace type of {catalogPath}.");
			}
			catch (COMException comException)
			{
				if (comException.ErrorCode == (int) fdoError.FDO_E_LICENSE_NOT_INITIALIZED)
				{
					throw new InvalidOperationException("ArcGIS License not initialized.",
					                                    comException);
				}

				throw;
			}
		}

		public static IFeatureWorkspace OpenFeatureWorkspace([NotNull] string catalogPath)
		{
			return (IFeatureWorkspace) OpenWorkspace(catalogPath);
		}

		/// <summary>
		/// Determines whether two workspace references are pointing to the same workspace, doing 
		/// either a version-specific comparison or disregarding version differences.
		/// </summary>
		/// <param name="workspace1">The workspace1, may be null</param>
		/// <param name="workspace2">The workspace2, may be null</param>
		/// <param name="workspaceComparison">Controls how workspaces are compared (exact comparison, 
		/// which includes version name, or disregarding version differences)
		/// </param>
		/// <returns>
		/// 	<c>true</c> if the workspace references point to the same workspace; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// If both workspaces are null, they are considered to be the same.
		/// </remarks>
		public static bool IsSameWorkspace([CanBeNull] IWorkspace workspace1,
		                                   [CanBeNull] IWorkspace workspace2,
		                                   WorkspaceComparison workspaceComparison)
		{
			if (workspace1 == null && workspace2 == null)
			{
				return true;
			}

			if (workspace1 == null || workspace2 == null)
			{
				return false;
			}

			switch (workspaceComparison)
			{
				case WorkspaceComparison.Exact:
					return workspace1 == workspace2;

				case WorkspaceComparison.AnyUserAnyVersion:
					return IsSameDatabase(workspace1, workspace2);

				case WorkspaceComparison.AnyUserSameVersion:
					return IsSameVersion(workspace1, workspace2);

				default:
					throw new ArgumentOutOfRangeException(nameof(workspaceComparison));
			}
		}

		/// <summary>
		/// Determine if two workspaces represent the same GDB version.
		/// </summary>
		/// <param name="workspace1">The first workspace.</param>
		/// <param name="workspace2">The other workspace.</param>
		/// <returns>
		/// True if they represent the same GDB version; otherwise, false.
		/// </returns>
		public static bool IsSameVersion([CanBeNull] IWorkspace workspace1,
		                                 [CanBeNull] IWorkspace workspace2)
		{
			if (workspace1 == workspace2)
			{
				return true; // same instance => same version
			}

			if (! IsSameDatabase(workspace1, workspace2))
			{
				return false; // different database => different version
			}

			if (workspace1 is IEquatable<IWorkspace> equatableWorkspace1)
			{
				return equatableWorkspace1.Equals(workspace2);
			}

			if (workspace2 is IEquatable<IWorkspace> equatableWorkspace2)
			{
				return equatableWorkspace2.Equals(workspace1);
			}

			var version1 = workspace1 as IVersion;
			var version2 = workspace2 as IVersion;

			if (version1 == null || version2 == null)
			{
				return true; // not versioned => only default version => same version
			}

			// Versions within a database are identified by their names.
			// Names are case sensitive and qualified with the owner name.
			// Only the qualified names (with the owner prefix) are unique.
			return string.Equals(version1.VersionName, version2.VersionName);
		}

		/// <summary>
		/// Determines whether two workspace connections point to the same database, 
		/// regardless of versions or connection information.
		/// </summary>
		/// <param name="workspace1">The first workspace.</param>
		/// <param name="workspace2">The second workspace.</param>
		/// <returns>
		/// 	<c>true</c> if the workspace references point to the same workspace; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsSameDatabase([CanBeNull] IWorkspace workspace1,
		                                  [CanBeNull] IWorkspace workspace2)
		{
			// workspaces may be null

			if (workspace1 == null || workspace2 == null)
			{
				return false;
			}

			if (workspace1 == workspace2)
			{
				// same workspace instance. obviously the same db
				return true;
			}

			if (workspace1 is GdbWorkspace gdbWorkspace1)
			{
				return gdbWorkspace1.IsSameDatabase(workspace2);
			}

			if (workspace2 is GdbWorkspace gdbWorkspace2)
			{
				return gdbWorkspace2.IsSameDatabase(workspace1);
			}

			if (workspace1.Type != workspace2.Type)
			{
				// Different workspace types (file system vs. local gdb vs. remote gdb). Different.
				return false;
			}

			if (workspace1.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace &&
			    workspace2.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				// Both not remote gdb. Compare based on workspace path.
				return UsesSameWorkspacePath(workspace1, workspace2);
			}
			
			var versionedWorkspace1 = workspace1 as IVersionedWorkspace;
			var versionedWorkspace2 = workspace2 as IVersionedWorkspace;

			if (versionedWorkspace1 == null && versionedWorkspace2 == null)
			{
				// Both not versioned, despite being remote gdbs

				if (string.IsNullOrEmpty(workspace1.PathName) &&
				    string.IsNullOrEmpty(workspace2.PathName))
				{
					// This could be a non-geodatabase Postgres database:
					// Note: Even the same password results in a different encrypted string -> replace
					string connectionString1 = GetConnectionString(workspace1, true);
					string connectionString2 = GetConnectionString(workspace2, true);

					return connectionString1.Equals(connectionString2,
					                                StringComparison.OrdinalIgnoreCase);
				}

				_msg.WarnFormat(
					"Comparison of unknown remote database workspaces, comparing workspace paths.{0}" +
					"- Remote database workspace 1: {1}{0}" +
					"- Remote database workspace 2: {2}{0}",
					Environment.NewLine, WorkspaceToString(workspace1),
					WorkspaceToString(workspace2));

				return UsesSameWorkspacePath(workspace1, workspace2);
			}

			if (versionedWorkspace1 == null || versionedWorkspace2 == null)
			{
				// One is versioned, the other not. Different.
				return false;
			}

			// Both are versioned. 

			// IMPORTANT: IDatabaseConnectionInfo2.ConnectionServer returns the CLIENT
			//            in case of OSA connections!!!!!!! Don't use here

			// IMPORTANT: connection properties can't be used for comparison, since
			//            the database name is in the encrypted password string in case of
			//            DBMS authentication

			//string server1 =
			//    ((IDatabaseConnectionInfo2) workspace1).ConnectionServer;
			//string server2 =
			//    ((IDatabaseConnectionInfo2) workspace2).ConnectionServer;

			//if (_msg.IsVerboseDebugEnabled)
			//{
			//    _msg.DebugFormat("Compare server names ({0}, {1})", server1, server2);
			//}

			//if (server1 != null && ! server1.Equals(server2, StringComparison.OrdinalIgnoreCase))
			//{
			//    return false;
			//}

			//if (IsInstanceEqual(workspace1.ConnectionProperties,
			//                    workspace2.ConnectionProperties))
			//{
			//    // instance and server properties are equal --> references same db
			//    return true;
			//}

			// Still not sure. Compare creation date of default version.
			IVersion defaultVersion1 = versionedWorkspace1.DefaultVersion;
			IVersion defaultVersion2 = versionedWorkspace2.DefaultVersion;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Compare default version instances");
			}

			if (defaultVersion1 == defaultVersion2)
			{
				// the same default version (only equal if same credentials also)
				return true;
			}

			string defaultVersionName1 = defaultVersion1.VersionName ??
			                             string.Empty;
			string defaultVersionName2 = defaultVersion2.VersionName ??
			                             string.Empty;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version names ({0}, {1})",
				                 defaultVersionName1, defaultVersionName2);
			}

			if (! defaultVersionName1.Equals(defaultVersionName2,
			                                 StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// not the same default version. might still be the same database,
			// but different credentials. Compare creation date of the default version.

			IVersionInfo default1Info = defaultVersion1.VersionInfo;
			IVersionInfo default2Info = defaultVersion2.VersionInfo;

			string creationDate1 = default1Info.Created.ToString();
			string creationDate2 = default2Info.Created.ToString();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version creation date: {0},{1}",
				                 creationDate1, creationDate2);
			}

			if (! Equals(creationDate1, creationDate2))
			{
				return false;
			}

			string modifyDate1 = default1Info.Modified.ToString();
			string modifyDate2 = default2Info.Modified.ToString();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version last modified date: {0},{1}",
				                 modifyDate1, modifyDate2);
			}

			return Equals(modifyDate1, modifyDate2);
		}

		private static bool UsesSameWorkspacePath([NotNull] IWorkspace workspace1,
		                                          [NotNull] IWorkspace workspace2)
		{
			Assert.ArgumentNotNull(workspace1, nameof(workspace1));
			Assert.ArgumentNotNull(workspace2, nameof(workspace2));

			if (string.IsNullOrEmpty(workspace1.PathName) ||
			    string.IsNullOrEmpty(workspace2.PathName))
			{
				return false;
			}

			//Determines whether two Uri instances have the same value.
			// e.g. these paths are equal
			// C:\Users\daro\AppData\Local\Temp\GdbWorkspaceTest.gdb
			// file:///C:/Users/daro/AppData/Local/Temp/GdbWorkspaceTest.gdb
			return Equals(new Uri(workspace1.PathName), new Uri(workspace2.PathName));
		}

		/// <summary>
		/// Opens the feature workspace referenced in a dataset name
		/// </summary>
		/// <param name="datasetName">The dataset name.</param>
		/// <returns></returns>
		[NotNull]
		public static IFeatureWorkspace OpenFeatureWorkspace(
			[NotNull] IDatasetName datasetName)
		{
			return (IFeatureWorkspace) OpenWorkspace(datasetName);
		}

		public static void EnableAllSdeSchemaCaches()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			IWorkspaceFactorySchemaCache factoryCache = GetSdeFactoryCache();

			if (factoryCache != null)
			{
				factoryCache.EnableSchemaCaching();
				factoryCache.EnableAllSchemaCaches();
			}

			_msg.DebugStopTiming(watch, "Enabling sde schema caches");
		}

		public static void DisableAllSdeSchemaCaches()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			IWorkspaceFactorySchemaCache factoryCache = GetSdeFactoryCache();

			if (factoryCache != null)
			{
				factoryCache.DisableSchemaCaching();
				factoryCache.DisableAllSchemaCaches();
			}

			_msg.DebugStopTiming(watch, "Disabling sde schema caches");
		}

		public static bool DisableSchemaCache([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var factoryCache = workspace.WorkspaceFactory as IWorkspaceFactorySchemaCache;

			if (factoryCache == null)
			{
				const bool plural = true;
				_msg.DebugFormat("Schema caching not implemented for {0}",
				                 workspace.WorkspaceFactory.WorkspaceDescription[plural]);
				return false;
			}

			Stopwatch watch = _msg.DebugStartTiming();

			factoryCache.DisableSchemaCache(workspace);

			_msg.DebugStopTiming(watch, "Disabling schema cache for workspace");

			return true;
		}

		public static bool IsSchemaCacheStale([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var factoryCache = workspace.WorkspaceFactory as IWorkspaceFactorySchemaCache;

			if (factoryCache == null)
			{
				const bool plural = true;
				_msg.DebugFormat("Schema caching not implemented for {0}",
				                 workspace.WorkspaceFactory.WorkspaceDescription[plural]);
				return false;
			}

			Stopwatch watch = _msg.DebugStartTiming();

			bool stale = factoryCache.IsSchemaCacheStale(workspace);

			_msg.DebugStopTiming(watch, "Determine staleness of workspace schema cache");

			return stale;
		}

		public static bool EnableSchemaCache([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var factoryCache = workspace.WorkspaceFactory as IWorkspaceFactorySchemaCache;

			if (factoryCache == null)
			{
				const bool plural = true;
				_msg.DebugFormat("Schema caching not implemented for {0}",
				                 workspace.WorkspaceFactory.WorkspaceDescription[plural]);
				return false;
			}

			var memoryUsageInfo = new MemoryUsageInfo();
			Stopwatch watch = _msg.DebugStartTiming();

			factoryCache.EnableSchemaCache(workspace);

			_msg.DebugStopTiming(watch, "Enabled schema cache for {0}, {1}",
			                     GetConnectionString(workspace, true),
			                     memoryUsageInfo.Refresh());

			return true;
		}

		public static bool TryRefreshVersion([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var version = workspace as IVersion;

			if (version == null)
			{
				return false;
			}

			bool refreshed = false;

			try
			{
				if (((IWorkspaceEdit) workspace).IsBeingEdited())
				{
					return false;
				}

				if (! IsVersionRedefined(version))
				{
					return false;
				}

				_msg.InfoFormat("Version {0} is redefined and will be refreshed",
				                version.VersionName);
				version.RefreshVersion();

				refreshed = true;
			}
			catch (COMException e)
			{
				if (e.ErrorCode == (int) fdoError.FDO_E_SE_VERSION_NOEXIST &&
				    IsMobileGeodatabase(workspace))
				{
					// Mobile geodatabases implement IVersionedWorkspace and IVersion,
					// but everything else fails...
				}
				else
				{
					throw;
				}
			}

			return refreshed;
		}

		public static bool IsVersionRedefined([NotNull] IVersion version)
		{
			try
			{
#if Server11
				var version2 = version;
#else
				var version2 = (IVersion2) version;
#endif
				return version2.IsRedefined;
			}
			catch (Exception e)
			{
				// IVersion2.IsRedefined is likely failing in PostGIS:
				// System.Runtime.InteropServices.COMException (0x80041538): Underlying DBMS error [no connection to the server ::SQLSTATE=Þ] [sde.DEFAULT]
				_msg.Debug("Error while determining whether the version is re-defined.", e);

				return false;
			}
		}

		public static bool IsVersionRedefined(IFeatureWorkspace workspace)
		{
			var version = workspace as IVersion;

			if (version == null)
			{
				return false;
			}

			return IsVersionRedefined(version);
		}

		[NotNull]
		public static IVersion CreateVersion(
			[NotNull] IWorkspace workspace,
			[NotNull] string versionName,
			esriVersionAccess access = esriVersionAccess.esriVersionAccessPrivate)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(versionName, nameof(versionName));

			TryRefreshVersion(workspace);

			var parentVersion = (IVersion) workspace;
			IVersion newVersion = parentVersion.CreateVersion(versionName);

			newVersion.Access = access;
			return newVersion;
		}

		[NotNull]
		public static IVersion CreateVersion([NotNull] IFeatureWorkspace workspace,
		                                     [NotNull] string versionName)
		{
			return CreateVersion((IWorkspace) workspace, versionName);
		}

		[NotNull]
		public static IVersion CreateVersion([NotNull] IFeatureWorkspace workspace,
		                                     [NotNull] string versionName,
		                                     esriVersionAccess access)
		{
			return CreateVersion((IWorkspace) workspace, versionName, access);
		}

		public static bool DeleteVersion([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string name)
		{
			return DeleteVersion((IWorkspace) workspace, name);
		}

		public static bool DeleteVersion([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string name,
		                                 DeleteChildVersions delete)
		{
			return DeleteVersion((IWorkspace) workspace, name, delete);
		}

		public static bool DeleteVersion([NotNull] IVersion version)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			return DeleteVersion((IWorkspace) version, version.VersionName);
		}

		public static bool DeleteVersion(
			[NotNull] IWorkspace workspace,
			[NotNull] string name,
			DeleteChildVersions deleteChildVersions = DeleteChildVersions.No)
		{
			var deleted = false;

			try
			{
				var versionedWorkspace = workspace as IVersionedWorkspace;

				IVersion version = versionedWorkspace?.FindVersion(name);

				if (version != null)
				{
					if (deleteChildVersions == DeleteChildVersions.Yes)
					{
						deleted =
							DeleteVersionTree(versionedWorkspace, version.VersionInfo);
					}
					else
					{
						DeleteVersionInSeparateThread(version);

						// TODO: refresh parent version? There have been cases
						//		 where the version could still be 'found' where it didn't exist

						deleted = true;
					}

					Marshal.ReleaseComObject(version);
				}
			}
			catch (COMException e)
			{
				if (e.ErrorCode == (int) fdoError.FDO_E_SE_VERSION_NOEXIST)
				{
					// version not found, ignore
				}
				else
				{
					throw;
				}
			}

			return deleted;
		}

		public static bool DeleteVersionInCurrentThread(
			[NotNull] IWorkspace workspace,
			[NotNull] string name)
		{
			var deleted = false;

			try
			{
				var versionedWorkspace = workspace as IVersionedWorkspace;

				IVersion version = versionedWorkspace?.FindVersion(name);

				if (version != null)
				{
					version.Delete();

					Marshal.ReleaseComObject(version);

					deleted = true;
				}
			}
			catch (COMException e)
			{
				if (e.ErrorCode == (int) fdoError.FDO_E_SE_VERSION_NOEXIST)
				{
					// version not found, ignore
				}
				else
				{
					throw;
				}
			}

			return deleted;
		}

		public static void DeleteAllVersions([NotNull] IWorkspace workspace)
		{
			var versionedWorkspace = (IVersionedWorkspace) workspace;
			IVersion defaultVersion = versionedWorkspace.DefaultVersion;

			DeleteVersionTree(versionedWorkspace, defaultVersion.VersionInfo);
		}

		/// <summary>
		/// Returns a value indicating if a version with the given name exists in a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="versionName">Name of the version.</param>
		/// <returns><c>true</c> if the version exists, <c>false</c>otherwise.</returns>
		public static bool ExistsVersion([NotNull] IWorkspace workspace,
		                                 [NotNull] string versionName)
		{
			return GetVersionInfo(workspace, versionName) != null;
		}

		/// <summary>
		/// Returns a value indicating if a version with the given name exists in a workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="versionName">Name of the version.</param>
		/// <returns><c>true</c> if the version exists, <c>false</c>otherwise.</returns>
		public static bool ExistsVersion([NotNull] IFeatureWorkspace workspace,
		                                 [NotNull] string versionName)
		{
			return ExistsVersion((IWorkspace) workspace, versionName);
		}

		/// <summary>
		/// Reconciles the editWorkspace with the reconcileWorkspace. The edit workspace should
		/// be a descendent of the reconcileWorkspace.
		/// </summary>
		/// <param name="editWorkspace">The edit workspace.</param>
		/// <param name="reconcileWorkspace">The reconcile workspace.</param>
		/// <param name="forPosting">if set to <c>true</c> an exclusive version lock is acquired
		/// which guarantees the success of a subsequent post operation.</param>
		/// <param name="childVersionWins">if set to <c>true</c> the reconcile is done in favor
		/// of the child version.</param>
		/// <param name="columnLevelConflictDetection">Whether conflict detection should be on 
		/// column level or on object level.</param>
		/// <returns>
		/// An enumeration of the conflict classes. If there are no conflicts null is returned.
		/// </returns>
		[CanBeNull]
		public static IEnumConflictClass Reconcile(
			[NotNull] IFeatureWorkspace editWorkspace,
			[NotNull] IFeatureWorkspace reconcileWorkspace,
			bool forPosting, bool childVersionWins,
			bool columnLevelConflictDetection)
		{
			Assert.ArgumentNotNull(editWorkspace, nameof(editWorkspace));
			Assert.ArgumentNotNull(reconcileWorkspace, nameof(reconcileWorkspace));

			Assert.True(((IWorkspaceEdit2) editWorkspace).IsBeingEdited(),
			            "The editWorkspace is not being edited. Start edit session first.");
			// NOTE: the reconcile operation starts (and stops) its own edit operation
			//       however in case of an error the edit operation is not aborted.
			// TODO: consider aborting the edit operation in this method rather
			//       than in the caller.
			Assert.False(((IWorkspaceEdit2) editWorkspace).IsInEditOperation,
			             "The editWorkspace is in an edit operation.");

			var editVersion = (IVersion) editWorkspace;
			var reconcileVersion = (IVersion) reconcileWorkspace;

			_msg.DebugFormat("Is edit version re-defined: {0}",
			                 IsVersionRedefined(editWorkspace));
			_msg.DebugFormat("Is reconcile version re-defined: {0}",
			                 IsVersionRedefined(reconcileVersion));

			return Reconcile(editVersion, reconcileVersion, forPosting, childVersionWins,
			                 columnLevelConflictDetection);
		}

		private static IEnumConflictClass Reconcile(IVersion editVersion, IVersion reconcileVersion,
		                                            bool forPosting, bool childVersionWins,
		                                            bool columnLevelConflictDetection)
		{
			_msg.DebugFormat(
				"Calling IVersionEdit4.Reconcile4() on {0} with target {1}, acquireLock: {2}, childWins: {3}, columnLevelDetection: {4}",
				editVersion.VersionName,
				reconcileVersion.VersionInfo.VersionName,
				forPosting, childVersionWins, columnLevelConflictDetection);

			_msg.DebugFormat("Reconciling user is: {0}",
			                 GetConnectedUser((IWorkspace) editVersion));

#if Server11
			var versionEdit = (IVersionEdit) editVersion;
#else
			var versionEdit = (IVersionEdit4) editVersion;
#endif
			bool hasConflicts =
				versionEdit.Reconcile4(reconcileVersion.VersionInfo.VersionName,
				                       forPosting, false, childVersionWins,
				                       columnLevelConflictDetection);

			_msg.DebugFormat("Reconcile4 completed. HasConflicts: {0}", hasConflicts);

			return hasConflicts
				       ? versionEdit.ConflictClasses
				       : null;
		}

		/// <summary>
		/// Posts the changes of a workspace to it's parent workspace
		/// </summary>
		/// <param name="workspaceToPost">The workspace to post.</param>
		/// <param name="destinationWorkspace">The destination workspace (parent).</param>
		public static void PostVersion([NotNull] IFeatureWorkspace workspaceToPost,
		                               [NotNull] IFeatureWorkspace destinationWorkspace)
		{
			Assert.ArgumentNotNull(workspaceToPost, nameof(workspaceToPost));
			Assert.ArgumentNotNull(destinationWorkspace, nameof(destinationWorkspace));

			var versionToPost = (IVersionEdit) workspaceToPost;
			var destinationVersion = (IVersion) destinationWorkspace;

			PostVersion(versionToPost, destinationVersion);
		}

		public static void PostVersion([NotNull] IVersionEdit versionToPost,
		                               [NotNull] IVersion destinationVersion)
		{
			Assert.ArgumentNotNull(versionToPost, nameof(versionToPost));
			Assert.ArgumentNotNull(destinationVersion, nameof(destinationVersion));

			if (! versionToPost.CanPost())
			{
				throw new InvalidOperationException(string.Format(
					                                    "Unable to post version {0}. Consider reconciling first.",
					                                    ((IVersion) versionToPost)
					                                    .VersionName));
			}

			_msg.DebugFormat("Posting {0} to {1}", ((IVersion) versionToPost).VersionName,
			                 destinationVersion.VersionName);

			try
			{
				versionToPost.Post(destinationVersion.VersionName);
			}
			catch (COMException e)
			{
				_msg.Debug("Exception in post.", e);

				if (e.ErrorCode == (int) fdoError.FDO_E_VERSION_REDEFINED)
				{
					if (IsVersionRedefined(destinationVersion))
					{
						_msg.Debug(
							"Target version is redefined. Another reconcile is needed first.");
					}
				}

				throw;
			}

			_msg.Debug("Post completed.");
		}

		public static bool CanAcquireVersionLock(
			[NotNull] IFeatureWorkspace featureWorkspace,
			esriLockType desiredLockType,
			bool hasKnownLock = false)
		{
			return CanAcquireVersionLock((IVersion) featureWorkspace, desiredLockType,
			                             hasKnownLock);
		}

		/// <summary>
		/// Determines whether a specific version can acquire the desired version lock.
		/// </summary>
		/// <param name="version">The version to be locked.</param>
		/// <param name="desiredLockType">Type of the desired lock.</param>
		/// <param name="hasKnownLock"></param>
		/// <param name="notifications">The notifications collection to add information
		/// in case the lock cannot be acquired.</param>
		/// <returns>
		/// 	<c>true</c> if the lock can be acquired; otherwise, <c>false</c>.
		/// </returns>
		public static bool CanAcquireVersionLock(
			[NotNull] IVersion version,
			esriLockType desiredLockType,
			bool hasKnownLock = false,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			string message = string.Empty;
			var result = true;

			var connectionInfo = (IDatabaseConnectionInfo2) version;
			string connectedDbUser = connectionInfo.ConnectedUser;

			var knownLockAccountedFor = false;

			IEnumLockInfo lockInfos = version.VersionLocks;
			lockInfos.Reset();

			ILockInfo lockInfo;
			while ((lockInfo = lockInfos.Next()) != null)
			{
				string lockMessage;

				if (PreventsRequestedLock(lockInfo, desiredLockType, out lockMessage))
				{
					_msg.DebugFormat("Lock found: {0}", lockMessage);

					if (connectedDbUser == lockInfo.UserName &&
					    hasKnownLock && ! knownLockAccountedFor)
					{
						_msg.DebugFormat(
							"Lock owned by current user - assuming from the same process.");

						// A known lock is accounted for only once - can the same user have several locks?
						knownLockAccountedFor = true;
					}
					else
					{
						NotificationUtils.Add(notifications,
						                      "Unable to acquire a lock on version {0}: {1}",
						                      version.VersionName, message);

						result = false;
					}
				}
			}

			return result;
		}

		private static bool PreventsRequestedLock([NotNull] ILockInfo lockInfo,
		                                          esriLockType requested,
		                                          [NotNull] out string message)
		{
			string lockType = lockInfo.LockType == esriLockType.esriLockTypeExclusive
				                  ? "exclusive"
				                  : "shared";

			message = string.Format("User {0} holds a {1} lock",
			                        lockInfo.UserName, lockType);

			if (requested == esriLockType.esriLockTypeExclusive)
			{
				// any lock prevents an exclusive lock
				return true;
			}

			if (requested == esriLockType.esriLockTypeShared &&
			    lockInfo.LockType == esriLockType.esriLockTypeExclusive)
			{
				// an exclusive lock prevents a shared lock
				return true;
			}

			return false;
		}

		public static void SetCurrrentVersion([NotNull] IWorkspace oracleWorkspace,
		                                      [NotNull] string versionName,
		                                      bool hasKnownLock = false)
		{
			Assert.ArgumentNotNull(oracleWorkspace, nameof(oracleWorkspace));
			Assert.ArgumentNotNullOrEmpty(versionName, nameof(versionName));

			string statement = string.Format(
				@"BEGIN sde.version_util.set_current_version('{0}'); END;",
				versionName);

			_msg.DebugFormat("Executing statement: {0}", statement);

			oracleWorkspace.ExecuteSQL(statement);
		}

		[CanBeNull]
		public static string TryGetCatalogPath([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			string path = workspace.PathName;
			if (StringUtils.IsNotEmpty(path) &&
			    (File.Exists(path) || Directory.Exists(path)))
			{
				return path;
			}

			if (workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace)
			{
				IWorkspaceName workspaceName = GetWorkspaceName(workspace);

				string database = PropertySetUtils.GetStringValue(
					workspaceName.ConnectionProperties, "DATABASE");

				if (StringUtils.IsNotEmpty(database) &&
				    (File.Exists(database) || Directory.Exists(database)))
				{
					return database;
				}
			}

			return null;
		}

		[NotNull]
		public static string GetConnectionString([NotNull] IWorkspace workspace,
		                                         bool replacePassword = false,
		                                         [CanBeNull] string passwordPadding =
			                                         null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var name = (IWorkspaceName) ((IDataset) workspace).FullName;

			return GetConnectionString(name, replacePassword, passwordPadding);
		}

		[NotNull]
		public static string GetConnectionString([NotNull] IWorkspaceName workspaceName,
		                                         bool replacePassword = false,
		                                         [CanBeNull] string passwordPadding = null)
		{
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));

			var workspaceName2 = (IWorkspaceName2) workspaceName;

			return GetConnectionString(workspaceName2, replacePassword, passwordPadding);
		}

		[NotNull]
		public static string GetConnectionString([NotNull] IWorkspaceName2 workspaceName,
		                                         bool replacePassword = false,
		                                         [CanBeNull] string passwordPadding =
			                                         null)
		{
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));

			string connectionString = IsInMemoryWorkspace(workspaceName)
				                          ? string.Empty
				                          : workspaceName.ConnectionString;

			if (! replacePassword)
			{
				return connectionString;
			}

			string replaced = ReplacePassword(connectionString, passwordPadding);
			Assert.NotNull(replaced, "replaced string is null");

			return replaced;
		}

		[NotNull]
		public static string GetFactoryProgId([NotNull] IWorkspace workspace)
		{
			return Assert.NotNull(GetWorkspaceName(workspace).WorkspaceFactoryProgID,
			                      "factory progId is null");
		}

		[NotNull]
		public static string WorkspaceToString([CanBeNull] IWorkspaceName workspaceName)
		{
			if (workspaceName == null)
			{
				return "<workspace not defined>";
			}

			string result;
			try
			{
				result = workspaceName.Type ==
				         esriWorkspaceType.esriRemoteDatabaseWorkspace
					         ? GetConnectionString(workspaceName, true)
					         : workspaceName.PathName;
			}
			catch (Exception e)
			{
				return $"<Error converting to string: {e.Message}>";
			}

			return result;
		}

		[NotNull]
		public static string WorkspaceToString([CanBeNull] IWorkspace workspace)
		{
			if (workspace == null)
			{
				return "<workspace not defined>";
			}

			IWorkspaceName workspaceName = GetWorkspaceName(workspace);

			return WorkspaceToString(workspaceName);
		}

		[NotNull]
		public static string WorkspaceToString(
			[CanBeNull] IFeatureWorkspace featureWorkspace)
		{
			return WorkspaceToString(featureWorkspace as IWorkspace);
		}

		/// <summary>
		/// Builds a string with the connection properties of the form 
		/// key1: value1; key2: value2.
		/// It is not a connection string but only for display.
		/// </summary>
		/// <param name="propertySet">The property set used to open the workspace.</param>
		/// <returns></returns>
		[NotNull]
		public static string WorkspaceConnectionPropertiesToString(
			[NotNull] IPropertySet propertySet)
		{
			Assert.ArgumentNotNull(propertySet, nameof(propertySet));

			const string instanceKeyWord = "INSTANCE";

			IDictionary<string, object> propertyDict =
				PropertySetUtils.GetDictionary(propertySet);

			string result = string.Empty;

			string instanceValue = PropertySetUtils.GetStringValue(
				propertyDict, instanceKeyWord);

			bool isOracle = instanceValue.ToLower().Contains("oracle");

			foreach (KeyValuePair<string, object> keyValuePair in propertyDict)
			{
				if (keyValuePair.Value == null)
				{
					continue;
				}

				string displayKey = keyValuePair.Key;
				string displayValue = keyValuePair.Value.ToString();

				if (IsPasswordPropertyName(keyValuePair.Key))
				{
					const string passwordPlaceholder = "*******";
					const string passwordSeparator = "@";

					if (isOracle && displayValue.Contains(passwordSeparator))
					{
						string databaseName = displayValue.Substring(
							displayValue.LastIndexOf(passwordSeparator,
							                         StringComparison.Ordinal) + 1);

						displayValue = string.Format("{0}{1}{2}",
						                             passwordPlaceholder,
						                             passwordSeparator,
						                             databaseName);
					}
					else
					{
						displayValue = passwordPlaceholder;
					}
				}

				if (! string.IsNullOrEmpty(result))
				{
					result += ";";
				}

				result += string.Format("{0}:{1}", displayKey, displayValue);
			}

			return result;
		}

		[CanBeNull]
		public static string ReplacePassword([CanBeNull] string workspaceConnectionString,
		                                     [CanBeNull] string passwordPadding = null)
		{
			if (workspaceConnectionString == null)
			{
				return null;
			}

			if (passwordPadding == null)
			{
				passwordPadding = "**********";
			}

			var result = StringUtils.RemoveWhiteSpaceCharacters(workspaceConnectionString);
			foreach (string passwordKeyword in GetPasswordKeywords())
			{
				string keyword = $"{passwordKeyword}=";

				// NOTE: The various password keywords contain each other. We have to search
				//       including the delimiters:
				int keywordIndex;
				if (result.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
				{
					keywordIndex = 0;
				}
				else
				{
					keyword = $";{keyword}";
					keywordIndex = result.IndexOf(keyword, 0,
					                              StringComparison.OrdinalIgnoreCase);
				}

				if (keywordIndex < 0)
				{
					continue;
				}

				result = ReplacePassword(result,
				                         passwordPadding,
				                         keywordIndex, passwordKeyword);
			}

			return result;
		}

		private static string ReplacePassword(string workspaceConnectionString,
		                                      string passwordPadding,
		                                      int passwordKeywordIndex, string passwordKeyword)
		{
			// there is a password in the string, replace it
			int pwdSeparator1Index =
				workspaceConnectionString.IndexOf("=",
				                                  passwordKeywordIndex +
				                                  passwordKeyword.Length,
				                                  StringComparison.Ordinal);

			if (pwdSeparator1Index < 0)
			{
				return workspaceConnectionString;
			}

			int pwdStartIndex = pwdSeparator1Index + 1;
			int pwdSeparator2Index = workspaceConnectionString.IndexOf(";", pwdStartIndex,
				StringComparison
					.Ordinal);

			int pwdEndIndex = pwdSeparator2Index < 0
				                  ? workspaceConnectionString.Length - 1
				                  : pwdSeparator2Index - 1;

			return workspaceConnectionString.Remove(pwdStartIndex,
			                                        pwdEndIndex - pwdStartIndex + 1)
			                                .Insert(pwdStartIndex, passwordPadding);
		}

		[NotNull]
		public static IWorkspaceName2 GetWorkspaceName([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			return (IWorkspaceName2) ((IDataset) workspace).FullName;
		}

		[NotNull]
		public static IWorkspaceName2 GetWorkspaceName(
			[NotNull] IFeatureWorkspace workspace)
		{
			return GetWorkspaceName((IWorkspace) workspace);
		}

		[NotNull]
		public static IWorkspaceName2 GetWorkspaceName([NotNull] IVersion version)
		{
			return GetWorkspaceName((IWorkspace) version);
		}

		[NotNull]
		public static string GetConnectedUser([NotNull] IWorkspace workspace)
		{
			var connectionInfo = workspace as IDatabaseConnectionInfo2;
			return connectionInfo == null
				       ? string.Empty
				       : connectionInfo.ConnectedUser ?? string.Empty;
		}

		public static bool IsBeingEdited([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			return ((IWorkspaceEdit) workspace).IsBeingEdited();
		}

		public static bool IsBeingEdited([NotNull] IFeatureWorkspace workspace)
		{
			return IsBeingEdited((IWorkspace) workspace);
		}

		public static bool HasEdits([NotNull] IWorkspace workspace)
		{
			var hasEdits = false;
			((IWorkspaceEdit) workspace).HasEdits(ref hasEdits);

			return hasEdits;
		}

		public static bool HasEdits([NotNull] IFeatureWorkspace featureWorkspace)
		{
			return HasEdits((IWorkspace) featureWorkspace);
		}

		public static bool HasUndos([NotNull] IWorkspace workspace)
		{
			var hasUndos = false;
			((IWorkspaceEdit) workspace).HasUndos(ref hasUndos);

			return hasUndos;
		}

		public static bool HasUndos([NotNull] IFeatureWorkspace featureWorkspace)
		{
			return HasUndos((IWorkspace) featureWorkspace);
		}

		public static bool HasRedos([NotNull] IWorkspace workspace)
		{
			var hasRedos = false;
			((IWorkspaceEdit) workspace).HasRedos(ref hasRedos);

			return hasRedos;
		}

		public static bool HasRedos([NotNull] IFeatureWorkspace featureWorkspace)
		{
			return HasRedos((IWorkspace) featureWorkspace);
		}

		[NotNull]
		public static string GetUnqualifiedVersionName(
			[NotNull] string qualifiedVersionName)
		{
			string[] nameParts = GetVersionNameParts(qualifiedVersionName);

			return nameParts[1];
		}

		[NotNull]
		public static string GetVersionOwnerName([NotNull] string qualifiedVersionName)
		{
			string[] nameParts = GetVersionNameParts(qualifiedVersionName);

			return nameParts[0];
			//string ownerRaw = nameParts[0];

			//const char quoteChar = '"';

			//return ownerRaw.Trim(quoteChar);
		}

		public static IFeatureWorkspace OpenShapefileWorkspace([NotNull] string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			IWorkspaceFactory factory = GetShapefileWorkspaceFactory();

			IPropertySet props = new PropertySetClass();
			props.SetProperty("DATABASE", path);

			return (IFeatureWorkspace) factory.Open(props, 0);
		}

		[NotNull]
		public static IRasterWorkspace2 OpenRasterWorkspace([NotNull] string directory)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));

			if (! Directory.Exists(directory))
			{
				throw new DirectoryNotFoundException($"Directory not found: {directory}");
			}

			if (directory.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				return (IRasterWorkspace2) OpenFileGdbWorkspace(directory);
			}

			var rasterWsFactory = (IWorkspaceFactory2) GetRasterWorkspaceFactory();

			if (! rasterWsFactory.IsWorkspace(directory))
			{
				_msg.DebugFormat("Not (yet) a raster workspace directory: {0}",
				                 directory);
			}

			return (IRasterWorkspace2) rasterWsFactory.OpenFromFile(directory, 0);
		}

		[NotNull]
		public static ITinWorkspace OpenTinWorkspace([NotNull] string directory)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));

			if (! Directory.Exists(directory))
			{
				throw new DirectoryNotFoundException($"Directory not found: {directory}");
			}

			var tinWsFactory = (IWorkspaceFactory2) GetTinWorkspaceFactory();
			if (! tinWsFactory.IsWorkspace(directory))
			{
				throw new ArgumentException("Not a Tin workspace directory: " +
				                            directory);
			}

			return (ITinWorkspace) tinWsFactory.OpenFromFile(directory, 0);
		}

		[NotNull]
		public static IVersion OpenVersion([NotNull] IWorkspace workspace,
		                                   [NotNull] string versionName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(versionName, nameof(versionName));

			var versionedWorkspace = (IVersionedWorkspace) workspace;

			return Assert.NotNull(versionedWorkspace.FindVersion(versionName),
			                      "version not found: {0}", versionName);
		}

		[NotNull]
		public static IVersion OpenVersion([NotNull] IFeatureWorkspace workspace,
		                                   [NotNull] string versionName)
		{
			return OpenVersion((IWorkspace) workspace, versionName);
		}

		[NotNull]
		public static IWorkspace OpenWorkspaceVersion([NotNull] IWorkspace workspace,
		                                              [NotNull] string versionName)
		{
			return (IWorkspace) OpenVersion(workspace, versionName);
		}

		/// <summary>
		/// Opens the workspace connection for the default version of a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns>Workspace connection to the default version. If the given workspace is 
		/// not versioned, it is returned directly.</returns>
		[NotNull]
		public static IWorkspace OpenWorkspaceDefaultVersion(
			[NotNull] IWorkspace workspace)
		{
			var versionedWorkspace = workspace as IVersionedWorkspace;

			if (versionedWorkspace != null)
			{
				return (IWorkspace) versionedWorkspace.DefaultVersion;
			}

			return workspace;
		}

		[NotNull]
		public static IWorkspace OpenWorkspace([NotNull] IWorkspaceName workspaceName)
		{
			Assert.ArgumentNotNull(workspaceName, nameof(workspaceName));

			try
			{
				var workspace = (IWorkspace) ((IName) workspaceName).Open();

				return Assert.NotNull(workspace, "workspace is null");
			}
			catch (Exception e)
			{
				_msg.Debug(
					string.Format(
						"Error opening workspace based on workspace name with connection string {0}",
						GetConnectionString(workspaceName, true)), e);

				throw;
			}
		}

		[NotNull]
		public static IFeatureWorkspace OpenFeatureWorkspaceVersion(
			[NotNull] IWorkspace workspace,
			[NotNull] string versionName)
		{
			return (IFeatureWorkspace) OpenVersion(workspace, versionName);
		}

		[NotNull]
		public static IFeatureWorkspace OpenFeatureWorkspaceVersion(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string versionName)
		{
			return (IFeatureWorkspace) OpenVersion((IWorkspace) workspace,
			                                       versionName);
		}

		/// <summary>
		/// Returns the reconcile version after a reconcile operation. Should
		/// be called after a reconcile was performed in the current edit
		/// session.
		/// </summary>
		/// <param name="editWorkspace"></param>
		/// <returns></returns>
		[NotNull]
		public static IFeatureWorkspace GetReconcileVersion(
			[NotNull] IFeatureWorkspace editWorkspace)
		{
			Assert.ArgumentNotNull(editWorkspace, nameof(editWorkspace));

			var editVersion = (IVersionEdit) editWorkspace;
			var reconcileVersion = (IFeatureWorkspace) editVersion.ReconcileVersion;

			Assert.NotNull(reconcileVersion,
			               "The reconcile version is null. Reconcile must be called first.");
			return reconcileVersion;
		}

		/// <summary>
		/// Returns the pre-reconcile version after a reconcile operation. Should
		/// be called after a reconcile was performed in the current edit
		/// session.
		/// </summary>
		/// <param name="editWorkspace"></param>
		/// <returns></returns>
		[NotNull]
		public static IFeatureWorkspace GetPreReconcileVersion(
			[NotNull] IFeatureWorkspace editWorkspace)
		{
			Assert.ArgumentNotNull(editWorkspace, nameof(editWorkspace));

			var editVersion = (IVersionEdit) editWorkspace;
			var preReconcileVersion =
				(IFeatureWorkspace) editVersion.PreReconcileVersion;

			Assert.NotNull(preReconcileVersion,
			               "The pre-reconcile version is null. Reconcile must be called first.");
			return preReconcileVersion;
		}

		/// <summary>
		/// Returns the common ancestor version after a reconcile operation. Should
		/// be called after a reconcile was performed in the current edit
		/// session otherwise null is returned. This is not the same as the workspace returned by 
		/// IVersion2.GetCommonAncestor.
		/// </summary>
		/// <param name="editWorkspace"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IFeatureWorkspace GetAncestorVersion(
			[NotNull] IFeatureWorkspace editWorkspace)
		{
			Assert.ArgumentNotNull(editWorkspace, nameof(editWorkspace));

			var editVersion = (IVersionEdit) editWorkspace;
			return (IFeatureWorkspace) editVersion.CommonAncestorVersion;
		}

		/// <summary>
		/// Gets the parent version count for a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns>The number of parent versions of this workspace. 0 if the workspace is not versioned.</returns>
		public static int GetParentVersionCount([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var version = workspace as IVersion;
			return version == null
				       ? 0
				       : GetParentVersionCount(version.VersionInfo);
		}

		/// <summary>
		/// Gets the parent version count for a given version.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>The number of parent versions of this version. </returns>
		public static int GetParentVersionCount([NotNull] IVersion version)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			return GetParentVersionCount(version.VersionInfo);
		}

		// TODO call from DatasetQueryMonitor (instead of GetAncestorCount)
		/// <summary>
		/// Gets the parent version count for a given version info.
		/// </summary>
		/// <param name="versionInfo">The version info.</param>
		/// <returns>The number of parent versions.</returns>
		public static int GetParentVersionCount([NotNull] IVersionInfo versionInfo)
		{
			Assert.ArgumentNotNull(versionInfo, nameof(versionInfo));

			IEnumVersionInfo ancestors = versionInfo.Ancestors;
			ancestors.Reset();

			IVersionInfo ancestor = ancestors.Next();
			var count = 0;
			while (ancestor != null)
			{
				count++;
				ancestor = ancestors.Next();
			}

			return count;
		}

		[CanBeNull]
		public static IVersionInfo GetVersionInfo([NotNull] IFeatureWorkspace workspace,
		                                          [NotNull] string qualifiedVersionName)
		{
			return GetVersionInfo((IWorkspace) workspace, qualifiedVersionName);
		}

		[CanBeNull]
		public static IVersionInfo GetVersionInfo([NotNull] IWorkspace workspace,
		                                          [NotNull] string qualifiedVersionName)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(qualifiedVersionName,
			                              nameof(qualifiedVersionName));

			var versionedWorkspace = workspace as IVersionedWorkspace;

			if (versionedWorkspace == null)
			{
				return null;
			}

			IEnumVersionInfo versionInfos = versionedWorkspace.Versions;
			versionInfos.Reset();

			try
			{
				IVersionInfo versionInfo;
				while ((versionInfo = versionInfos.Next()) != null)
				{
					if (versionInfo.VersionName.Equals(qualifiedVersionName,
					                                   StringComparison
						                                   .OrdinalIgnoreCase))
					{
						return versionInfo;
					}
				}
			}
			finally
			{
				versionInfos.Reset();
			}

			return null;
		}

		[NotNull]
		public static string GetDefaultVersionName(
			[NotNull] IVersionedWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			return workspace.DefaultVersion.VersionName;
		}

		[NotNull]
		public static string GetSdeRepositoryOwner(
			[NotNull] IVersionedWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			return GetVersionOwnerName(GetDefaultVersionName(workspace));
		}

		/// <summary>
		/// Gets the version info descriptors for a given workspace, which match a given predicate.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="match">The match predicate.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IVersionInfo> GetVersionInfos([NotNull] IWorkspace workspace,
		                                                  [CanBeNull] Predicate<IVersionInfo> match)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var versionedWorkspace = workspace as IVersionedWorkspace;

			if (versionedWorkspace == null)
			{
				return new List<IVersionInfo>();
			}

			IEnumVersionInfo versionInfos = versionedWorkspace.Versions;
			versionInfos.Reset();

			var list = new List<IVersionInfo>();

			IVersionInfo versionInfo;
			while ((versionInfo = versionInfos.Next()) != null)
			{
				if (match == null || match(versionInfo))
				{
					list.Add(versionInfo);
				}
			}

			versionInfos.Reset();

			return list;
		}

		public static bool HasChildVersions([NotNull] IVersion parentVersion)
		{
			return GetChildVersionInfos(parentVersion).Any();
		}

		[NotNull]
		public static IEnumerable<IVersionInfo> GetChildVersionInfos(
			[NotNull] IVersion parentVersion)
		{
			Assert.ArgumentNotNull(parentVersion, nameof(parentVersion));

			return GetChildVersionInfos(parentVersion.VersionInfo);
		}

		[NotNull]
		public static IEnumerable<IVersionInfo> GetChildVersionInfos(
			[NotNull] IVersionInfo parentVersionInfo)
		{
			Assert.ArgumentNotNull(parentVersionInfo, nameof(parentVersionInfo));

			IEnumVersionInfo versionInfos = parentVersionInfo.Children;

			try
			{
				versionInfos.Reset();

				IVersionInfo child;

				while ((child = versionInfos.Next()) != null)
				{
					yield return child;
				}
			}
			finally
			{
				Marshal.ReleaseComObject(versionInfos);
			}
		}

		/// <summary>
		/// Determines whether there exists geodatabase release information for the specified workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="gdbRelease">The geodatabase release information.</param>
		/// <returns><c>true</c> if geodatabase information is available, <c>false</c> otherwise</returns>
		[ContractAnnotation("=>true, gdbRelease:notnull; =>false, gdbRelease:canbenull")]
		public static bool HasGeodatabaseReleaseInformation(
			[NotNull] IWorkspace workspace,
			out IGeodatabaseRelease gdbRelease)
		{
			gdbRelease = workspace as IGeodatabaseRelease;

			if (gdbRelease == null)
			{
				return false;
			}

			try
			{
				bool dummy = gdbRelease.CurrentRelease;
				if (dummy) { } // to avoid compiler warning

				return true;
			}
			catch (NotImplementedException)
			{
				// for non-sde sql workspaces ("query layers")
				return false;
			}
		}

		public static bool IsOleDbWorkspace([NotNull] IWorkspace workspace)
		{
			if (! Marshal.IsComObject(workspace))
			{
				// Specific implementation
				return false;
			}

			const string classId = "{59158055-3171-11D2-AA94-00C04FA37849}";

			return workspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace &&
			       classId.Equals(GetWorkspaceFactoryClassID(workspace),
			                      StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsFileGeodatabase([NotNull] IWorkspace workspace)
		{
			string workspacePath = GetWorkspacePath(workspace);

			return workspacePath != null &&
			       workspacePath.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase);

			// Original implementation which fails for GdbWorkspace implementations:
			//const string fgdbClassId = "{71FE75F0-EA0C-4406-873E-B7D53748AE7E}";

			//return workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace &&
			//       fgdbClassId.Equals(GetWorkspaceFactoryClassID(workspace),
			//                          StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsPersonalGeodatabase([NotNull] IWorkspace workspace)
		{
			string workspacePath = GetWorkspacePath(workspace);

			return workspacePath != null &&
			       workspacePath.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase);

			// Original implementation which fails for GdbWorkspace implementations:

			//Assert.ArgumentNotNull(workspace, nameof(workspace));

			//const string pgdbClassId = "{DD48C96A-D92A-11D1-AA81-00C04FA33A15}";

			//return workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace &&
			//       pgdbClassId.Equals(GetWorkspaceFactoryClassID(workspace),
			//                          StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsMobileGeodatabase([NotNull] IWorkspace workspace)
		{
			string workspacePath = GetWorkspacePath(workspace);

			return workspacePath != null &&
			       workspacePath.EndsWith(".geodatabase", StringComparison.OrdinalIgnoreCase);

			// Original implementation which fails for GdbWorkspace implementations:

			//Assert.ArgumentNotNull(workspace, nameof(workspace));

			//const string mobileGdbClassId = "{DEB394DD-6F72-4C2C-AB4D-4C4E04CBBF9F}";

			//return workspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace &&
			//       mobileGdbClassId.Equals(GetWorkspaceFactoryClassID(workspace),
			//                               StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether the specified workspace is an SDE workspace.
		/// DO NOT USE with custom workspace implementations.
		/// </summary>
		/// <param name="workspace"></param>
		/// <returns></returns>
		public static bool IsSDEGeodatabase([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			const string sdegdbClassId = "{D9B4FA40-D6D9-11D1-AA81-00C04FA33A15}";

			return workspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace &&
			       sdegdbClassId.Equals(GetWorkspaceFactoryClassID(workspace),
			                            StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsShapefileWorkspace([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			const string classId = "{A06ADB96-D95C-11D1-AA81-00C04FA33A15}";

			return workspace.Type == esriWorkspaceType.esriFileSystemWorkspace &&
			       classId.Equals(GetWorkspaceFactoryClassID(workspace),
			                      StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsInMemoryWorkspace([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var workspaceName = (IWorkspaceName) GetWorkspaceName(workspace);

			return IsInMemoryWorkspace(workspaceName);
		}

		public static bool IsInMemoryWorkspace([NotNull] IWorkspaceName workspaceName)
		{
			if (workspaceName.WorkspaceFactory is InMemoryWorkspaceFactoryClass)
			{
				return true;
			}

			int intWorkspaceType =
				(int) (workspaceName.WorkspaceFactory?.WorkspaceType ?? workspaceName.Type);

			const int workspaceTypeInMemory = 99;

			return intWorkspaceType == workspaceTypeInMemory;
		}

		/// <summary>
		/// Gets a displayable text describing a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetWorkspaceDisplayText([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			const string nullPathText = "<undefined path>";

			switch (workspace.Type)
			{
				case esriWorkspaceType.esriFileSystemWorkspace:
					return workspace.PathName ?? nullPathText;

				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					return workspace.PathName ?? nullPathText;

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:

					const string nullVersionText = "<undefined version>";

					var version = workspace as IVersion;

					string versionName = version != null &&
					                     ! string.IsNullOrEmpty(version.VersionName)
						                     ? version.VersionName
						                     : nullVersionText;

					var connectionInfo = (IDatabaseConnectionInfo2) workspace;

					// TODO use IDatabaseConnectionInfo3.ConnectedDatabaseEx if > 10.0
					// --> returns TNS database name for oracle

					string databaseName =
						connectionInfo.ConnectedDatabase; // not defined for oracle
					string machineName = connectionInfo.ConnectionServer;

					return string.IsNullOrEmpty(databaseName)
						       ? string.Format("{0} - {1}", machineName, versionName)
						       : string.Format("{0} ({1}) - {2}", databaseName,
						                       machineName, versionName);

				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Unsupported workspace type: {0}",
						              workspace.Type));
			}
		}

		/// <summary>
		/// Determines whether a given workspace is a read-only version.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns>
		///   <c>true</c> if the workspace points to a version that is read-only for the 
		///   connected user; otherwise (either the workspace is not versioned, or the user
		///   has write access to the version), <c>false</c>.
		/// </returns>
		public static bool IsReadOnlyVersion([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			var version = workspace as IVersion;

			return version != null && IsReadOnlyVersion(version);
		}

		/// <summary>
		/// Determines whether a given version is read-only for the connected user.
		/// </summary>
		/// <param name="version">The version.</param>
		/// <returns>
		///   <c>true</c> if the version is read-only for the connected user; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsReadOnlyVersion([NotNull] IVersion version)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			IVersionInfo versionInfo = version.VersionInfo;

			return versionInfo.Access != esriVersionAccess.esriVersionAccessPublic &&
			       ! versionInfo.IsOwner();
		}

		/// <summary>
		/// Gets all configuration keywords for a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IConfigurationKeyword> GetConfigurationKeywords(
			[NotNull] IWorkspace workspace)
		{
			return GetConfigurationKeywords(workspace, keyword => keyword, null);
		}

		/// <summary>
		/// Gets the configuration keywords which match a given predicate, for a given workspace.
		/// </summary>
		/// <param name="workspace">The workspace.</param>
		/// <param name="match">The match predicate.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IConfigurationKeyword> GetConfigurationKeywords(
			[NotNull] IWorkspace workspace,
			[CanBeNull] Predicate<IConfigurationKeyword> match)
		{
			return GetConfigurationKeywords(workspace, keyword => keyword, match);
		}

		/// <summary>
		/// Gets the configuration keywords which match a given predicate, for a given workspace,
		/// and returns an enumeration of transformed results.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="workspace">The workspace.</param>
		/// <param name="getResult">The function for getting a result element based on a keyword.</param>
		/// <param name="match">The match predicate.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<T> GetConfigurationKeywords<T>(
			[NotNull] IWorkspace workspace,
			[NotNull] Func<IConfigurationKeyword, T> getResult,
			[CanBeNull] Predicate<IConfigurationKeyword> match)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(getResult, nameof(getResult));

			var workspaceConfiguration = workspace as IWorkspaceConfiguration;

			if (workspaceConfiguration == null)
			{
				yield break;
			}

			IEnumConfigurationKeyword keywords =
				workspaceConfiguration.ConfigurationKeywords;
			keywords.Reset();

			IConfigurationKeyword keyword;
			while ((keyword = keywords.Next()) != null)
			{
				if (match == null || match(keyword))
				{
					yield return getResult(keyword);
				}
			}
		}

		[CanBeNull]
		public static IVersion GetParentVersion([NotNull] IVersion version)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			if (! version.HasParent())
			{
				return null;
			}

			var workspace = (IFeatureWorkspace) version;

			IVersionInfo parentVersionInfo = version.VersionInfo.Parent;

			return OpenVersion(workspace, parentVersionInfo.VersionName);
		}

		public static bool UsesQualifiedDatasetNames([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (! IsOleDbWorkspace(workspace))
			{
				return IsMobileGeodatabase(workspace) ||
				       workspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace;
			}

			// try to determine if workspace content uses qualified dataset names

			// NOTE: property esriWorkspacePropSupportsQualifiedNames on IWorkspaceProperties always returns False
			//       for OLE DB workspaces (observed at 10.2) --> don't rely on it, look at one of the datasets

			IEnumDatasetName datasetNames = DatasetUtils.GetRootDatasetNames(
				workspace, esriDatasetType.esriDTAny);
			if (datasetNames == null)
			{
				return true; // no result, assume "qualified"
			}

			try
			{
				datasetNames.Reset();
				IDatasetName firstDatasetName = datasetNames.Next();
				if (firstDatasetName == null)
				{
					return true; // no datasets, assume "qualified"
				}

				string owner = DatasetUtils.GetOwnerName(firstDatasetName);

				return StringUtils.IsNotEmpty(owner);
			}
			finally
			{
				datasetNames.Reset();
			}
		}

		public static WorkspaceDbType GetWorkspaceDbType(IWorkspace workspace)
		{
			switch (workspace.Type)
			{
				case esriWorkspaceType.esriFileSystemWorkspace:
					return WorkspaceDbType.FileSystem;

				case esriWorkspaceType.esriLocalDatabaseWorkspace:
					if (IsFileGeodatabase(workspace))
					{
						return WorkspaceDbType.FileGeodatabase;
					}

					if (IsPersonalGeodatabase(workspace))
					{
						return WorkspaceDbType.PersonalGeodatabase;
					}

					if (IsMobileGeodatabase(workspace))
					{
						return WorkspaceDbType.MobileGeodatabase;
					}

					break;

				case esriWorkspaceType.esriRemoteDatabaseWorkspace:

					var connectionInfo = workspace as IDatabaseConnectionInfo2;
					if (connectionInfo != null)
					{
						switch (connectionInfo.ConnectionDBMS)
						{
							case esriConnectionDBMS.esriDBMS_Unknown:
								break;

							case esriConnectionDBMS.esriDBMS_Oracle:
								return WorkspaceDbType.ArcSDEOracle;

							case esriConnectionDBMS.esriDBMS_Informix:
								return WorkspaceDbType.ArcSDEInformix;

							case esriConnectionDBMS.esriDBMS_SQLServer:
								return WorkspaceDbType.ArcSDESqlServer;

							case esriConnectionDBMS.esriDBMS_DB2:
								return WorkspaceDbType.ArcSDEDB2;

							case esriConnectionDBMS.esriDBMS_PostgreSQL:
								return WorkspaceDbType.ArcSDEPostgreSQL;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
					{
						return WorkspaceDbType.ArcSDE;
					}

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return WorkspaceDbType.Unknown;
		}

		public static esriWorkspaceType ToEsriWorkspaceType(WorkspaceDbType dbType)
		{
			switch (dbType)
			{
				case WorkspaceDbType.FileGeodatabase:
				case WorkspaceDbType.PersonalGeodatabase:
				case WorkspaceDbType.MobileGeodatabase:
					return esriWorkspaceType.esriLocalDatabaseWorkspace;
				case WorkspaceDbType.ArcSDE:
				case WorkspaceDbType.ArcSDESqlServer:
				case WorkspaceDbType.ArcSDEOracle:
				case WorkspaceDbType.ArcSDEPostgreSQL:
				case WorkspaceDbType.ArcSDEInformix:
				case WorkspaceDbType.ArcSDEDB2:
					return esriWorkspaceType.esriRemoteDatabaseWorkspace;
				default:
					throw new ArgumentOutOfRangeException(nameof(dbType), dbType,
					                                      "Unknown DB type");
			}
		}

		#region Non-public methods

		private static int GetPasswordKeywordIndex([NotNull] string connectionString,
		                                           out string passwordKeyword)
		{
			foreach (string keyword in GetPasswordKeywords())
			{
				int index = connectionString.IndexOf(keyword, 0,
				                                     StringComparison.OrdinalIgnoreCase);

				if (index < 0)
				{
					continue;
				}

				passwordKeyword = keyword;
				return index;
			}

			passwordKeyword = null;
			return -1;
		}

		private static bool IsPasswordPropertyName([NotNull] string propertyName)
		{
			return GetPasswordKeywords().Any(
				keyword =>
					string.Equals(propertyName, keyword,
					              StringComparison.OrdinalIgnoreCase));
		}

		[NotNull]
		private static IEnumerable<string> GetPasswordKeywords()
		{
			yield return "PASSWORD";
			yield return "ENCRYPTED_PASSWORD";
			yield return "ENCRYPTED_PASSWORD_UTF8";
		}

		[NotNull]
		private static IWorkspace OpenWorkspace(
			[NotNull] IWorkspaceFactory2 workspaceFactory,
			[NotNull] string connectionString)
		{
			Assert.ArgumentNotNull(workspaceFactory, nameof(workspaceFactory));
			Assert.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));

			try
			{
				return workspaceFactory.OpenFromString(connectionString, 0);
			}
			catch (Exception e)
			{
				_msg.DebugFormat(
					"Error opening workspace ({0}) from connection string {1}: {2}",
					workspaceFactory.WorkspaceType, connectionString, e.Message);
				throw;
			}
		}

		[NotNull]
		private static string GetWorkspaceFactoryClassID([NotNull] IWorkspace workspace)
		{
			return workspace.WorkspaceFactory.GetClassID().Value.ToString();
		}

		//private static string GetPasswordStringOra(string dbName, string password)
		//{
		//    // compose the password string ("<pwd>@<dbname>")

		//    return string.Format("{0}@{1}", password, dbName);
		//}

		//private static string GetSdeInstanceOra(DirectConnectDriver driver,
		//                                        string repositoryName)
		//{
		//    return string.Format(@"{0}:/:{1}",
		//                         GetSdeInstance(driver),
		//                         repositoryName);
		//}

		[NotNull]
		private static string GetSdeInstancePostgreSQL(DirectConnectDriver driver,
		                                               [NotNull] string serverName)
		{
			return string.Format(@"{0}:{1}", GetSdeInstance(driver), serverName);
		}

		[NotNull]
		private static string GetSdeInstanceSqlServer(DirectConnectDriver driver,
		                                              [NotNull] string serverName)
		{
			return string.Format(@"{0}:{1}", GetSdeInstance(driver), serverName);
		}

		[NotNull]
		private static string GetSdeInstanceOra(DirectConnectDriver driver,
		                                        [NotNull] string oracleDbName,
		                                        [NotNull] string repositoryName)
		{
			return string.Format(@"{0}:{1}:{2}",
			                     GetSdeInstance(driver), oracleDbName, repositoryName);
		}

		[NotNull]
		private static string GetSdeInstance(DirectConnectDriver driver)
		{
			switch (driver)
			{
				case DirectConnectDriver.Oracle9i:
					return "sde:oracle9i";

				case DirectConnectDriver.Oracle10g:
					return "sde:oracle10g";

				case DirectConnectDriver.Oracle:
				case DirectConnectDriver.Oracle11g:
					return "sde:oracle11g";

				case DirectConnectDriver.SqlServer:
					return "sde:sqlserver";

				case DirectConnectDriver.PostgreSQL:
					return "sde:postgresql";

				default:
					throw new ArgumentException(@"unknown driver: " + driver,
					                            nameof(driver));
			}
		}

		[NotNull]
		public static IWorkspaceFactory GetOleDbWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesOleDB.OLEDBWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetFileGdbWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesGDB.FileGDBWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetAccessWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesGDB.AccessWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetSqliteWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesGDB.SqliteWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetTinWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesFile.TinWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetRasterWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesRaster.RasterWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetSdeWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesGDB.SdeWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetShapefileWorkspaceFactory()
		{
			return GetWorkspaceFactory("esriDataSourcesFile.ShapefileWorkspaceFactory");
		}

		[NotNull]
		public static IWorkspaceFactory GetWorkspaceFactory(
			[NotNull] string factoryProgId)
		{
			return ComUtils.CreateObject<IWorkspaceFactory>(factoryProgId);
		}

		[NotNull]
		public static IWorkspace OpenWorkspace(
			[NotNull] IWorkspaceFactory workspaceFactory,
			[NotNull] IPropertySet propertySet,
			int hWnd)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Opening workspace with {0}",
				                 WorkspaceConnectionPropertiesToString(propertySet));
			}

			try
			{
				return workspaceFactory.Open(propertySet, hWnd);
			}
			catch (Exception e)
			{
				try
				{
					_msg.DebugFormat("Error opening workspace with {0}: {1}",
					                 WorkspaceConnectionPropertiesToString(propertySet),
					                 e.Message);
				}
				catch (Exception e1)
				{
					_msg.ErrorFormat("Error writing property set to string: {0}",
					                 e1.Message);
				}

				throw;
			}
		}

		[NotNull]
		private static string GetDefaultVersionName([NotNull] string repositoryName)
		{
			return string.Format("{0}.DEFAULT", repositoryName);
		}

		[NotNull]
		private static string[] GetVersionNameParts([NotNull] string qualifiedVersionName)
		{
			Assert.ArgumentNotNull(qualifiedVersionName, nameof(qualifiedVersionName));

			const char separator = '.';

			return qualifiedVersionName.Split(separator);
		}

		[CanBeNull]
		private static IWorkspaceFactorySchemaCache GetSdeFactoryCache()
		{
			try
			{
				IWorkspaceFactory factory = GetSdeWorkspaceFactory();

				return (IWorkspaceFactorySchemaCache) factory;
			}
			catch (Exception e)
			{
				_msg.Warn("Unable to get workspace factory cache", e);
				return null;
			}
		}

		private static bool DeleteVersionTree([NotNull] IVersionedWorkspace workspace,
		                                      [NotNull] IVersionInfo versionInfo)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(versionInfo, nameof(versionInfo));

			var allDeleted = true;

			ICollection<IVersionInfo> children = GetChildVersionInfoList(versionInfo);

			foreach (IVersionInfo child in children)
			{
				bool deleted = DeleteVersionTree(workspace, child);

				if (! deleted)
				{
					allDeleted = false;
				}
			}

			if (allDeleted)
			{
				// delete this version (if not the root version)
				if (versionInfo.Parent != null)
				{
					DeleteVersion((IWorkspace) workspace, versionInfo.VersionName);
				}
				else
				{
					allDeleted = false;
				}
			}

			return allDeleted;
		}

		[NotNull]
		// ReSharper disable once ReturnTypeCanBeEnumerable.Local
		private static List<IVersionInfo> GetChildVersionInfoList(
			[NotNull] IVersionInfo versionInfo)
		{
			Assert.ArgumentNotNull(versionInfo, nameof(versionInfo));

			IEnumVersionInfo childrenEnum = versionInfo.Children;
			try
			{
				childrenEnum.Reset();
				IVersionInfo childVersionInfo;

				var result = new List<IVersionInfo>();
				while ((childVersionInfo = childrenEnum.Next()) != null)
				{
					result.Add(childVersionInfo);
				}

				return result;
			}
			finally
			{
				Marshal.ReleaseComObject(childrenEnum);
			}
		}

		/// <summary>
		/// Gets the workspace name corresponding to the default version based on a given workspace name.
		/// </summary>
		/// <param name="wsName">The workspace name.</param>
		/// <returns></returns>
		[NotNull]
		private static IWorkspaceName GetWorkspaceNameForDefault(
			[NotNull] IWorkspaceName wsName)
		{
			Assert.ArgumentNotNull(wsName, nameof(wsName));

			IWorkspaceName result = new WorkspaceNameClass();

			result.WorkspaceFactoryProgID = wsName.WorkspaceFactoryProgID;
			result.BrowseName = wsName.BrowseName;
			result.PathName = wsName.PathName;
			// wsNameClone.ConnectionString = wsName.ConnectionString;

			IPropertySet connectionProps = wsName.ConnectionProperties;

			const string propNameVersion = "VERSION";

			if (PropertySetUtils.HasProperty(connectionProps, propNameVersion))
			{
				connectionProps.RemoveProperty(propNameVersion);
			}

			result.ConnectionProperties = connectionProps;

			return result;
		}

		private static void DeleteVersionInSeparateThread([NotNull] IVersion version)
		{
			Assert.ArgumentNotNull(version, nameof(version));

			GC.Collect();
			GC.WaitForPendingFinalizers();

			var deleter = new VersionDeleter(version);

			var thread = new Thread(deleter.DeleteVersion);

			thread.SetApartmentState(ApartmentState.STA);

			thread.Start();
			thread.Join();

			if (deleter.LastException != null)
			{
				// do not wrap to allow error filtering by caller (VERSION_DOES_NOT_EXIST)
				_msg.DebugFormat("Error deleting version {0}: {1}", version.VersionName,
				                 deleter.LastException);

				throw deleter.LastException;
			}
		}

		[CanBeNull]
		private static string GetWorkspacePath([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			if (string.IsNullOrEmpty(workspace.PathName))
			{
				return null;
			}

			return workspace.PathName;
		}

		#endregion

		#region Nested types

		private class VersionDeleter
		{
			[NotNull] private readonly string[] _connectionProps;
			[NotNull] private readonly object[] _connectionValues;
			[NotNull] private readonly string _versionName;

			/// <summary>
			/// Initializes a new instance of the <see cref="VersionDeleter"/> class.
			/// </summary>
			/// <param name="version">Version to be deleted.</param>
			public VersionDeleter([NotNull] IVersion version)
			{
				Assert.ArgumentNotNull(version, nameof(version));

				_versionName = Assert.NotNull(version.VersionName);
				var ws = (IWorkspace) version;
				IPropertySet propSet = ws.ConnectionProperties;
				int propertyCount = propSet.Count;
				object oNames;
				object oValues;
				propSet.GetAllProperties(out oNames, out oValues);
				var names = (IList<object>) oNames;
				var values = (IList<object>) oValues;

				_connectionProps = new string[propertyCount];
				_connectionValues = new object[propertyCount];

				for (var propertyIndex = 0;
				     propertyIndex < propertyCount;
				     propertyIndex++)
				{
					_connectionProps[propertyIndex] = (string) names[propertyIndex];
					_connectionValues[propertyIndex] = values[propertyIndex];
				}
			}

			[CanBeNull]
			public Exception LastException { get; private set; }

			/// <summary>
			/// Deletes the version.
			/// </summary>
			public void DeleteVersion()
			{
				LastException = null;
				try
				{
					IPropertySet connectionProperties = new PropertySetClass();

					int propertyCount = _connectionProps.Length;

					for (var propertyIndex = 0;
					     propertyIndex < propertyCount;
					     propertyIndex++)
					{
						connectionProperties.SetProperty(
							_connectionProps[propertyIndex],
							_connectionValues[propertyIndex]);
					}

					IWorkspaceFactory wsFactory = GetSdeWorkspaceFactory();
					var version = (IVersion) wsFactory.Open(connectionProperties, 0);

					Assert.AreEqual(version.VersionName, _versionName,
					                "Unexpected version");

					DeleteVersionCore(wsFactory, connectionProperties, version);
				}
				catch (Exception e)
				{
					LastException = e;
				}
			}

			private static void DeleteVersionCore([NotNull] IWorkspaceFactory wsFactory,
			                                      [NotNull] IPropertySet connectionProperties,
			                                      [NotNull] IVersion version)
			{
				// workaround, to allow deletion of version: Open default version
				var versionedWorkspace = (IVersionedWorkspace) version;

				string defaultVersion = versionedWorkspace.DefaultVersion.VersionName;
				connectionProperties.SetProperty("Version", defaultVersion);
				IWorkspace defaultWorkspace = wsFactory.Open(connectionProperties, 0);
				// end workaround

				version.Delete();

				Marshal.ReleaseComObject(version);
				Marshal.ReleaseComObject(defaultWorkspace);
			}
		}

		#endregion
	}
}
