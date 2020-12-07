using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	/// <summary>
	/// A basic workspace implementation that allows equality comparison / differentiation
	/// by a workspace handle.
	/// </summary>
	public class GdbWorkspace : IWorkspace, IFeatureWorkspace, IGeodatabaseRelease, IDataset
	{
		private readonly BackingDataStore _backingDataStore;

		private IName _fullName;

		public static GdbWorkspace CreateNullWorkspace()
		{
			BackingDataStore dataStore = new GdbTableContainer(new GdbTable[0]);

			return new GdbWorkspace(dataStore);
		}

		public GdbWorkspace([NotNull] BackingDataStore backingDataStore)
		{
			_backingDataStore = backingDataStore;
		}

		public int? WorkspaceHandle { get; set; }

		public IEnumerable<IDataset> GetDatasets()
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

		string IWorkspace.PathName => throw new NotImplementedException();

		public esriWorkspaceType Type { get; set; } = esriWorkspaceType.esriRemoteDatabaseWorkspace;

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
			get => throw new NotImplementedException();
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

		IWorkspaceFactory IWorkspaceName.WorkspaceFactory => throw new NotImplementedException();

		IPropertySet IWorkspaceName.ConnectionProperties
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriWorkspaceType Type => esriWorkspaceType.esriRemoteDatabaseWorkspace;

		string IWorkspaceName.Category => throw new NotImplementedException();

		#endregion

		IPropertySet IWorkspaceName2.ConnectionProperties
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		IWorkspaceFactory IWorkspaceName2.WorkspaceFactory => throw new NotImplementedException();

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
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}
	}
}
