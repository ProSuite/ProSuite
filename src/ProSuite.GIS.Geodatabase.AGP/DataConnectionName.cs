using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;
using esriDatasetType = ProSuite.GIS.Geodatabase.API.esriDatasetType;
using esriRelCardinality = ProSuite.GIS.Geodatabase.API.esriRelCardinality;

namespace ProSuite.GIS.Geodatabase.AGP
{
	/// <summary>
	/// A name implementation based on the Pro CIMStandardDataConnection, with the main purpose of
	/// comparing data sources.
	/// </summary>
	public class DataConnectionName : IDatasetName
	{
		[CanBeNull]
		public static DataConnectionName FromDataConnection(
			[NotNull] CIMDataConnection cimDataConnection)
		{
			switch (cimDataConnection)
			{
				case CIMFeatureDatasetDataConnection cimFeatureDatasetDataConnection:
					return FromDataConnection(cimFeatureDatasetDataConnection);
				case CIMStandardDataConnection cimStandardDataConnection:
					return FromDataConnection(cimStandardDataConnection);
				case CIMRelQueryTableDataConnection cimRelQueryTableConnection:
					return new MemoryRelQueryTableName(cimRelQueryTableConnection);
				default:
					throw new ArgumentOutOfRangeException(nameof(cimDataConnection));
			}
		}

		public static DataConnectionName FromDataConnection(
			[NotNull] CIMStandardDataConnection standardConnection)
		{
			return new DataConnectionName(standardConnection);
		}

		public static DataConnectionName FromDataConnection(
			[NotNull] CIMFeatureDatasetDataConnection featureDataConnection)
		{
			return new DataConnectionName(featureDataConnection);
		}

		public DataConnectionName(CIMFeatureDatasetDataConnection featureDataConnection)
			: this(featureDataConnection.Dataset,
			       (esriDatasetType) featureDataConnection.DatasetType,
			       new DataConnectionWorkspaceName(featureDataConnection.WorkspaceConnectionString,
			                                       featureDataConnection.WorkspaceFactory)) { }

		public DataConnectionName(CIMStandardDataConnection standardConnection)
			: this(standardConnection.Dataset, (esriDatasetType) standardConnection.DatasetType,
			       new DataConnectionWorkspaceName(standardConnection)) { }

		public DataConnectionName(string name, esriDatasetType type,
		                          DataConnectionWorkspaceName workspaceName)
		{
			NameString = name;
			Type = type;
			DataConnectionWorkspaceName = workspaceName;
		}

		public DataConnectionWorkspaceName DataConnectionWorkspaceName { get; set; }

		public string NameString { get; set; }
		public esriDatasetType Type { get; }
		public IWorkspaceName WorkspaceName => DataConnectionWorkspaceName;

		public object Open()
		{
			throw new NotImplementedException();
		}

		public string Name => NameString;

		public virtual void ChangeVersion(string newVersionName)
		{
			DataConnectionWorkspaceName.ChangeVersion(newVersionName);
		}

		// TODO: Extract base class to be used by clients, or add to IDatasetName interface
		public virtual CIMDataConnection ToCIMDataConnection()
		{
			return new CIMStandardDataConnection
			       {
				       WorkspaceConnectionString = WorkspaceName.ConnectionString,
				       WorkspaceFactory = DataConnectionWorkspaceName.FactoryType,
				       Dataset = NameString,
				       DatasetType = (ArcGIS.Core.CIM.esriDatasetType) Type
			       };
		}

		#region Overrides of Object

		public override string ToString()
		{
			return $"Name : {Name}, Type: {Type} - Datastore: {DataConnectionWorkspaceName}";
		}

		#endregion
	}

	/// <summary>
	/// The name implementation for a layer join (rel query table) in memory,
	/// representing a CIMRelQueryTableDataConnection in Pro.
	/// </summary>
	public class MemoryRelQueryTableName : DataConnectionName, IMemoryRelQueryTableName
	{
		// TODO: abstract base rather than deriving from DataConnectionName.

		private readonly DataConnectionName _sourceConnectionName;
		private readonly DataConnectionName _destinationConnectionName;
		private readonly esriJoinType _joinType;

