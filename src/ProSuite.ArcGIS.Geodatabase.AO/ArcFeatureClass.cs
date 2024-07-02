extern alias EsriGeodatabase;
extern alias EsriGeometry;
using System;
using System.Collections.Generic;
using EsriGeodatabase::ESRI.ArcGIS.Geodatabase;

using ESRI.ArcGIS.Geodatabase.AO;
using EsriGeometry::ESRI.ArcGIS.Geometry;


namespace ESRI.ArcGIS.Geodatabase
{
	public class ArcFeatureClass : ArcTable, ITable, IFeatureClass, ISubtypes
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass _aoFeatureClass;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable _aoTable;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISubtypes _aoSubtypes;

		public ArcFeatureClass(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass aoFeatureClass)
		: base((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)aoFeatureClass)
		{
			_aoFeatureClass = aoFeatureClass;
			_aoTable = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)aoFeatureClass;
			_aoSubtypes = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISubtypes)aoFeatureClass;
		}

		#region Implementation of IClass

		int IClass.FindField(string name)
		{
			return _aoFeatureClass.FindField(name);
		}

		//public void AddField(IField field)
		//{
		//	_aoTable.AddField(field);
		//}

		//public void DeleteField(IField Field)
		//{
		//	_aoTable.DeleteField(Field);
		//}

		public void AddIndex(IIndex Index)
		{
			_aoTable.AddIndex(Index);
		}

		public void DeleteIndex(IIndex Index)
		{
			_aoTable.DeleteIndex(Index);
		}

		IFields IClass.Fields => Fields;

		//public IRow CreateRow()
		//{
		//	return CreateFeature();
		//}

		//public IRow GetRow(int OID)
		//{
		//	return GetFeature(OID);
		//}

		//public ICursor GetRows(object oids, bool Recycling)
		//{
		//	return _aoTable.GetRows(oids, Recycling);
		//}

		//public IRowBuffer CreateRowBuffer()
		//{
		//	var rowBuffer = _aoTable.CreateRowBuffer();
		//	throw new NotImplementedException();
		//	//return rowBuffer;
		//}

		//public void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)queryFilter;
		//	var arcRow = (ArcRow)buffer;
		//	_aoTable.UpdateSearchedRows(arcQueryFilter.AoQueryFilter, arcRow.AoObject);
		//}

		//public void DeleteSearchedRows(IQueryFilter queryFilter)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)queryFilter;
		//	_aoTable.DeleteSearchedRows(arcQueryFilter.AoQueryFilter);
		//}

		//public int RowCount(IQueryFilter queryFilter)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)queryFilter;
		//	return _aoTable.RowCount(arcQueryFilter.AoQueryFilter);
		//}


		//IEnumerable<IRow> ITable.Search(IQueryFilter queryFilter, bool recycling)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)queryFilter;
		//	ICursor cursor = _aoTable.Search(arcQueryFilter.AoQueryFilter, recycling);

		//	EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow row;

		//	while ((row = cursor.NextRow()) != null)
		//	{
		//		yield return ToArcObject(row);
		//	}
		//}

		IEnumerable<IFeature> IFeatureClass.Update(IQueryFilter filter, bool recycling)
		{
			foreach (var row in base.Update(filter, recycling))
			{
				yield return (IFeature)row;
			}
		}

		IEnumerable<IFeature> IFeatureClass.Insert(bool useBuffering)
		{
			foreach (IRow row in base.Insert(useBuffering))
			{
				yield return (IFeature)row;
			}
		}

		IEnumerable<IFeature> IFeatureClass.Search(IQueryFilter filter, bool recycling)
		{
			foreach (IRow row in base.Search(filter, recycling))
			{
				yield return (IFeature)row;
			}
		}

		IEnumerable<IRow> ITable.Update(IQueryFilter queryFilter, bool recycling)
		{
			foreach (IRow row in base.Update(queryFilter, recycling))
			{
				yield return (IFeature)row;
			}
		}

		//public ICursor Insert(bool useBuffering)
		//{
		//	return _aoTable.Insert(useBuffering);
		//}

		//public int FindField(string Name)
		//{
		//	return _aoTable.FindField(Name);
		//}


		public IFeature CreateFeature()
		{
			var aoFeature = _aoFeatureClass.CreateFeature();
			return new ArcFeature(aoFeature);
		}

		public IFeature GetFeature(int id)
		{
			var aoFeature = _aoFeatureClass.GetFeature(id);
			return new ArcFeature(aoFeature);
		}

		public IFeatureCursor GetFeatures(object fids, bool recycling)
		{
			return _aoFeatureClass.GetFeatures(fids, recycling);
		}

		public IFeatureBuffer CreateFeatureBuffer()
		{
			return _aoFeatureClass.CreateFeatureBuffer();
		}

		public long FeatureCount(IQueryFilter queryFilter)
		{
			var arcQueryFilter = (ArcQueryFilter)queryFilter;
			return _aoFeatureClass.FeatureCount(arcQueryFilter.AoQueryFilter);
		}

		//public IFeatureCursor Search(IQueryFilter filter, bool recycling)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)filter;
		//	return _aoFeatureClass.Search(arcQueryFilter.AoQueryFilter, recycling);
		//}

		//public IFeatureCursor Update(IQueryFilter filter, bool recycling)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)filter;
		//	return _aoFeatureClass.Update(arcQueryFilter.AoQueryFilter, recycling);
		//}

		//public IFeatureCursor Insert(bool useBuffering)
		//{
		//	return _aoFeatureClass.Insert(useBuffering);
		//}

		//public ISelectionSet Select(
		//	IQueryFilter queryFilter,
		//	esriSelectionType selType,
		//	esriSelectionOption selOption,
		//	IWorkspace selectionContainer)
		//{
		//	var arcQueryFilter = (ArcQueryFilter)queryFilter;
		//	ArcWorkspace arcWorkspace = (ArcWorkspace)selectionContainer;

		//	return _aoFeatureClass.Select(arcQueryFilter.AoQueryFilter, selType, selOption, arcWorkspace.AoWorkspace);
		//}

		void IClass.AddField(IField field)
		{
			ArcField arcField = (ArcField)field;

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField = arcField.AoField;
			_aoFeatureClass.AddField(aoField);
		}

		void IClass.DeleteField(IField field)
		{
			ArcField arcField = (ArcField)field;
			_aoFeatureClass.DeleteField(arcField.AoField);
		}

		//void IClass.AddIndex(IIndex Index)
		//{
		//	_aoFeatureClass.AddIndex(Index);
		//}

		//void IClass.DeleteIndex(IIndex Index)
		//{
		//	_aoFeatureClass.DeleteIndex(Index);
		//}


		//public IFields Fields => new ArcFields(_aoFeatureClass.Fields);

		////public IIndexes Indexes => ((IClass)_aoFeatureClass).Indexes;

		//public bool HasOID => _aoFeatureClass.HasOID;

		//public string OIDFieldName => _aoFeatureClass.OIDFieldName;

		//public UID CLSID => _aoFeatureClass.CLSID;

		//public UID EXTCLSID => _aoFeatureClass.EXTCLSID;

		//public object Extension => _aoFeatureClass.Extension;

		//public IPropertySet ExtensionProperties => _aoFeatureClass.ExtensionProperties;

		//public int ObjectClassID => _aoFeatureClass.ObjectClassID;

		//public string AliasName => _aoFeatureClass.AliasName;

		public ESRI.ArcGIS.Geometry.esriGeometryType ShapeType => (ESRI.ArcGIS.Geometry.esriGeometryType)_aoFeatureClass.ShapeType;

		public esriFeatureType FeatureType => _aoFeatureClass.FeatureType;

		public string ShapeFieldName => _aoFeatureClass.ShapeFieldName;

		public IField AreaField => new ArcField(_aoFeatureClass.AreaField);

		public IField LengthField => new ArcField(_aoFeatureClass.LengthField);

		public IFeatureDataset FeatureDataset => _aoFeatureClass.FeatureDataset;

		public int FeatureClassID => _aoFeatureClass.FeatureClassID;

		public IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role)
		{
			var enumRelClasses =
				_aoFeatureClass.get_RelationshipClasses((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriRelRole)role);

			enumRelClasses.Reset();

			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass aoRelClass = null;

			while ((aoRelClass = enumRelClasses.Next()) != null)
			{
				yield return new ArcRelationshipClass(aoRelClass);
			}
		}

		#endregion

		#region Implementation of ISubtypes

		public bool HasSubtype => _aoSubtypes.HasSubtype;

		public int DefaultSubtypeCode
		{
			get => _aoSubtypes.DefaultSubtypeCode;
			set => _aoSubtypes.DefaultSubtypeCode = value;
		}

		public object get_DefaultValue(int SubtypeCode, string FieldName)
		{
			return _aoSubtypes.get_DefaultValue(SubtypeCode, FieldName);
		}

		public void set_DefaultValue(int SubtypeCode, string FieldName, object Value)
		{
			_aoSubtypes.set_DefaultValue(SubtypeCode, FieldName, Value);
		}

		public IDomain get_Domain(int SubtypeCode, string FieldName)
		{
			EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDomain aoDomain = _aoSubtypes.get_Domain(SubtypeCode, FieldName);
			return new ArcDomain(aoDomain);
		}

		//public void set_Domain(int SubtypeCode, string FieldName, IDomain Domain)
		//{
		//	_aoSubtypes.set_Domain(SubtypeCode, FieldName, Domain);
		//}

		public string SubtypeFieldName
		{
			get => _aoSubtypes.SubtypeFieldName;
			set => _aoSubtypes.SubtypeFieldName = value;
		}

		public int SubtypeFieldIndex => _aoSubtypes.SubtypeFieldIndex;

		public string get_SubtypeName(int SubtypeCode)
		{
			return _aoSubtypes.get_SubtypeName(SubtypeCode);
		}

		public IEnumerable<KeyValuePair<int, string>> Subtypes
		{
			get
			{
				var enumSubtypes = _aoSubtypes.Subtypes;

				enumSubtypes.Reset();

				int subtypeCode;
				string subtypeName = enumSubtypes.Next(out subtypeCode);
				while (subtypeName != null)
				{
					yield return new KeyValuePair<int, string>(subtypeCode, subtypeName);

					subtypeName = enumSubtypes.Next(out subtypeCode);
				}
			}
		}

		public void AddSubtype(int SubtypeCode, string SubtypeName)
		{
			_aoSubtypes.AddSubtype(SubtypeCode, SubtypeName);
		}

		public void DeleteSubtype(int SubtypeCode)
		{
			_aoSubtypes.DeleteSubtype(SubtypeCode);
		}

		#endregion

	}
}
