using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using IDatasetContainer = ProSuite.Commons.GeoDb.IDatasetContainer;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyTable : ITableData, IReadOnlyTable, ISubtypes
	{
		private static readonly bool _provideFailingOidInException =
			EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_DATA_ACCESS_ERROR_WITH_OID");

		protected static ReadOnlyTable CreateReadOnlyTable(ITable table)
		{
			return new ReadOnlyTable(table);
		}

		// cache name for debugging purposes (avoid all ArcObjects threading issues)
		private readonly string _name;
		private int _oidFieldIndex = -1;

		protected ReadOnlyTable([NotNull] ITable table)
		{
			BaseTable = table;
			_name = DatasetUtils.GetName(BaseTable);
		}

		public override string ToString() => $"{_name} ({GetType().Name})";

		public string AlternateOidFieldName { get; set; }

		public ITable BaseTable { get; }

		protected ITable Table => BaseTable;

		IName IReadOnlyDataset.FullName => ((IDataset) BaseTable).FullName;
		IWorkspace IReadOnlyDataset.Workspace => ((IDataset) BaseTable).Workspace;
		public string Name => DatasetUtils.GetName(BaseTable);
		public IFields Fields => BaseTable.Fields;

		public IReadOnlyRow GetRow(long oid)
		{
			try
			{
#if Server11
				return CreateRow(BaseTable.GetRow(oid));
#else
				return CreateRow(BaseTable.GetRow((int) oid));
#endif
			}
			catch (Exception e)
			{
				string tableName = DatasetUtils.GetName(BaseTable);
				throw new DataAccessException($"Error getting {tableName} <oid> {oid}", oid,
				                              tableName, e);
			}
		}

		public long RowCount(ITableFilter filter) =>
			BaseTable.RowCount(TableFilterUtils.GetQueryFilter(filter, BaseTable as IFeatureClass));

		public virtual int FindField(string name) => BaseTable.FindField(name);

		public bool HasOID => AlternateOidFieldName != null || BaseTable.HasOID;

		public string OIDFieldName => AlternateOidFieldName ?? BaseTable.OIDFieldName;

		public IReadOnlyRow GetRow(int oid)
		{
			IRow row = AlternateOidFieldName != null
				           ? GdbQueryUtils.GetObject((IObjectClass) BaseTable, oid,
				                                     AlternateOidFieldName)
				           : BaseTable.GetRow(oid);

			return CreateRow(row);
		}

		public bool Equals(IReadOnlyTable otherTable)
		{
			return Equals((object) otherTable);
		}

		public virtual ReadOnlyRow CreateRow(IRow row)
		{
			return new ReadOnlyRow(this, row);
		}

		public IEnumerable<IReadOnlyRow> EnumRows(ITableFilter filter, bool recycle)
		{
			if (BaseTable is IReadOnlyTable roTable)
			{
				foreach (IReadOnlyRow readOnlyRow in roTable.EnumRows(filter, recycle))
				{
					IRow baseRow = (IRow) readOnlyRow;
					yield return CreateRow(baseRow); // TODO : is this necessary
				}
			}
			else
			{
				var queryFilter =
					TableFilterUtils.GetQueryFilter(filter, BaseTable as IFeatureClass);

				bool withOidInException =
					_provideFailingOidInException ||
					(BaseTable is IFeatureClass featureClass &&
					 featureClass.ShapeType == esriGeometryType.esriGeometryMultiPatch);

				foreach (var row in new EnumCursor(BaseTable, queryFilter, recycle,
				                                   withOidInException))
				{
					yield return CreateRow(row);
				}
			}
		}

		private ISubtypes _subtypes => BaseTable as ISubtypes;

		void ISubtypes.AddSubtype(int SubtypeCode, string SubtypeName)
		{
			_subtypes.AddSubtype(SubtypeCode, SubtypeName);
		}

		void ISubtypes.DeleteSubtype(int SubtypeCode)
		{
			_subtypes.DeleteSubtype(SubtypeCode);
		}

		bool ISubtypes.HasSubtype => _subtypes?.HasSubtype ?? false;

		int ISubtypes.DefaultSubtypeCode
		{
			get => _subtypes.DefaultSubtypeCode;
			set => _subtypes.DefaultSubtypeCode = value;
		}

		object ISubtypes.get_DefaultValue(int subtypeCode, string fieldName)
			=> _subtypes.DefaultValue[subtypeCode, fieldName];

		void ISubtypes.set_DefaultValue(int subtypeCode, string fieldName, object value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		IDomain ISubtypes.get_Domain(int subtypeCode, string fieldName)
			=> _subtypes.Domain[subtypeCode, fieldName];

		void ISubtypes.set_Domain(int subtypeCode, string fieldName, IDomain value)
			=> _subtypes.DefaultValue[subtypeCode, fieldName] = value;

		string ISubtypes.SubtypeFieldName
		{
			get => _subtypes.SubtypeFieldName;
			set => _subtypes.SubtypeFieldName = value;
		}

		int ISubtypes.SubtypeFieldIndex => _subtypes.SubtypeFieldIndex;

		string ISubtypes.get_SubtypeName(int subtypeCode)
			=> _subtypes.SubtypeName[subtypeCode];

		IEnumSubtype ISubtypes.Subtypes => _subtypes.Subtypes;

		#region Equality members

		public bool Equals(ReadOnlyTable other)
		{
			if (other == null)
			{
				return false;
			}

			// NOTE: Stick to the AO-equality logic of tables. The problem with anything else is
			// that the AO-workspace sometimes changes its hash-code!
			// -> never use workspaces in dictionaries!

			return BaseTable.Equals(other.BaseTable);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			if (ReferenceEquals(this, obj)) return true;

			if (BaseTable is IObjectClass thisClass && obj is IObjectClass objectClass)
			{
				return DatasetUtils.IsSameObjectClass(thisClass, objectClass);
			}

			if (obj is ReadOnlyTable roTable)
			{
				return Equals(roTable);
			}

			return false;
		}

		public override int GetHashCode()
		{
			// NOTE: Never make the AO-workspace part of the hash code calculation because it
			// has been observed to change its hash code! 
			return BaseTable.GetHashCode();
		}

		#endregion

		public long GetRowOid(IRow row)
		{
			return AlternateOidFieldName != null
				       ? Assert.NotNull(GdbObjectUtils.ReadRowOidValue(row, OidFieldIndex)).Value
				       : row.OID;
		}

		internal int OidFieldIndex
		{
			get
			{
				if (_oidFieldIndex < 0)
				{
					_oidFieldIndex = Table.FindField(OIDFieldName);
				}

				return _oidFieldIndex;
			}
		}

		#region Implementation of IDbDataset

		private IDatasetContainer _datasetContainer;

		IDatasetContainer IDatasetDef.DbContainer =>
			_datasetContainer ??
			(_datasetContainer = new GeoDbWorkspace(DatasetUtils.GetWorkspace(BaseTable)));

		DatasetType IDatasetDef.DatasetType =>
			((IDataset) BaseTable).Type == esriDatasetType.esriDTFeatureClass
				? DatasetType.FeatureClass
				: DatasetType.Table;

		bool IDatasetDef.Equals(IDatasetDef otherDataset)
		{
			return Equals(otherDataset);
		}

		#endregion

		#region Implementation of IDbTableSchema

		IReadOnlyList<ITableField> ITableSchemaDef.TableFields
		{
			get
			{
				// TODO: If this is heavily used, wrap IFields in separate object.
				return DatasetUtils.EnumFields(BaseTable.Fields)
				                   .Select(FieldUtils.ToTableField)
				                   .ToList();
			}
		}

		#endregion

		#region Implementation of IDbTable

		IDbRow ITableData.GetRow(long oid)
		{
			return GetRow(oid);
		}

		IEnumerable<IDbRow> ITableData.EnumRows(ITableFilter filter, bool recycle)
		{
			return EnumRows(filter, recycle);
		}

		#endregion
	}
}
