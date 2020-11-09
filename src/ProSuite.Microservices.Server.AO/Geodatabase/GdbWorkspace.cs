using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	/// <summary>
	/// A basic workpace implementation that allows equality comparison / differentiation
	/// by a workspace handle.
	/// </summary>
	public class GdbWorkspace : IWorkspace, IFeatureWorkspace
	{
		private readonly BackingDataStore _backingDataStore;

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

		public IPropertySet ConnectionProperties { get; } = new PropertySetClass();

		public IWorkspaceFactory WorkspaceFactory => throw new NotImplementedException();

		public IEnumDataset get_Datasets(esriDatasetType datasetType)
		{
			return new DatasetEnum(_backingDataStore.GetDatasets(datasetType));
		}

		public IEnumDatasetName get_DatasetNames(esriDatasetType datasetType)
		{
			throw new NotImplementedException();
		}

		public string PathName => throw new NotImplementedException();

		public esriWorkspaceType Type { get; } = esriWorkspaceType.esriRemoteDatabaseWorkspace;

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

		public IFeatureDataset OpenFeatureDataset(string name)
		{
			throw new NotImplementedException();
		}

		public IRelationshipClass OpenRelationshipClass(string name)
		{
			throw new NotImplementedException();
		}

		public ITable OpenRelationshipQuery(IRelationshipClass relClass, bool joinForward,
		                                    IQueryFilter srcQueryFilter,
		                                    ISelectionSet srcSelectionSet, string targetColumns,
		                                    bool doNotPushJoinToDb)
		{
			throw new NotImplementedException();
		}

		public IFeatureDataset OpenFeatureQuery(string queryName, IQueryDef queryDef)
		{
			throw new NotImplementedException();
		}

		public ITable CreateTable(string name, IFields fields, UID clsid, UID extclsid,
		                          string configKeyword)
		{
			throw new NotImplementedException();
		}

		public IFeatureClass CreateFeatureClass(string name, IFields fields, UID clsid,
		                                        UID extclsid,
		                                        esriFeatureType featureType,
		                                        string shapeFieldName, string configKeyword)
		{
			throw new NotImplementedException();
		}

		public IFeatureDataset CreateFeatureDataset(string name, ISpatialReference spatialReference)
		{
			throw new NotImplementedException();
		}

		public IQueryDef CreateQueryDef()
		{
			throw new NotImplementedException();
		}

		public IRelationshipClass CreateRelationshipClass(string relClassName,
		                                                  IObjectClass originClass,
		                                                  IObjectClass destinationClass,
		                                                  string forwardLabel, string backwardLabel,
		                                                  esriRelCardinality cardinality,
		                                                  esriRelNotification notification,
		                                                  bool isComposite, bool isAttributed,
		                                                  IFields relAttrFields,
		                                                  string originPrimaryKey,
		                                                  string destPrimaryKey,
		                                                  string originForeignKey,
		                                                  string destForeignKey)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
