extern alias EsriGeodatabase;
using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase.AO;
using ProSuite.ArcGIS.Geodatabase.AO;
using ProSuite.Commons.Text;
using ICursor = EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ICursor;
using IEnumDataset = EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IEnumDataset;


namespace ESRI.ArcGIS.Geodatabase
{
	extern alias EsriSystem;

	public class ArcTable : ITable, IObjectClass, IDataset
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable _aoTable;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObjectClass _aoObjectClass;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset _aoDataset;

		public ArcTable(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable aoTable)
		{
			_aoTable = aoTable;
			_aoObjectClass = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObjectClass)aoTable;
			_aoDataset = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset)aoTable;
		}

		#region Implementation of IClass

		public int FindField(string name)
		{
			return _aoTable.FindField(name);
		}

		void IClass.AddField(IField field)
		{
			ArcField arcField = (ArcField)field;

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField = arcField.AoField;
			_aoTable.AddField(aoField);
		}

		void IClass.DeleteField(IField field)
		{
			ArcField arcField = (ArcField)field;
			_aoTable.DeleteField(arcField.AoField);
		}


		//public void AddIndex(IIndex Index)
		//{
		//	_aoObjectClass.AddIndex(Index);
		//}

		//public void DeleteIndex(IIndex Index)
		//{
		//	_aoObjectClass.DeleteIndex(Index);
		//}

		//public int FindField(string Name)
		//{
		//	return _aoObjectClass.FindField(Name);
		//}

		public IRow CreateRow()
		{
			var aoRow = _aoTable.CreateRow();
			return new ArcRow((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject)aoRow);
		}

		public IRow GetRow(int oid)
		{
			var aoRow = _aoTable.GetRow(oid);
			return new ArcRow((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObject)aoRow);
		}

		public IEnumerable<IRow> GetRows(object oids, bool recycling)
		{
			ICursor cursor = _aoTable.GetRows(oids, recycling);

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow row;

			while ((row = cursor.NextRow()) != null)
			{
				yield return ArcUtils.ToArcObject(row);
			}
		}

		public IRowBuffer CreateRowBuffer()
		{
			throw new NotImplementedException();
			//return _aoTable.CreateRowBuffer();
		}

		public void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;
			var arcRow = (ArcRow)buffer;
			_aoTable.UpdateSearchedRows(arcQueryFilter.AoQueryFilter, arcRow.AoObject);
		}

		public void DeleteSearchedRows(IQueryFilter queryFilter)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;
			_aoTable.DeleteSearchedRows(arcQueryFilter.AoQueryFilter);
		}


		public int RowCount(IQueryFilter queryFilter)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;
			return _aoTable.RowCount(arcQueryFilter.AoQueryFilter);
		}

		//public IEnumerable<IRow> EnumRows(IQueryFilter queryFilter, bool recycle)
		//{
		//	foreach (var row in new EnumCursor(this, queryFilter, recycle))
		//	{
		//		yield return row;
		//	}
		//}

		public IEnumerable<IRow> Search(IQueryFilter queryFilter, bool recycling)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;

			ICursor cursor = _aoTable.Search(arcQueryFilter.AoQueryFilter, recycling);

			return ArcUtils.GetArcRows(cursor);
			//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow row;

			//while ((row = cursor.NextRow()) != null)
			//{
			//	yield return ToArcObject(row);
			//}
		}

		public IEnumerable<IRow> Update(IQueryFilter queryFilter, bool recycling)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;

			ICursor cursor = _aoTable.Update(arcQueryFilter.AoQueryFilter, recycling);

			return ArcUtils.GetArcRows(cursor);
		}

		public IEnumerable<IRow> Insert(bool useBuffering)
		{
			ICursor cursor = _aoTable.Insert(useBuffering);

			return ArcUtils.GetArcRows(cursor);
		}

		public ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;
			ArcWorkspace arcWorkspace = (ArcWorkspace)selectionContainer;

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISelectionSet selectionSet =
				_aoTable.Select(arcQueryFilter.AoQueryFilter,
					(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriSelectionType)selType,
					(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriSelectionOption)selOption,
					arcWorkspace.AoWorkspace);

			return new ArcSelectionSet(selectionSet);
		}

		//void IClass.AddField(IField field)
		//{
		//	ArcField arcField = (ArcField)field;

		//	EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField = arcField.AoField;

		//	_aoTable.AddField(aoField);
		//}

		//void IClass.DeleteField(IField field)
		//{
		//	ArcField arcField = (ArcField)field;

		//	EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField = arcField.AoField;

		//	_aoTable.DeleteField(aoField);
		//}

		//void IClass.AddIndex(IIndex Index)
		//{
		//	_aoTable.AddIndex(Index);
		//}

		//void IClass.DeleteIndex(IIndex Index)
		//{
		//	_aoTable.DeleteIndex(Index);
		//}

		public IFields Fields => new ArcFields(_aoTable.Fields);

		//public IIndexes Indexes => ((IClass)_aoTable).Indexes;

		public bool HasOID => _aoTable.HasOID;

		public string OIDFieldName => _aoTable.OIDFieldName;

		//public UID CLSID => _aoTable.CLSID;

		//public UID EXTCLSID => _aoTable.EXTCLSID;

		//public object Extension => _aoTable.Extension;

		public int ObjectClassID => _aoObjectClass.ObjectClassID;

		public string AliasName
		{
			get
			{
				try
				{
					string aliasName = _aoObjectClass.AliasName;

					return StringUtils.IsNotEmpty(aliasName)
						? aliasName
						: Name;
				}
				catch (NotImplementedException)
				{
					return Name;
				}
			}
		}

		public IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role)
		{
			var enumRelClasses =
				_aoObjectClass.get_RelationshipClasses((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriRelRole)role);

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass relClass;

			while ((relClass = enumRelClasses.Next()) != null)
			{
				yield return new ArcRelationshipClass(relClass);
			}
		}

		//public IPropertySet ExtensionProperties => _aoTable.ExtensionProperties;

		#endregion

		#region Implementation of IDataset

		public bool CanCopy()
		{
			return _aoDataset.CanCopy();
		}

		public bool CanDelete()
		{
			return _aoDataset.CanDelete();
		}

		public void Delete()
		{
			_aoDataset.Delete();
		}

		public bool CanRename()
		{
			return _aoDataset.CanRename();
		}

		public void Rename(string name)
		{
			_aoDataset.Rename(name);
		}

		public string Name => _aoDataset.Name;

		public IName FullName
		{
			get
			{
				EsriSystem::ESRI.ArcGIS.esriSystem.IName aoDatasetFullName = _aoDataset.FullName;

				return new ArcName(aoDatasetFullName);
			}
		}

		public string BrowseName
		{
			get => _aoDataset.BrowseName;
			set => _aoDataset.BrowseName = value;
		}

		public esriDatasetType Type => (esriDatasetType)_aoDataset.Type;

		public string Category => _aoDataset.Category;

		public IEnumerable<IDataset> Subsets
		{
			get
			{
				IEnumDataset enumDataset = _aoDataset.Subsets;

				EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset dataset;
				while ((dataset = enumDataset.Next()) != null)
				{
					yield return dataset is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass
						? new ArcFeatureClass((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass)dataset)
						: new ArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)dataset);
				}
			}
		}

		IWorkspace IDataset.Workspace =>
			new ArcWorkspace((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IWorkspace)_aoDataset.Workspace);
		//public IWorkspace Workspace => new ArcWorkspace(_aoDataset.Workspace);

		//public IPropertySet PropertySet => _aoDataset.PropertySet;

		//public IDataset Copy(string copyName, IWorkspace copyWorkspace)
		//{
		//	return _aoDataset.Copy(copyName, copyWorkspace);
		//}

		#endregion
	}

	public class ArcName : IName
	{
		private readonly EsriSystem::ESRI.ArcGIS.esriSystem.IName _aoName;

		public ArcName(EsriSystem::ESRI.ArcGIS.esriSystem.IName aoName)
		{
			_aoName = aoName;
		}

		#region Implementation of IName

		public string NameString
		{
			get => _aoName.NameString;
			set { _aoName.NameString = value; }
		}

		public object Open()
		{
			return _aoName.Open();
		}

		#endregion
	}
}
