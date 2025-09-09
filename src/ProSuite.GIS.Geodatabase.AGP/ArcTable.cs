using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using Subtype = ProSuite.GIS.Geodatabase.API.Subtype;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcTable : ITable, IObjectClass, ISubtypes
	{
		private bool _cachePropertiesEagerly;

		private long? _workspaceHandle;

		// Property caching for non CIM-thread access:
		[CanBeNull] private bool? _hasOID;
		[CanBeNull] private string _oidFieldName;
		[CanBeNull] private long? _objectClassID;
		[CanBeNull] private string _name;
		[CanBeNull] private string _aliasName;
		[CanBeNull] private string _subtypeFieldName;
		[CanBeNull] private int? _defaultSubtypeCode;

		private ConcurrentDictionary<int, Subtype> _subtypes;

		private IFields _fields;

		public ArcTable([NotNull] Table proTable,
		                bool cachePropertiesEagerly = false)
		{
			ProTable = proTable;
			ProTableDefinition = proTable.GetDefinition();

			if (cachePropertiesEagerly)
			{
				CacheProperties();
			}
		}

		public Table ProTable { get; }

		public TableDefinition ProTableDefinition { get; }

		internal void CacheProperties()
		{
			if (_cachePropertiesEagerly)
			{
				// Already cached
				return;
			}

			_cachePropertiesEagerly = true;

			var geodatabase = ProTable.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

			if (geodatabase != null)
			{
				_workspaceHandle = geodatabase.Handle.ToInt64();
				ArcWorkspace.Create(geodatabase, true);
			}

			_hasOID = HasOID;
			_oidFieldName = OIDFieldName;
			_objectClassID = ObjectClassID;
			_aliasName = AliasName;
			_subtypeFieldName = SubtypeFieldName;
			_defaultSubtypeCode = DefaultSubtypeCode;

			IWorkspace workspace = Workspace;

			_fields = Fields;

			// Cache subtypes
			InitializeSubtypes();

			CachePropertiesCore();
		}

		protected internal virtual void CachePropertiesCore() { }

		#region Implementation of IClass

		public int FindField(string name)
		{
			return Fields.FindField(name);
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
			Row proRow = CreateProRow(subtypeCode);

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

		public long RowCount(ITableFilter filter)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(filter);

			return ProTable.GetCount(proQueryFilter);
		}

		public IEnumerable<IRow> Search(ITableFilter filter, bool recycling)
		{
			QueryFilter proQueryFilter = GetProQueryFilter(filter);

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

		public IFields Fields =>
			_fields ??= new ArcFields(ProTableDefinition.GetFields(),
			                          this is ArcFeatureClass fc ? fc.GeometryDefinition : null,
			                          Workspace as IFeatureWorkspace);

		public bool HasOID
		{
			get { return _hasOID ??= ProTableDefinition.HasObjectID(); }
		}

		public string OIDFieldName
		{
			get { return _oidFieldName ??= ProTableDefinition.GetObjectIDField(); }
		}

		public long ObjectClassID
		{
			get { return _objectClassID ??= ProTable.GetID(); }
		}

		public string AliasName
		{
			get
			{
				if (_aliasName == null)
				{
					try
					{
						string aliasName = ProTableDefinition.GetAliasName();

						_aliasName = StringUtils.IsNotEmpty(aliasName)
							             ? aliasName
							             : Name;
					}
					catch (NotImplementedException)
					{
						_aliasName = Name;
					}
				}

				return _aliasName;
			}
		}

		public IEnumerable<IRelationshipClass> get_RelationshipClasses(esriRelRole role)
		{
			if (Workspace == null)
			{
				yield break;
			}

			foreach (IDataset dataset in Workspace.get_Datasets(
				         esriDatasetType.esriDTRelationshipClass))
			{
				if (! (dataset is IRelationshipClass relClass))
				{
					throw new AssertionException("Unexpected dataset type.");
				}

				if (role == esriRelRole.esriRelRoleAny &&
				    (relClass.OriginClass.Equals(this) ||
				     relClass.DestinationClass.Equals(this)))
				{
					yield return PrepareCached(relClass);
				}

				if (role == esriRelRole.esriRelRoleOrigin &&
				    relClass.OriginClass.Equals(this))
				{
					yield return PrepareCached(relClass);
				}

				if (role == esriRelRole.esriRelRoleDestination &&
				    relClass.DestinationClass.Equals(this))
				{
					yield return PrepareCached(relClass);
				}
			}
		}

		public Row CreateProRow(int? subtypeCode)
		{
			subtypeCode ??= DatasetUtils.GetDefaultSubtypeCode(ProTableDefinition);

			ArcGIS.Core.Data.Subtype subtype =
				DatasetUtils.GetSubtype(ProTableDefinition, subtypeCode);

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
			return proRow;
		}

		private IRelationshipClass PrepareCached(IRelationshipClass relClass)
		{
			if (_cachePropertiesEagerly && relClass is ArcRelationshipClass arcRelClass)
			{
				arcRelClass.CacheProperties();
			}

			return relClass;
		}

		private IRelationshipClass CreateArcRelationshipClass(
			ArcGIS.Core.Data.Geodatabase geodatabase, string relClassName)
		{
			RelationshipClass relClass =
				geodatabase.OpenDataset<RelationshipClass>(relClassName);

			return ArcRelationshipClass.Create(relClass, _cachePropertiesEagerly);
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

		public string Name
		{
			get { return _name ??= ProTableDefinition.GetName(); }
		}

		public IName FullName => new ArcDatasetName(this);

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

		public IWorkspace Workspace
		{
			get
			{
				if (_workspaceHandle != null)
				{
					// This works in any thread:
					ArcWorkspace cachedWorkspace = ArcWorkspace.GetByHandle(_workspaceHandle.Value);

					if (cachedWorkspace != null)
					{
						return cachedWorkspace;
					}
				}

				// CIM thread is needed:
				var geodatabase = ProTable.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				if (geodatabase == null)
				{
					return null;
				}

				_workspaceHandle = geodatabase.Handle.ToInt64();

				return ArcWorkspace.Create(geodatabase);
			}
		}

		#endregion

		#region Implementation of ISubtypes

		public bool HasSubtype => SubtypeFieldIndex >= 0;

		public int DefaultSubtypeCode
		{
			get { return _defaultSubtypeCode ??= ProTableDefinition.GetDefaultSubtypeCode(); }
			set => _defaultSubtypeCode = value;
		}

		public object get_DefaultValue(int subtypeCode, string fieldName)
		{
			if (_subtypes != null && _subtypes.TryGetValue(subtypeCode, out var subtype))
			{
				return subtype.GetDefaultValue(fieldName);
			}

			Field field = GetExistingField(fieldName);

			ArcGIS.Core.Data.Subtype proSubtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			return field.GetDefaultValue(proSubtype);
		}

		public void set_DefaultValue(int subtypeCode, string fieldName, object value)
		{
			if (_subtypes != null)
			{
				_subtypes[subtypeCode].SetDefaultValue(fieldName, value);
				return;
			}

			throw new NotImplementedException();
		}

		public IDomain get_Domain(int subtypeCode, string fieldName)
		{
			if (_subtypes != null && _subtypes.TryGetValue(subtypeCode, out var subtype))
			{
				return subtype.GetAttributeDomain(fieldName);
			}

			Field field = GetExistingField(fieldName);

			ArcGIS.Core.Data.Subtype proSubtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			Domain proDomain = field.GetDomain(proSubtype);

			return ArcGeodatabaseUtils.ToArcDomain(proDomain, (IFeatureWorkspace) Workspace);
		}

		public string SubtypeFieldName
		{
			get
			{
				if (_subtypeFieldName == null)
				{
					try
					{
						_subtypeFieldName = ProTableDefinition.GetSubtypeField();
					}
					catch (Exception)
					{
						// TODO: Handle specific exception (shapefiles?)
						throw;
						//_subtypeFieldName = string.Empty;
					}
				}

				return _subtypeFieldName;
			}
			set => throw new NotImplementedException();
		}

		public int SubtypeFieldIndex => string.IsNullOrEmpty(SubtypeFieldName)
			                                ? -1
			                                : Fields.FindField(SubtypeFieldName);

		public string get_SubtypeName(int subtypeCode)
		{
			if (_subtypes != null && _subtypes.TryGetValue(subtypeCode, out var subtype))
			{
				return subtype.Name;
			}

			ArcGIS.Core.Data.Subtype proSubtype =
				ProTableDefinition.GetSubtypes()
				                  .FirstOrDefault(s => s.GetCode() == subtypeCode);

			return proSubtype?.GetName();
		}

		public IEnumerable<KeyValuePair<int, string>> Subtypes
		{
			get
			{
				if (_subtypes != null)
				{
					return _subtypes.Select(kvp => new KeyValuePair<int, string>(
						                        kvp.Key, kvp.Value.Name));
				}

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

		private void InitializeSubtypes()
		{
			_subtypes = new ConcurrentDictionary<int, Subtype>();

			if (string.IsNullOrEmpty(SubtypeFieldName))
			{
				return;
			}

			var proSubtypes = ProTableDefinition.GetSubtypes();

			foreach (var proSubtype in proSubtypes)
			{
				int code = proSubtype.GetCode();
				string name = proSubtype.GetName();

				var subtype = new Subtype(code, name);
				_subtypes[code] = subtype;

				// Cache default values and domains for each field
				foreach (Field field in ProTableDefinition.GetFields())
				{
					object defaultValue = field.GetDefaultValue(proSubtype);
					subtype.SetDefaultValue(field.Name, defaultValue);

					Domain proDomain = field.GetDomain(proSubtype);
					if (proDomain != null)
					{
						IDomain domain =
							ArcGeodatabaseUtils.ToArcDomain(
								proDomain, (IFeatureWorkspace) Workspace);
						subtype.SetAttributeDomain(field.Name, domain);
					}
				}
			}
		}

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

		private static QueryFilter GetProQueryFilter(ITableFilter queryFilter)
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
				                  .FirstOrDefault(f => f.Name.Equals(
					                                  fieldName,
					                                  StringComparison.CurrentCultureIgnoreCase));

			if (field == null)
				throw new ArgumentException($"Field {fieldName} does not exist in {Name}");
			return field;
		}
	}

	public class ArcDatasetName : IDatasetName
	{
		private readonly IDataset _dataset;

		public ArcDatasetName(IDataset dataset)
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

		#region Implementation of IDatasetName

		public string Name => _dataset.Name;

		public esriDatasetType Type => _dataset.Type;

		public IWorkspaceName WorkspaceName =>
			new ArcWorkspaceName((ArcWorkspace) _dataset.Workspace);

		#endregion
	}

	public class ArcTableDefinitionName : IDatasetName
	{
		private readonly TableDefinition _tableDefinition;
		private readonly IFeatureWorkspace _workspace;

		public ArcTableDefinitionName(TableDefinition tableDefinition,
		                              IFeatureWorkspace workspace)
		{
			_tableDefinition = tableDefinition;
			_workspace = workspace;
		}

		#region Implementation of IName

		public string NameString
		{
			get => Name;
			set => throw new NotImplementedException();
		}

		public object Open()
		{
			return _workspace.OpenFeatureClass(Name);
		}

		#endregion

		#region Implementation of IDatasetName

		public string Name => _tableDefinition.GetName();

		public esriDatasetType Type => _tableDefinition is FeatureClassDefinition
			                               ? esriDatasetType.esriDTFeatureClass
			                               : esriDatasetType.esriDTTable;

		public IWorkspaceName WorkspaceName => _workspace.GetWorkspaceName();

		#endregion
	}

	public class ArcRelationshipClassDefinitionName : IDatasetName
	{
		private readonly RelationshipClassDefinition _relClassDef;
		private readonly IFeatureWorkspace _workspace;

		public ArcRelationshipClassDefinitionName(RelationshipClassDefinition relClassDef,
		                                          IFeatureWorkspace workspace)
		{
			_relClassDef = relClassDef;
			_workspace = workspace;
		}

		#region Implementation of IName

		public string NameString
		{
			get => Name;
			set => throw new NotImplementedException();
		}

		public object Open()
		{
			return _workspace.OpenRelationshipClass(Name);
		}

		#endregion

		#region Implementation of IDatasetName

		public string Name => _relClassDef.GetName();

		public esriDatasetType Type => esriDatasetType.esriDTRelationshipClass;

		public IWorkspaceName WorkspaceName => _workspace.GetWorkspaceName();

		#endregion
	}
}
