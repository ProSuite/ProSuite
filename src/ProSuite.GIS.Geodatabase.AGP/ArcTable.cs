using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcTable : ITable, IObjectClass, ISubtypes
	{
		public ArcTable(Table proTable)
		{
			ProTable = proTable;
			ProTableDefinition = proTable.GetDefinition();
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
			throw new NotImplementedException();
		}

		void IClass.DeleteField(IField field)
		{
			throw new NotImplementedException();
		}

		public IRow CreateRow(int? subtypeCode = null)
		{
			if (subtypeCode == null)
			{
				subtypeCode = DatasetUtils.GetDefaultSubtypeCode(ProTableDefinition);
			}

			Subtype subtype = DatasetUtils.GetSubtype(ProTableDefinition, subtypeCode);

			RowBuffer rowBuffer = ProTable.CreateRowBuffer(subtype);

			GdbObjectUtils.SetNullValuesToGdbDefault(
				rowBuffer, ProTableDefinition, subtype);

			if (ProTable is FeatureClass fc)
			{
				// TODO: Move to GeometryFactory
				ArcGIS.Core.Geometry.Geometry geometry;
				switch (fc.GetShapeType())
				{
					case GeometryType.Point:
						geometry = new MapPointBuilder().ToGeometry();
						break;
					case GeometryType.Polyline:
						geometry = new PolylineBuilder().ToGeometry();
						break;
					case GeometryType.Polygon:
						geometry = new PolygonBuilder().ToGeometry();
						break;
					case GeometryType.Multipoint:
						geometry = new MultipointBuilder().ToGeometry();
						break;
					case GeometryType.Multipatch:
						geometry = new MultipatchBuilderEx().ToGeometry();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				FeatureClassDefinition classDefinition = fc.GetDefinition();

				geometry =
					GeometryUtils.EnsureGeometrySchema(geometry, classDefinition.HasZ(),
					                                   classDefinition.HasM());

				rowBuffer[fc.GetDefinition().GetShapeField()] = geometry;
			}

			Row proRow = ProTable.CreateRow(rowBuffer);

			return ArcGeodatabaseUtils.ToArcRow(proRow);
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
					return ArcGeodatabaseUtils.ToArcRow(rowCursor.Current);
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
					yield return ArcGeodatabaseUtils.ToArcRow(rowCursor.Current, this);
				}
			}
		}

		public IRowBuffer CreateRowBuffer()
		{
			throw new NotImplementedException();
		}

		public void UpdateSearchedRows(IQueryFilter queryFilter, IRowBuffer buffer)
		{
			throw new NotImplementedException();
		}

		public void DeleteSearchedRows(IQueryFilter queryFilter)
		{
			throw new NotImplementedException();
		}

		public long RowCount(IQueryFilter queryFilter)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			return ProTable.GetCount(proQueryFilter);
		}

		public IEnumerable<IRow> Search(IQueryFilter queryFilter, bool recycling)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			RowCursor cursor = ProTable.Search(proQueryFilter, recycling);

			return ArcGeodatabaseUtils.GetArcRows(cursor, this);
		}

		public IEnumerable<IRow> Update(IQueryFilter queryFilter, bool recycling)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IRow> Insert(bool useBuffering)
		{
			throw new NotImplementedException();
		}

		public ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(queryFilter);

			Selection selectionSet = ProTable.Select(proQueryFilter,
			                                         (SelectionType) selType,
			                                         (SelectionOption) selOption);

			return new ArcSelectionSet(selectionSet, ProTable);
		}

		public object NativeImplementation => ProTable;

		public IFields Fields => new ArcFields(ProTableDefinition.GetFields(),
		                                       this is ArcFeatureClass fc
			                                       ? fc.GeometryDefinition
			                                       : null);

		public bool HasOID => ProTableDefinition.HasObjectID();

		public string OIDFieldName => ProTableDefinition.GetObjectIDField();

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
				string thisTableName = ProTableDefinition.GetName();

				if (role == esriRelRole.esriRelRoleAny &&
				    (relClassDef.GetOriginClass() == thisTableName ||
				     relClassDef.GetDestinationClass() == thisTableName))
				{
					yield return CreateArcRelationshipClass(geodatabase, relClassName);
				}

				if (role == esriRelRole.esriRelRoleOrigin &&
				    relClassDef.GetOriginClass() == thisTableName)
				{
					yield return CreateArcRelationshipClass(geodatabase, relClassName);
				}

				if (role == esriRelRole.esriRelRoleDestination &&
				    relClassDef.GetDestinationClass() == thisTableName)
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

		public esriDatasetType Type => esriDatasetType.esriDTTable;

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

				return ArcWorkspace.Create(geodatabase);
			}
		}

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

			Subtype subtype =
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

			Subtype subtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			Domain proDomain = field.GetDomain(subtype);

			return ArcGeodatabaseUtils.ToArcDomain(proDomain);
		}

		public string SubtypeFieldName
		{
			get => ProTableDefinition.GetSubtypeField();
			set => throw new NotImplementedException();
		}

		public int SubtypeFieldIndex =>
			ProTableDefinition.FindField(ProTableDefinition.GetSubtypeField());

		public string get_SubtypeName(int subtypeCode)
		{
			Subtype subtype =
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

		private static QueryFilter GetProQueryFilter(IQueryFilter queryFilter)
		{
			QueryFilter proQueryFilter;

			if (queryFilter is ArcQueryFilter arcQueryFilter)
			{
				// Wrapper only:
				return arcQueryFilter.ProQueryFilter;
			}

			if (queryFilter is ISpatialFilter fcFilter)
			{
				proQueryFilter = new SpatialQueryFilter()
				                 {
					                 SpatialRelationship =
						                 (SpatialRelationship) fcFilter.SpatialRel,
					                 FilterGeometry =
						                 ArcGeometryUtils.CreateProGeometry(fcFilter.Geometry),
					                 SpatialRelationshipDescription = fcFilter.SpatialRelDescription
				                 };
			}
			else
			{
				proQueryFilter = new QueryFilter();
			}

			proQueryFilter.SubFields = queryFilter.SubFields;
			proQueryFilter.WhereClause = queryFilter.WhereClause;
			proQueryFilter.PostfixClause = queryFilter.PostfixClause;

			if (proQueryFilter == null)
			{
				throw new ArgumentException("Unknown filter type");
			}

			return proQueryFilter;
		}

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
