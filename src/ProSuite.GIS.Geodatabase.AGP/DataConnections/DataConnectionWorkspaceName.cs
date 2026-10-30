using System;
using System.Collections.Generic;
using System.IO;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP.DataConnections;

public class DataConnectionWorkspaceName : IWorkspaceName
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull]
	public DataConnectionWorkspaceName FromDataConnection(
		[NotNull] CIMStandardDataConnection dataConnection)
	{
		Assert.NotNull(dataConnection, nameof(dataConnection));

		return new DataConnectionWorkspaceName(
			dataConnection.WorkspaceConnectionString,
			dataConnection.WorkspaceFactory);
	}

	public static DataConnectionWorkspaceName FromDataConnection(
		[NotNull] CIMFeatureDatasetDataConnection featureDataConnection)
	{
		Assert.NotNull(featureDataConnection, nameof(featureDataConnection));

		return new DataConnectionWorkspaceName(
			featureDataConnection.WorkspaceConnectionString,
			featureDataConnection.WorkspaceFactory);
	}

	[CanBeNull]
	public static DataConnectionWorkspaceName FromDataConnection(
		[NotNull] CIMDataConnection dataConnection)
	{
		switch (dataConnection)
		{
			case CIMFeatureDatasetDataConnection featureDatasetDataConnection:
				return FromDataConnection(featureDatasetDataConnection);
			case CIMStandardDataConnection standardDataConnection:
				return FromDataConnection(standardDataConnection);
			default:
				throw new ArgumentOutOfRangeException(nameof(dataConnection));
		}
	}

	[CanBeNull]
	public static DataConnectionWorkspaceName FromGeodatabase(
		[NotNull] Datastore datastore)
	{
		Connector connector = datastore.GetConnector();

		if (! WorkspaceUtils.TryGetWorkspaceFactory(
			    connector, out WorkspaceFactory workspaceFactory))
		{
			_msg.Debug($"No workspace factory for connector type {connector.GetType()} " +
			           $"({WorkspaceUtils.GetDatastoreDisplayText(connector)})");

			return null;
		}

		return new DataConnectionWorkspaceName(datastore.GetConnectionString(),
		                                       workspaceFactory)
		       {
			       ConnectionFilePath = TryGetConnectionFilePath(datastore)
		       };
	}

	/// <summary>
	/// The original catalog path (e.g. an .sde connection file) the datastore was opened
	/// from, if any. Preferred over <see cref="ConnectionString"/> when re-opening, because
	/// it preserves the authentication mode (e.g. OSA) and credentials. The connection string
	/// round-tripped from a live enterprise geodatabase can otherwise fail with
	/// "invalid login account" on a fresh connection.
	/// </summary>
	[CanBeNull]
	public string ConnectionFilePath { get; set; }

	[CanBeNull]
	private static string TryGetConnectionFilePath([NotNull] Datastore datastore)
	{
		try
		{
			string localPath = WorkspaceUtils.GetDatastorePath(datastore)?.LocalPath;

			return ! string.IsNullOrEmpty(localPath) &&
			       localPath.EndsWith(".sde", StringComparison.OrdinalIgnoreCase)
				       ? localPath
				       : null;
		}
		catch (Exception e)
		{
			_msg.Debug("Could not determine catalog path for datastore", e);
			return null;
		}
	}

	public DataConnectionWorkspaceName(CIMStandardDataConnection standardConnection)
		: this(standardConnection.WorkspaceConnectionString,
		       standardConnection.WorkspaceFactory) { }

	public DataConnectionWorkspaceName(string connectionString,
	                                   WorkspaceFactory workspaceFactoryType)
	{
		// TODO: What is a good name string, PathName, etc.
		//       Also, consider cleaning up the interface (Category?)
		NameString = connectionString;
		ConnectionString = connectionString;
		FactoryType = workspaceFactoryType;
	}

	public WorkspaceFactory FactoryType { get; }

	#region Implementation of IName

	public string NameString { get; set; }

	/// <summary>
	/// The versioned-database version to open, regardless of the one baked into the connection
	/// file / string. Set it when the application works in a different version than the one
	/// captured when this name was persisted, which would otherwise keep being read. Null keeps
	/// whatever the connection carries.
	/// </summary>
	[CanBeNull]
	public string VersionOverride { get; set; }

	public Datastore OpenDatastore()
	{
		// Prefer the original .sde connection file: it preserves the authentication mode
		// (e.g. OSA) and credentials. Re-opening from the round-tripped connection string can
		// fail with "invalid login account" (stale/mismatched DBMS credentials).
		if (! string.IsNullOrEmpty(ConnectionFilePath) && File.Exists(ConnectionFilePath))
		{
			_msg.DebugFormat("Opening datastore from connection file {0}", ConnectionFilePath);
			return ApplyVersionOverride(WorkspaceUtils.OpenGeodatabase(ConnectionFilePath));
		}

		_msg.DebugFormat(
			"No usable connection file (path='{0}'); opening from connection string",
			ConnectionFilePath);

		Connector connector = WorkspaceUtils.CreateConnector(FactoryType, ConnectionString);

		return ApplyVersionOverride(WorkspaceUtils.OpenDatastore(connector));
	}

	/// <summary>
	/// Re-opens <paramref name="datastore"/> in <see cref="VersionOverride"/> unless it is already
	/// there. The new connector is derived from the opened datastore's own one, so authentication
	/// mode and credentials are carried over; rewriting <see cref="ConnectionString"/> instead
	/// (see <see cref="ChangeVersion"/>) would have no effect at all whenever the connection file
	/// wins above.
	/// </summary>
	[NotNull]
	private Datastore ApplyVersionOverride([NotNull] Datastore datastore)
	{
		if (string.IsNullOrEmpty(VersionOverride) ||
		    datastore is not ArcGIS.Core.Data.Geodatabase geodatabase)
		{
			return datastore;
		}

		if (geodatabase.GetConnector() is not DatabaseConnectionProperties properties ||
		    string.IsNullOrEmpty(properties.Version) ||
		    string.Equals(properties.Version, VersionOverride,
		                  StringComparison.OrdinalIgnoreCase))
		{
			return datastore;
		}

		string openedVersion = properties.Version;

		try
		{
			properties.Version = VersionOverride;

			Datastore versioned = WorkspaceUtils.OpenDatastore(properties);

			_msg.DebugFormat("Opened datastore in version {0} instead of {1}",
			                 VersionOverride, openedVersion);

			datastore.Dispose();

			return versioned;
		}
		catch (Exception e)
		{
			// Continuing in the opened version would silently read stale data.
			datastore.Dispose();

			throw new InvalidOperationException(
				$"Could not open the datastore in version {VersionOverride} " +
				$"instead of {openedVersion}", e);
		}
	}

	public object Open()
	{
		Datastore datastore = OpenDatastore();

		return ArcWorkspace.Create(datastore);
	}

	#endregion

	#region Implementation of IWorkspaceName

	public string PathName { get; set; }
	public string WorkspaceFactoryProgID { get; set; }
	public string BrowseName => throw new NotImplementedException();

	// TODO:
	public IEnumerable<KeyValuePair<string, string>> ConnectionProperties { get; set; }

	public esriWorkspaceType Type
	{
		get
		{
			switch (FactoryType)
			{
				case WorkspaceFactory.SDE:
				case WorkspaceFactory.FeatureService:
				case WorkspaceFactory.Sql:
				case WorkspaceFactory.OLEDB:
					return esriWorkspaceType.esriRemoteDatabaseWorkspace;
				case WorkspaceFactory.FileGDB:
				case WorkspaceFactory.SQLite:
				case WorkspaceFactory.Access:
					return esriWorkspaceType.esriLocalDatabaseWorkspace;
				case WorkspaceFactory.Raster:
				case WorkspaceFactory.Shapefile:
				case WorkspaceFactory.DelimitedTextFile:
				case WorkspaceFactory.LASDataset:
				case WorkspaceFactory.Tin:
				case WorkspaceFactory.Excel:
				case WorkspaceFactory.BIMFile:

					return esriWorkspaceType.esriFileSystemWorkspace;
				default:

					throw new ArgumentOutOfRangeException(
						nameof(FactoryType), FactoryType,
						$"No workspace type for workspace factory {FactoryType}");
			}
		}
	}

	public string Category => throw new NotImplementedException();
	public string ConnectionString { get; set; }

	public bool IsFeatureService => FactoryType == WorkspaceFactory.FeatureService;

	/// <summary>
	/// Reads the version keyword out of a database connection string, or null when it carries
	/// none. Cheap and thread-safe: unlike ProjectWorkspace.GetVersionName it does not touch the
	/// datastore, so it can be called off the MCT.
	/// </summary>
	[CanBeNull]
	public static string GetVersionName([CanBeNull] string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString) ||
		    ! TryFindVersion(connectionString, out int startIndex, out int length))
		{
			return null;
		}

		return connectionString.Substring(startIndex, length);
	}

	public void ChangeVersion(string newVersionName)
	{
		if (! TryFindVersion(ConnectionString, out int startIndex, out int length))
		{
			_msg.DebugFormat("No version keyword found in connection string: {0}",
			                 ConnectionString);
			return;
		}

		ConnectionString = ConnectionString.Remove(startIndex, length)
		                                   .Insert(startIndex, newVersionName);
	}

	private static bool TryFindVersion([NotNull] string connectionString,
	                                   out int startIndex, out int length)
	{
		const string keyword = "VERSION=";

		int keywordIndex =
			connectionString.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

		if (keywordIndex < 0)
		{
			startIndex = length = 0;
			return false;
		}

		startIndex = keywordIndex + keyword.Length;
		int endIndex = connectionString.IndexOf(";", startIndex, StringComparison.Ordinal);
		if (endIndex < 0)
		{
			endIndex = connectionString.Length;
		}

		length = endIndex - startIndex;
		return true;
	}

	#endregion

	#region Overrides of Object

	public override string ToString()
	{
		return $"{FactoryType} - {ConnectionString}";
	}

	#endregion

	// TODO: Coppies - move to ProSuite.Commons

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

	[NotNull]
	private static IEnumerable<string> GetPasswordKeywords()
	{
		yield return "PASSWORD";
		yield return "ENCRYPTED_PASSWORD";
		yield return "ENCRYPTED_PASSWORD_UTF8";
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
			StringComparison.Ordinal);

		int pwdEndIndex = pwdSeparator2Index < 0
			                  ? workspaceConnectionString.Length - 1
			                  : pwdSeparator2Index - 1;

		return workspaceConnectionString.Remove(pwdStartIndex,
		                                        pwdEndIndex - pwdStartIndex + 1)
		                                .Insert(pwdStartIndex, passwordPadding);
	}
}
