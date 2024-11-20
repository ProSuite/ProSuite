using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcFeatureClass : ArcTable, ITable, IFeatureClass
	{
		private readonly FeatureClass _proFeatureClass;

		private readonly FeatureClassDefinition _proFeatureClassDefinition;

		public ArcFeatureClass(FeatureClass proFeatureClass) : base(proFeatureClass)
		{
			_proFeatureClass = proFeatureClass;
			_proFeatureClassDefinition = proFeatureClass.GetDefinition();
			GeometryDefinition =
				new ArcGeometryDef(new ShapeDescription(_proFeatureClassDefinition));
		}

		public IGeometryDef GeometryDefinition { get; }

		#region Implementation of IClass

		int IClass.FindField(string name)
		{
			return FindField(name);
		}

		//public void AddField(IField field)
		//{
		//	_aoTable.AddField(field);
		//}

		//public void DeleteField(IField Field)
		//{
		//	_aoTable.DeleteField(Field);
		//}

		//public void AddIndex(IIndex Index)
		//{
		//	_aoTable.AddIndex(Index);
		//}

		//public void DeleteIndex(IIndex Index)
		//{
		//	_aoTable.DeleteIndex(Index);
		//}

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
			foreach (var row in Update(filter, recycling))
			{
				yield return (IFeature) row;
			}
		}

		IEnumerable<IFeature> IFeatureClass.Insert(bool useBuffering)
		{
			foreach (IRow row in Insert(useBuffering))
			{
				yield return (IFeature) row;
			}
		}

		IEnumerable<IFeature> IFeatureClass.Search(IQueryFilter filter, bool recycling)
		{
			foreach (IRow row in Search(filter, recycling))
			{
				yield return (IFeature) row;
			}
		}

		IEnumerable<IRow> ITable.Update(IQueryFilter queryFilter, bool recycling)
		{
			foreach (IRow row in Update(queryFilter, recycling))
			{
				yield return (IFeature) row;
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
			return (IFeature) CreateRow();
		}

		public IFeature GetFeature(long id)
		{
			return (IFeature) GetRow(id);
		}

		//public IFeatureCursor GetFeatures(object fids, bool recycling)
		//{
		//	return _aoFeatureClass.GetFeatures(fids, recycling);
		//}

		//public IFeatureBuffer CreateFeatureBuffer()
		//{
		//	return _aoFeatureClass.CreateFeatureBuffer();
		//}

		public long FeatureCount(IQueryFilter queryFilter)
		{
			return RowCount(queryFilter);
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
			throw new NotImplementedException();
			//ArcField arcField = (ArcField)field;

			//Field proField = arcField.ProField;

			//_aoFeatureClass.AddField(proField);
		}

		void IClass.DeleteField(IField field)
		{
			throw new NotImplementedException();

			//ArcField arcField = (ArcField)field;
			//_aoFeatureClass.DeleteField(arcField.ProField);
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

		public esriGeometryType ShapeType
		{
			get
			{
				GeometryType coreGeometryType = _proFeatureClassDefinition.GetShapeType();

				ProSuiteGeometryType geometryType =
					GeometryUtils.TranslateToProSuiteGeometryType(coreGeometryType);

				return (esriGeometryType) geometryType;
			}
		}

		//public esriFeatureType FeatureType => _proFeatureClass.FeatureType;

		public string ShapeFieldName => _proFeatureClassDefinition.GetShapeField();

		public IField AreaField => TryGetField(_proFeatureClassDefinition.GetAreaField());

		public IField LengthField => TryGetField(_proFeatureClassDefinition.GetLengthField());

		//public IFeatureDataset FeatureDataset => _proFeatureClass.FeatureDataset;

		public long FeatureClassID => _proFeatureClass.GetID();

		//public IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role)
		//{
		//	var enumRelClasses =
		//		_proFeatureClass.get_RelationshipClasses((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriRelRole)role);

		//	enumRelClasses.Reset();

		//	EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass aoRelClass = null;

		//	while ((aoRelClass = enumRelClasses.Next()) != null)
		//	{
		//		yield return new ArcRelationshipClass(aoRelClass);
		//	}
		//}

		#endregion

		#region Implementation of IGeoDataset

		public ISpatialReference SpatialReference =>
			new ArcSpatialReference(_proFeatureClassDefinition.GetSpatialReference());

		public IEnvelope Extent => new ArcEnvelope(_proFeatureClassDefinition.GetExtent());

		#endregion

		private ArcField TryGetField(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				return null;
			}

			Field field =
				_proFeatureClassDefinition.GetFields()
				                          .FirstOrDefault(
					                          f => f.Name.Equals(fieldName));
			if (field == null)
			{
				return null;
			}

			return new ArcField(field);
		}
	}
}
