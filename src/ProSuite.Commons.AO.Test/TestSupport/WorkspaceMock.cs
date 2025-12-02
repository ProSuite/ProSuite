using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class WorkspaceMock : IWorkspaceEdit2, IFeatureWorkspace, IWorkspace
	{
		private readonly string _pathName;
		private Dictionary<string, IDataset> _datasets;

		public WorkspaceMock() { }

		public WorkspaceMock(string pathName)
		{
			_pathName = pathName;
		}

		#region IWorkspaceEdit Members

		public void AbortEditOperation()
		{
			throw new NotImplementedException();
		}

		public void DisableUndoRedo()
		{
			throw new NotImplementedException();
		}

		public void EnableUndoRedo()
		{
			throw new NotImplementedException();
		}

		public void HasEdits(ref bool pHasEdits)
		{
			throw new NotImplementedException();
		}

		#region IWorkspaceEdit2 Members

		public bool IsInEditOperation { get; private set; }

		public IDataChangesEx get_EditDataChanges(esriEditDataChangesType editChangeType)
		{
			throw new NotImplementedException();
		}

		#endregion

		public void HasRedos(ref bool pHasRedos)
		{
			throw new NotImplementedException();
		}

		public void HasUndos(ref bool pHasUndos)
		{
			throw new NotImplementedException();
		}

		public bool IsBeingEdited()
		{
			throw new NotImplementedException();
		}

		public void RedoEditOperation()
		{
			throw new NotImplementedException();
		}

		public void StartEditOperation()
		{
			IsInEditOperation = true;
		}

		public void StartEditing(bool withUndoRedo)
		{
			throw new NotImplementedException();
		}

		public void StopEditOperation()
		{
			IsInEditOperation = false;
		}

		public void StopEditing(bool saveEdits)
		{
			throw new NotImplementedException();
		}

		public void UndoEditOperation()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IFeatureWorkspace Members

		public IFeatureClass CreateFeatureClass(string Name, IFields Fields, UID CLSID,
		                                        UID EXTCLSID, esriFeatureType FeatureType,
		                                        string ShapeFieldName,
		                                        string ConfigKeyword)
		{
			throw new NotImplementedException();
		}

		public IFeatureDataset CreateFeatureDataset(string Name,
		                                            ISpatialReference SpatialReference)
		{
			throw new NotImplementedException();
		}

		public IQueryDef CreateQueryDef()
		{
			throw new NotImplementedException();
		}

		public IRelationshipClass CreateRelationshipClass(string relClassName,
		                                                  IObjectClass OriginClass,
		                                                  IObjectClass DestinationClass,
		                                                  string forwardLabel,
		                                                  string backwardLabel,
		                                                  esriRelCardinality Cardinality,
		                                                  esriRelNotification
			                                                  Notification,
		                                                  bool IsComposite,
		                                                  bool IsAttributed,
		                                                  IFields relAttrFields,
		                                                  string OriginPrimaryKey,
		                                                  string destPrimaryKey,
		                                                  string OriginForeignKey,
		                                                  string destForeignKey)
		{
			throw new NotImplementedException();
		}

		public ITable CreateTable(string Name, IFields Fields, UID CLSID, UID EXTCLSID,
		                          string ConfigKeyword)
		{
			throw new NotImplementedException();
		}

		public IFeatureClass OpenFeatureClass(string Name)
		{
			throw new NotImplementedException();
		}

		public IFeatureDataset OpenFeatureDataset(string Name)
		{
			throw new NotImplementedException();
		}

		public IFeatureDataset OpenFeatureQuery(string QueryName, IQueryDef pQueryDef)
		{
			throw new NotImplementedException();
		}

		public IRelationshipClass OpenRelationshipClass(string Name)
		{
			throw new NotImplementedException();
		}

		public ITable OpenRelationshipQuery(IRelationshipClass pRelClass,
		                                    bool joinForward,
		                                    IQueryFilter pSrcQueryFilter,
		                                    ISelectionSet pSrcSelectionSet,
		                                    string TargetColumns, bool DoNotPushJoinToDB)
		{
			throw new NotImplementedException();
		}

#if Server11 || ARCGIS_12_0_OR_GREATER
		public IDataset OpenExtensionDataset(esriDatasetType extensionDatasetType,
		                                     string extensionDatasetName)
		{
			throw new NotImplementedException();
		}
#endif

		public ITable OpenTable(string Name)
		{
			return (ITable) _datasets[Name];
		}

		public void AddDataset(ObjectClassMock dataset)
		{
			if (_datasets == null)
			{
				_datasets = new Dictionary<string, IDataset>();
			}

			_datasets.Add(((IDataset) dataset).Name, dataset);
			dataset.SetWorkspace(this);
		}

		#endregion

		#region IWorkspace Members

		IPropertySet IWorkspace.ConnectionProperties
		{
			get { throw new NotImplementedException(); }
		}

		void IWorkspace.ExecuteSQL(string sqlStmt)
		{
			throw new NotImplementedException();
		}

		bool IWorkspace.Exists()
		{
			throw new NotImplementedException();
		}

		bool IWorkspace.IsDirectory()
		{
			throw new NotImplementedException();
		}

		string IWorkspace.PathName => _pathName;

		esriWorkspaceType IWorkspace.Type
		{
			get { throw new NotImplementedException(); }
		}

		IWorkspaceFactory IWorkspace.WorkspaceFactory => new WorkspaceFactoryMock();

		IEnumDatasetName IWorkspace.get_DatasetNames(esriDatasetType DatasetType)
		{
			throw new NotImplementedException();
		}

		IEnumDataset IWorkspace.get_Datasets(esriDatasetType DatasetType)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
