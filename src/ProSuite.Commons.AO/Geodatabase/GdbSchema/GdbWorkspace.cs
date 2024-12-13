using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// A basic workspace implementation that allows equality comparison and identity checking
	/// by a workspace handle.
	/// </summary>
	public class GdbWorkspace : IWorkspace, IFeatureWorkspace, IGeodatabaseRelease, IDataset,
	                            IDatabaseConnectionInfo2,
	                            IEquatable<IWorkspace>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly BackingDataStore _backingDataStore;

		private readonly string _versionName;

		private readonly string _defaultVersionName;
		private readonly DateTime? _defaultVersionCreationDate;
		private readonly DateTime? _defaultVersionModificationDate;

		private IName _fullName;

		public static GdbWorkspace CreateEmptyWorkspace(long? workspaceHandle = null)
		{
			BackingDataStore dataStore = new GdbTableContainer();

			return new GdbWorkspace(dataStore, workspaceHandle);
		}

		public static GdbWorkspace CreateFromFgdb([NotNull] IWorkspace fileGdbWorkspace,
		                                          [CanBeNull] BackingDataStore dataStore = null)
		{
			if (dataStore == null)
			{
				dataStore = new GdbTableContainer();
			}

			return new GdbWorkspace(dataStore, fileGdbWorkspace.GetHashCode(),
			                        WorkspaceDbType.FileGeodatabase,
			                        fileGdbWorkspace.PathName);
		}

		public static GdbWorkspace CreateFromSdeWorkspace(
			[NotNull] IVersionedWorkspace sdeWorkspace,
			[CanBeNull] BackingDataStore dataStore = null)
		{
			if (dataStore == null)
			{
				dataStore = new GdbTableContainer();
			}

			string versionName = ((IVersion) sdeWorkspace).VersionName;
			string defaultVersionName = sdeWorkspace.DefaultVersion.VersionName;

			DateTime? defaultVersionCreationDate =
				(DateTime?) sdeWorkspace.DefaultVersion.VersionInfo.Created;

			DateTime? defaultVersionModificationDate =
				(DateTime?) sdeWorkspace.DefaultVersion.VersionInfo.Modified;

			return new GdbWorkspace(dataStore, sdeWorkspace.GetHashCode(),
			                        WorkspaceUtils.GetWorkspaceDbType((IWorkspace) sdeWorkspace),
			                        null, versionName, defaultVersionName,
			                        defaultVersionCreationDate, defaultVersionModificationDate);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbWorkspace"/> class.
		/// </summary>
		/// <param name="backingDataStore">The container that maintains the list of </param>
		/// <param name="workspaceHandle">Application-defined handle that simplifies equality comparisons.</param>
		/// <param name="dbType">The database type.</param>
		/// <param name="path">The path in case it is a file-based geodatabase.</param>
		/// <param name="versionName">The current version name in case it represents a version.</param>
		/// <param name="defaultVersionName">The name of the default version, used for equality
		/// comparisons.</param>
		/// <param name="defaultVersionCreationDate">The creation date of the default version, used
		/// for equality comparisons.</param>
		/// <param name="defaultVersionModificationDate">The creation date of the default version, used
		/// for equality comparisons</param>
		public GdbWorkspace([NotNull] BackingDataStore backingDataStore,
		                    long? workspaceHandle = null,
		                    WorkspaceDbType dbType = WorkspaceDbType.ArcSDE,
		                    string path = null,
		                    string versionName = null,
		                    string defaultVersionName = null,
		                    DateTime? defaultVersionCreationDate = null,
		                    DateTime? defaultVersionModificationDate = null)
		{
			_backingDataStore = backingDataStore;

			WorkspaceHandle = workspaceHandle;

			DbType = dbType;
			PathName = path;

			_versionName = versionName;
			_defaultVersionName = defaultVersionName;
			_defaultVersionCreationDate = defaultVersionCreationDate;
			_defaultVersionModificationDate = defaultVersionModificationDate;
		}

		/// <summary>
		/// An application-defined handle that simplifies equality comparisons.
		/// </summary>
		public long? WorkspaceHandle { get; }

		public WorkspaceDbType DbType { get; }

		public IEnumerable<VirtualTable> GetDatasets()
		{
			return _backingDataStore.GetDatasets(esriDatasetType.esriDTAny);
		}

		public ITable OpenQueryTable(string relationshipClassName)
		{
			return _backingDataStore.OpenQueryTable(relationshipClassName);
		}

		#region private class implementing IEnumDataset

		private class DatasetEnum : IEnumDataset
		{
			private readonly IEnumerator<IDataset> _datasetsEnumerator;

			public DatasetEnum(IEnumerable<IDataset> datasets)
			{
				_datasetsEnumerator = datasets.GetEnumerator();
				_datasetsEnumerator.Reset();
			}

			public IDataset Next()
			{
				if (_datasetsEnumerator.MoveNext()) return _datasetsEnumerator.Current;

				return null;
			}

			public void Reset()
			{
				_datasetsEnumerator.Reset();
			}
		}

		#endregion

		#region IWorkspace members

		public bool IsDirectory()
		{
			return false;
		}

		public bool Exists()
		{
			return true;
		}

		public void ExecuteSQL(string sqlStatement)
		{
			_backingDataStore.ExecuteSql(sqlStatement);
		}

		IPropertySet IWorkspace.ConnectionProperties { get; } = new PropertySetClass();

		IWorkspaceFactory IWorkspace.WorkspaceFactory => throw new NotImplementedException();

		public IEnumDataset get_Datasets(esriDatasetType datasetType)
		{
			return new DatasetEnum(_backingDataStore.GetDatasets(datasetType));
		}

		IEnumDatasetName IWorkspace.get_DatasetNames(esriDatasetType datasetType)
		{
			throw new NotImplementedException();
		}

		public string PathName { get; }

		public esriWorkspaceType Type
		{
			get => WorkspaceUtils.ToEsriWorkspaceType(DbType);
		}

		string IDataset.Category => throw new NotImplementedException();

		IEnumDataset IDataset.Subsets => throw new NotImplementedException();

		IWorkspace IDataset.Workspace => throw new NotImplementedException();

		IPropertySet IDataset.PropertySet => throw new NotImplementedException();

		#endregion

		#region IFeatureWorkspace members

		public ITable OpenTable(string name)
		{
			return _backingDataStore.OpenTable(name);
		}

		public IFeatureClass OpenFeatureClass(string name)
		{
			return (IFeatureClass) _backingDataStore.OpenTable(name);
		}

		IFeatureDataset IFeatureWorkspace.OpenFeatureDataset(string name)
		{
			throw new NotImplementedException();
		}

		IRelationshipClass IFeatureWorkspace.OpenRelationshipClass(string name)
		{
			throw new NotImplementedException();
		}

		ITable IFeatureWorkspace.OpenRelationshipQuery(
			IRelationshipClass relClass, bool joinForward, IQueryFilter srcQueryFilter,
			ISelectionSet srcSelectionSet, string targetColumns, bool doNotPushJoinToDb)
		{
			throw new NotImplementedException();
		}

		public IDataset OpenExtensionDataset(esriDatasetType extensionDatasetType,
		                                     string extensionDatasetName)
		{
			throw new NotImplementedException();
		}

		IFeatureDataset IFeatureWorkspace.OpenFeatureQuery(string queryName, IQueryDef queryDef)
		{
			throw new NotImplementedException();
		}

		ITable IFeatureWorkspace.CreateTable(
			string name, IFields fields, UID clsid, UID extclsid, string configKeyword)
		{
			throw new NotImplementedException();
		}

		IFeatureClass IFeatureWorkspace.CreateFeatureClass(
			string name, IFields fields, UID clsid, UID extclsid, esriFeatureType featureType,
			string shapeFieldName, string configKeyword)
		{
			throw new NotImplementedException();
		}

		IFeatureDataset IFeatureWorkspace.CreateFeatureDataset(
			string name, ISpatialReference spatialReference)
		{
			throw new NotImplementedException();
		}

		IQueryDef IFeatureWorkspace.CreateQueryDef()
		{
			throw new NotImplementedException();
		}

		IRelationshipClass IFeatureWorkspace.CreateRelationshipClass(
			string relClassName, IObjectClass originClass, IObjectClass destinationClass,
			string forwardLabel, string backwardLabel,
			esriRelCardinality cardinality, esriRelNotification notification, bool isComposite,
			bool isAttributed, IFields relAttrFields, string originPrimaryKey,
			string destPrimaryKey, string originForeignKey, string destForeignKey)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IGeodatabaseRelease members

		public void Upgrade()
		{
			throw new NotImplementedException();
		}

		public bool CanUpgrade => false;
		public bool CurrentRelease { get; set; }

		public int MajorVersion { get; } = 3;
		public int MinorVersion { get; } = 0;
		public int BugfixVersion { get; } = 0;

		// NOTE: This property has been added in AO11 
		public bool ForceCurrentRelease => false;

		#endregion

		#region IDataset members

		bool IDataset.CanCopy()
		{
			return false;
		}

		IDataset IDataset.Copy(string copyName, IWorkspace copyWorkspace)
		{
			throw new InvalidOperationException();
		}

		bool IDataset.CanDelete()
		{
			return false;
		}

		void IDataset.Delete()
		{
			throw new InvalidOperationException();
		}

		bool IDataset.CanRename()
		{
			return false;
		}

		void IDataset.Rename(string name)
		{
			throw new InvalidOperationException();
		}

		string IDataset.Name => string.Empty;

		IName IDataset.FullName
		{
			get
			{
				if (_fullName == null)
				{
					_fullName = new GdbWorkspaceName(this);
				}

				return _fullName;
			}
		}

		string IDataset.BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		esriDatasetType IDataset.Type => esriDatasetType.esriDTContainer;

		#endregion

		#region IDatabaseConnectionInfo members

		string IDatabaseConnectionInfo.ConnectedDatabase => throw new NotImplementedException();
		string IDatabaseConnectionInfo2.ConnectedDatabase => throw new NotImplementedException();

		esriGeodatabaseServerClassType IDatabaseConnectionInfo2.GeodatabaseServerClass =>
			esriGeodatabaseServerClassType.esriServerClassUnknown;

		string IDatabaseConnectionInfo.ConnectedUser => throw new NotImplementedException();
		string IDatabaseConnectionInfo2.ConnectedUser => throw new NotImplementedException();

		string IDatabaseConnectionInfo2.ConnectionServer => throw new NotImplementedException();

		esriConnectionDBMS IDatabaseConnectionInfo2.ConnectionDBMS => ToEsriConnectionDbms(DbType);

		object IDatabaseConnectionInfo2.ConnectionCurrentDateTime =>
			throw new NotImplementedException();

		#endregion

		#region Equality members

		public bool IsSameDatabase([CanBeNull] IWorkspace other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other.Type != Type)
			{
				return false;
			}

			if (other is GdbWorkspace otherGdbWorkspace)
			{
				return Equals(otherGdbWorkspace);
			}

			// Other workspace is file based:
			if (other.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				// Compare file paths
				if (string.IsNullOrEmpty(PathName) ||
				    string.IsNullOrEmpty(other.PathName))
				{
					return false;
				}

				//Determines whether two Uri instances have the same value.
				// e.g. these paths are equal
				// C:\Users\daro\AppData\Local\Temp\GdbWorkspaceTest.gdb
				// file:///C:/Users/daro/AppData/Local/Temp/GdbWorkspaceTest.gdb
				return Equals(new Uri(PathName), new Uri(other.PathName));
			}

			// Other workspace is SDE:
			var otherVersionedWorkspace = other as IVersionedWorkspace;

			if (otherVersionedWorkspace != null)
			{
				return IsSameDatabase(otherVersionedWorkspace);
			}

			_msg.Debug(
				"Unknown remote database workspace type that does not implement IVersionedWorkspace.");

			return false;
		}

		public bool IsSameDatabase([CanBeNull] IVersionedWorkspace other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			IVersion otherDefaultVersion = other.DefaultVersion;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Compare default version instances");
			}

			string otherDefaultVersionName = otherDefaultVersion.VersionName ?? string.Empty;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version names ({0}, {1})",
				                 _defaultVersionName, otherDefaultVersionName);
			}

			if (string.IsNullOrEmpty(_defaultVersionName) ||
			    ! _defaultVersionName.Equals(otherDefaultVersionName,
			                                 StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			IVersionInfo otherDefaultInfo = otherDefaultVersion.VersionInfo;

			string creationDateThis = _defaultVersionCreationDate?.ToString();
			string creationDateOther = otherDefaultInfo.Created?.ToString();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version creation dates ({0}, {1})",
				                 creationDateThis, creationDateOther);
			}

			if (! Equals(creationDateThis, creationDateOther))
			{
				return false;
			}

			// NOTE: To be completely sure both dates should be a few seconds in the past
			//       to prevent comparing a stale date with a current date.
			string modificationDateThis = _defaultVersionModificationDate?.ToString();
			string modificationDateOther = otherDefaultInfo.Modified?.ToString();

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Compare default version modification dates ({0}, {1})",
				                 modificationDateThis, modificationDateOther);
			}

			return Equals(modificationDateThis, modificationDateOther);
		}

		public bool Equals([CanBeNull] GdbWorkspace other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return WorkspaceHandle == other.WorkspaceHandle &&
			       Type == other.Type &&
			       PathName == other.PathName &&
			       _versionName == other._versionName &&
			       _defaultVersionName == other._defaultVersionName &&
			       _defaultVersionCreationDate == other._defaultVersionCreationDate &&
			       _defaultVersionModificationDate == other._defaultVersionModificationDate;
		}

		public bool Equals(IWorkspace other)
		{
			if (! IsSameDatabase(other))
			{
				return false;
			}

			string versionName = null;
			if (other is IVersion version)
			{
				versionName = version.VersionName;
			}

			return string.Equals(versionName, _versionName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj is IWorkspace workspace)
			{
				return Equals(workspace);
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals(obj as GdbWorkspace);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _defaultVersionName != null ? _defaultVersionName.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^
				           (_versionName != null ? _versionName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ _defaultVersionCreationDate.GetHashCode();
				hashCode = (hashCode * 397) ^ _defaultVersionModificationDate.GetHashCode();
				hashCode = (hashCode * 397) ^ WorkspaceHandle.GetHashCode();
				hashCode = (hashCode * 397) ^ (PathName != null ? PathName.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion

		private static esriConnectionDBMS ToEsriConnectionDbms(WorkspaceDbType dbType)
		{
			switch (dbType)
			{
				case WorkspaceDbType.Unknown:
				case WorkspaceDbType.FileSystem:
				case WorkspaceDbType.FileGeodatabase:
				case WorkspaceDbType.PersonalGeodatabase:
				case WorkspaceDbType.MobileGeodatabase:
					return esriConnectionDBMS.esriDBMS_Unknown;
				case WorkspaceDbType.ArcSDE:
					return esriConnectionDBMS.esriDBMS_Unknown;
				case WorkspaceDbType.ArcSDESqlServer:
					return esriConnectionDBMS.esriDBMS_SQLServer;
				case WorkspaceDbType.ArcSDEOracle:
					return esriConnectionDBMS.esriDBMS_Oracle;
				case WorkspaceDbType.ArcSDEPostgreSQL:
					return esriConnectionDBMS.esriDBMS_PostgreSQL;
				case WorkspaceDbType.ArcSDEInformix:
					return esriConnectionDBMS.esriDBMS_Informix;
				case WorkspaceDbType.ArcSDEDB2:
					return esriConnectionDBMS.esriDBMS_DB2;
				default:
					throw new ArgumentOutOfRangeException(nameof(dbType), dbType,
					                                      "Unknown DB type");
			}
		}
	}

	internal class GdbWorkspaceName : IName, IWorkspaceName2
	{
		private readonly GdbWorkspace _workspace;

		private string _connectionString;

		public GdbWorkspaceName(GdbWorkspace workspace)
		{
			_workspace = workspace;

			// Used for comparison. Assumption: model-workspace relationship is 1-1
			// Once various versions should be supported, this will have to be more fancy.
			_connectionString = _connectionString =
				                    $"Provider=GdbWorkspaceName;Data Source={workspace.WorkspaceHandle}";
		}

		#region IName members

		public object Open()
		{
			return _workspace;
		}

		public string NameString { get; set; }

		#endregion

		#region IWorkspaceName members

		string IWorkspaceName.PathName
		{
			get => _workspace.PathName;
			set => throw new NotImplementedException();
		}

		string IWorkspaceName.WorkspaceFactoryProgID
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		string IWorkspaceName.BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		IWorkspaceFactory IWorkspaceName.WorkspaceFactory => null;

		IPropertySet IWorkspaceName.ConnectionProperties
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriWorkspaceType Type => _workspace.Type;

		string IWorkspaceName.Category => throw new NotImplementedException();

		#endregion

		IPropertySet IWorkspaceName2.ConnectionProperties
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		IWorkspaceFactory IWorkspaceName2.WorkspaceFactory => null;

		string IWorkspaceName2.Category => throw new NotImplementedException();

		string IWorkspaceName2.ConnectionString
		{
			get => _connectionString;
			set => _connectionString = value;
		}

		string IWorkspaceName2.WorkspaceFactoryProgID
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		string IWorkspaceName2.BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		string IWorkspaceName2.PathName
		{
			get => _workspace.PathName;
			set => throw new NotImplementedException();
		}
	}
}