		public MemoryRelQueryTableName(CIMRelQueryTableDataConnection relQueryConnection)
			: base(relQueryConnection.Name, esriDatasetType.esriDTRelationshipClass,
			       DataConnectionWorkspaceName.FromDataConnection(relQueryConnection.SourceTable))
		{
			// TODO: Proper enum translation -> ProSuite JoinCardinality, etc The enums don't match between AO and Pro!
			ForwardDirection = relQueryConnection.JoinForward;
			Cardinality = (esriRelCardinality) relQueryConnection.Cardinality;

			_sourceConnectionName = FromDataConnection(relQueryConnection.SourceTable);
			_destinationConnectionName = FromDataConnection(relQueryConnection.DestinationTable);

			PrimaryKey = relQueryConnection.PrimaryKey;
			ForeignKey = relQueryConnection.ForeignKey;

			_joinType = relQueryConnection.JoinType;
		}

		public MemoryRelQueryTableName(string name,
		                               esriDatasetType type,
		                               DataConnectionWorkspaceName workspaceName)
			: base(name, type, workspaceName)
		{
			// TODO: Proper constructor
		}

		#region Implementation of IMemoryRelQueryTableName

		public bool ForwardDirection { get; }
		public esriRelCardinality Cardinality { get; }

		public IDatasetName SourceTable => _sourceConnectionName;
		public IDatasetName DestinationTable => _destinationConnectionName;

		public string PrimaryKey { get; }
		public string ForeignKey { get; }

		#endregion

		public override void ChangeVersion(string newVersionName)
		{
			_sourceConnectionName.ChangeVersion(newVersionName);
			_destinationConnectionName.ChangeVersion(newVersionName);
		}

		public override CIMDataConnection ToCIMDataConnection()
		{
			var result = new CIMRelQueryTableDataConnection()
			             {
				             Name = Name,
				             Cardinality = (ArcGIS.Core.CIM.esriRelCardinality) Cardinality,
				             JoinType = _joinType,
				             JoinForward = ForwardDirection,
				             // TODO: Deal with one-to-first once properly implemented
				             //OneToFirst = false,
				             SourceTable = _sourceConnectionName.ToCIMDataConnection(),
				             DestinationTable = _destinationConnectionName.ToCIMDataConnection(),
				             PrimaryKey = PrimaryKey,
				             ForeignKey = ForeignKey
			             };

			return result;
		}

		public override string ToString()
		{
			return $"Name : {Name}, Type: {Type} - Datastore: {DataConnectionWorkspaceName}";
		}
	}

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

			WorkspaceFactory workspaceFactory;

			switch (connector)
			{
				case DatabaseConnectionProperties:
					workspaceFactory = WorkspaceFactory.SDE;
					break;
				case FileGeodatabaseConnectionPath fileGeodatabaseConnectionPath:
					workspaceFactory = WorkspaceFactory.FileGDB;
					break;
				case FileSystemConnectionPath fileSystemConnection:
					workspaceFactory = WorkspaceFactory.Shapefile;
					break;
				case MobileGeodatabaseConnectionPath mobileGeodatabaseConnectionPath:
					workspaceFactory = WorkspaceFactory.SQLite;
					break;
				case PluginDatasourceConnectionPath pluginDatasourceConnectionPath:
					workspaceFactory = WorkspaceFactory.Custom;
					break;
				default:
					throw new NotImplementedException(
						$"connector {connector.GetType()} is not implemented");
			}

			return new DataConnectionWorkspaceName(datastore.GetConnectionString(),
			                                       workspaceFactory);
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

		public object Open()
		{
			Connector connector = WorkspaceUtils.CreateConnector(FactoryType, ConnectionString);

			return WorkspaceUtils.OpenDatastore(connector);
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

						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public string Category => throw new NotImplementedException();
		public string ConnectionString { get; set; }

		public void ChangeVersion(string newVersionName)
		{
			string keyword = $"VERSION=";

			int keywordIndex =
				ConnectionString.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

			if (keywordIndex < 0)
			{
				_msg.DebugFormat("No version keyword found in connection string: {0}",
				                 ConnectionString);
				return;
			}

			int startIndex = keywordIndex + keyword.Length;
			int endIndex = ConnectionString.IndexOf(";", startIndex, StringComparison.Ordinal);
			if (endIndex < 0)
			{
				endIndex = ConnectionString.Length;
			}

			ConnectionString = ConnectionString.Remove(startIndex, endIndex - startIndex)
			                                   .Insert(startIndex, newVersionName);
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
}
