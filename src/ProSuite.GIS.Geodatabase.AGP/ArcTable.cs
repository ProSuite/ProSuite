using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcTable : ITable, IObjectClass, ISubtypes
	{
		//private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObjectClass _aoObjectClass;
		//private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset _aoDataset;

		public ArcTable(Table proTable)
		{
			ProTable = proTable;
			ProTableDefinition = proTable.GetDefinition();

			//_aoObjectClass = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObjectClass)table;
			//_aoDataset = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset)table;
		}

		public Table ProTable { get; }

		public TableDefinition ProTableDefinition { get; }

		#region Implementation of IClass

		public int FindField(string name)
		{
			return ProTableDefinition.FindField(name);
		}

		void IClass.AddField(IField field)
		{
			throw new AbandonedMutexException();
			//ArcField arcField = (ArcField)field;

			//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField = arcField.ProField;
			//_table.AddField(aoField);
		}

		void IClass.DeleteField(IField field)
		{
			throw new AbandonedMutexException();
			//ArcField arcField = (ArcField)field;
			//_table.DeleteField(arcField.ProField);
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
			RowBuffer rowBuffer = ProTable.CreateRowBuffer();
			Row proRow = ProTable.CreateRow(rowBuffer);

			return ArcUtils.ToArcRow(proRow);
		}

		public IRow GetRow(long oid)
		{
			IReadOnlyList<long> objectIdList = new[] { oid };

			QueryFilter queryFilter = new QueryFilter()
			                          {
				                          ObjectIDs = objectIdList
			                          };

			const bool recycling = false;
			using (RowCursor rowCursor = ProTable.Search(queryFilter, recycling))
			{
				while (rowCursor.MoveNext())
				{
					return ArcUtils.ToArcRow(rowCursor.Current);
				}
			}

			// TODO: Type of exception?
			throw new InvalidOperationException($"No row found with OID {oid}");
		}

		public IEnumerable<IRow> GetRows(object oids, bool recycling)
		{
			if (! (oids is IEnumerable<long> oidList))
			{
				throw new InvalidOperationException(
					$"Cannot convert oids ({oids})to IEnumerable<long>");
			}

			QueryFilter queryFilter = new QueryFilter()
			                          {
				                          ObjectIDs = oidList.ToList()
			                          };

			using (RowCursor rowCursor = ProTable.Search(queryFilter))
			{
				while (rowCursor.MoveNext())
				{
					yield return ArcUtils.ToArcRow(rowCursor.Current, this);
				}
			}
		}

		public IRowBuffer CreateRowBuffer()
		{
			throw new NotImplementedException();
			//return _aoTable.CreateRowBuffer();
		}

		public void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer)
		{
			throw new NotImplementedException();
			//var arcQueryFilter = (ArcQueryFilter) queryFilter;
			//var arcRow = (ArcRow) buffer;
			//_proTable.UpdateSearchedRows(arcQueryFilter.ProQueryFilter, arcRow.ProRow);
		}

		public void DeleteSearchedRows(IQueryFilter queryFilter)
		{
			throw new NotImplementedException();
			//var arcQueryFilter = (ArcQueryFilter) queryFilter;
			//_proTable.DeleteSearchedRows(arcQueryFilter.ProQueryFilter);
		}

		public long RowCount(IQueryFilter queryFilter)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			return ProTable.GetCount(proQueryFilter);
		}

		private static QueryFilter GetProQueryFilter(IQueryFilter queryFilter)
		{
			QueryFilter proQueryFilter;

			if (queryFilter is ArcQueryFilter arcQueryFilter)
			{
				proQueryFilter = arcQueryFilter.ProQueryFilter;
			}
			else if (queryFilter is TableFilter tableFilter)
			{
				proQueryFilter = new QueryFilter()
				                 {
					                 SubFields = tableFilter.SubFields,
					                 WhereClause = tableFilter.WhereClause,
					                 PostfixClause = tableFilter.PostfixClause,
					                 //OutputSpatialReference = tableFilter.OutputSpatialReference
				                 };
			}
			else
			{
				throw new ArgumentException("Unknown filter type");
			}

			return proQueryFilter;
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
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			RowCursor cursor = ProTable.Search(proQueryFilter, recycling);

			return ArcUtils.GetArcRows(cursor, this);
			//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRow row;

			//while ((row = cursor.NextRow()) != null)
			//{
			//	yield return ToArcObject(row);
			//}
		}

		public IEnumerable<IRow> Update(IQueryFilter queryFilter, bool recycling)
		{
			throw new NotImplementedException();
			//var arcQueryFilter = (ArcQueryFilter) queryFilter;

			//ICursor cursor = _proTable.Update(arcQueryFilter.AoQueryFilter, recycling);

			//return ArcUtils.GetArcRows(cursor);
		}

		public IEnumerable<IRow> Insert(bool useBuffering)
		{
			throw new NotImplementedException();

			//ICursor cursor = _proTable.Insert(useBuffering);

			//return ArcUtils.GetArcRows(cursor);
		}

		public ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			ArcWorkspace arcWorkspace = (ArcWorkspace) selectionContainer;

			Selection selectionSet = ProTable.Select(proQueryFilter,
			                                         (SelectionType) selType,
			                                         (SelectionOption) selOption);

			return new ArcSelectionSet(selectionSet, ProTable);
		}

		public object NativeImplementation => ProTable;

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

		public IFields Fields => new ArcFields(ProTableDefinition.GetFields(),
		                                       this is ArcFeatureClass fc
			                                       ? fc.GeometryDefinition
			                                       : null);

		//public IIndexes Indexes => ((IClass)_aoTable).Indexes;

		public bool HasOID => ProTableDefinition.HasObjectID();

		public string OIDFieldName => ProTableDefinition.GetObjectIDField();

		//public UID CLSID => _aoTable.CLSID;

		//public UID EXTCLSID => _aoTable.EXTCLSID;

		//public object Extension => _aoTable.Extension;

		public long ObjectClassID => ProTable.GetID();

		public string AliasName
		{
			get
			{
				try
				{
					string aliasName = ProTableDefinition.GetAliasName();

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
			var geodatabase = ProTable.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

			if (geodatabase == null)
			{
				yield break;
			}

			foreach (var relClassDef in geodatabase.GetDefinitions<RelationshipClassDefinition>())
			{
				string relClassName = relClassDef.GetName();

				if (role == esriRelRole.esriRelRoleAny)
				{
					yield return CreateArcRelationshipClass(geodatabase, relClassName);
				}

				if (role == esriRelRole.esriRelRoleOrigin &&
				    relClassDef.GetOriginClass() == ProTableDefinition.GetName())
				{
					yield return CreateArcRelationshipClass(geodatabase, relClassName);
				}

				if (role == esriRelRole.esriRelRoleDestination &&
				    relClassDef.GetDestinationClass() == ProTableDefinition.GetName())
				{
					yield return CreateArcRelationshipClass(geodatabase, relClassName);
				}
			}
		}

		private static IRelationshipClass CreateArcRelationshipClass(
			ArcGIS.Core.Data.Geodatabase geodatabase, string relClassName)
		{
			RelationshipClass relClass =
				geodatabase.OpenDataset<RelationshipClass>(relClassName);

			return new ArcRelationshipClass(relClass);
		}

		//public IPropertySet ExtensionProperties => _aoTable.ExtensionProperties;

		#endregion

		#region Implementation of IDataset

		public bool CanCopy()
		{
			return false;
		}

		public bool CanDelete()
		{
			return false;
		}

		public void Delete()
		{
			throw new NotImplementedException();
		}

		public bool CanRename()
		{
			return false;
		}

		public void Rename(string name)
		{
			throw new NotImplementedException();
		}

		public string Name => ProTableDefinition.GetName();

		public IName FullName => new ArcName(this);

		public string BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => (esriDatasetType) esriDatasetType.esriDTTable;

		public string Category => throw new NotImplementedException();

		public IEnumerable<IDataset> Subsets
		{
			get { yield break; }
		}

		IWorkspace IDataset.Workspace
		{
			get
			{
				var geodatabase = ProTable.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				if (geodatabase == null)
				{
					return null;
				}

				return new ArcWorkspace(geodatabase);
			}
		}
		//public IWorkspace Workspace => new ArcWorkspace(_aoDataset.Workspace);

		//public IPropertySet PropertySet => _aoDataset.PropertySet;

		//public IDataset Copy(string copyName, IWorkspace copyWorkspace)
		//{
		//	return _aoDataset.Copy(copyName, copyWorkspace);
		//}

		#endregion

		#region Implementation of ISubtypes

		public bool HasSubtype => ProTableDefinition.GetSubtypes().Count > 0;

		public int DefaultSubtypeCode
		{
			get => ProTableDefinition.GetDefaultSubtypeCode();
			set => throw new NotImplementedException();
		}

		public object get_DefaultValue(int subtypeCode, string fieldName)
		{
			Field field = GetExistingField(fieldName);

			ArcGIS.Core.Data.Subtype subtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			return field.GetDefaultValue(subtype);
		}

		public void set_DefaultValue(int subtypeCode, string fieldName, object value)
		{
			throw new NotImplementedException();
		}

		public IDomain get_Domain(int subtypeCode, string fieldName)
		{
			Field field = GetExistingField(fieldName);

			ArcGIS.Core.Data.Subtype subtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			Domain proDomain = field.GetDomain(subtype);

			return new ArcDomain(proDomain);
		}

		//public void set_Domain(int SubtypeCode, string FieldName, IDomain Domain)
		//{
		//	_aoSubtypes.set_Domain(SubtypeCode, FieldName, Domain);
		//}

		public string SubtypeFieldName
		{
			get => ProTableDefinition.GetSubtypeField();
			set => throw new NotImplementedException();
		}

		public int SubtypeFieldIndex =>
			ProTableDefinition.FindField(ProTableDefinition.GetSubtypeField());

		public string get_SubtypeName(int subtypeCode)
		{
			ArcGIS.Core.Data.Subtype subtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			return subtype?.GetName();
		}

		public IEnumerable<KeyValuePair<int, string>> Subtypes
		{
			get
			{
				return ProTableDefinition.GetSubtypes()
				                         .Select(s => new KeyValuePair<int, string>(
					                                 s.GetCode(), s.GetName()));
			}
		}

		public void AddSubtype(int subtypeCode, string subtypeName)
		{
			throw new NotImplementedException();
		}

		public void DeleteSubtype(int subtypeCode)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Equality members

		// TODO: Consider implementing operator == / !=

		public bool Equals(ArcTable other)
		{
			if (other == null)
			{
				return false;
			}

			return ProTable.Handle.Equals(other.ProTable.Handle);
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other)) return false;

			if (ReferenceEquals(this, other)) return true;

			if (other is ArcTable arcTable)
			{
				return Equals(arcTable);
			}

			if (other is Table proTable)
			{
				return ProTable.Handle.Equals(proTable.Handle);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return ProTable.Handle.GetHashCode();
		}

		#endregion

		private Field GetExistingField(string fieldName)
		{
			Field field =
				ProTableDefinition.GetFields()
				                  .FirstOrDefault(
					                  f => f.Name.Equals(
						                  fieldName,
						                  StringComparison.CurrentCultureIgnoreCase));

			if (field == null)
				throw new ArgumentException($"Field {fieldName} does not exist in {Name}");
			return field;
		}
	}

	public class ArcName : IName
	{
		private readonly IDataset _dataset;

		public ArcName(IDataset dataset)
		{
			_dataset = dataset;
		}

		#region Implementation of IName

		public string NameString
		{
			get => _dataset.Name;
			set => throw new NotImplementedException();
		}

		public object Open()
		{
			return _dataset;
		}

		#endregion
	}
}
