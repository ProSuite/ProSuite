using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	public class RemoteDataset : BackingDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly GdbTable _schema;
		[CanBeNull] private readonly ClassDef _classDefinition;
		[CanBeNull] private readonly RelationshipClassQuery _queryDefinition;

		private IEnvelope _extent;

		// TODO: use GdbTable instead of ITable in Constr
		public RemoteDataset(
			[NotNull] ITable schema,
			[NotNull] Func<DataVerificationResponse, DataVerificationRequest> getRemoteDataFunc,
			[CanBeNull] ClassDef classDefinition,
			[CanBeNull] RelationshipClassQuery queryDefinition = null)
		{
			Assert.True(classDefinition != null | queryDefinition != null,
			            "Either the class definition or the query definition must be provided.");

			_schema = (GdbTable) schema;
			_classDefinition = classDefinition;
			_queryDefinition = queryDefinition;

			GetData = getRemoteDataFunc;
		}

		public Func<DataVerificationResponse, DataVerificationRequest> GetData { get; set; }

		public override IEnvelope Extent
		{
			get
			{
				if (_extent == null)
				{
					_extent = GetExtent();
				}

				return _extent;
			}
		}

		public override VirtualRow GetRow(long id)
		{
			Assert.True(_schema.HasOID, "The table {0} has no OID", _schema.Name);
			Assert.False(string.IsNullOrEmpty(_schema.OIDFieldName),
			             "The table {0} has no OID Field Name", _schema.Name);

			DataVerificationResponse response =
				new DataVerificationResponse
				{
					DataRequest = new DataRequest
					              {
						              WhereClause = $"{_schema.OIDFieldName} = {id}"
					              }
				};

			if (_queryDefinition != null)
			{
				response.DataRequest.RelQueryDef = _queryDefinition;
			}
			else
			{
				response.DataRequest.ClassDef = Assert.NotNull(_classDefinition);
			}

			DataVerificationRequest moreData = GetData(response);

			GdbData gdbData = ConfirmDataReceived(moreData, response.DataRequest);

			foreach (GdbObjectMsg gdbObjMsg in gdbData.GdbObjects)
			{
				return ProtobufConversionUtils.FromGdbObjectMsg(gdbObjMsg, _schema);
			}

			// or better: COMException?
			return null;
		}

		public override long GetRowCount(ITableFilter filter)
		{
			DataRequest dataRequest = CreateDataRequest(filter);

			dataRequest.CountOnly = true;

			DataVerificationResponse response =
				new DataVerificationResponse
				{
					DataRequest = dataRequest
				};

			DataVerificationRequest moreData = GetData(response);

			GdbData gdbData = ConfirmDataReceived(moreData, dataRequest);

			return gdbData.GdbObjectCount;
		}

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			DataRequest dataRequest = CreateDataRequest(filter);

			DataVerificationResponse response =
				new DataVerificationResponse
				{
					DataRequest = dataRequest
				};

			Stopwatch watch = Stopwatch.StartNew();

			DataVerificationRequest moreData = GetData(response);

			GdbData gdbData = ConfirmDataReceived(moreData, dataRequest);

			_msg.DebugStopTiming(watch, "Received {0} objects", gdbData.GdbObjects.Count);
			watch.Restart();

			if (gdbData.GdbColumnarData != null)
			{
				// Column-based data was provided:
				foreach (var row in ProcessColumnarData(gdbData.GdbColumnarData))
				{
					yield return row;
				}

				yield break;
			}

			// Row-based data was provided:
			RepeatedField<GdbObjectMsg> foundGdbObjects = gdbData.GdbObjects;

			if (foundGdbObjects == null || foundGdbObjects.Count == 0)
			{
				yield break;
			}

			string subFields = filter?.SubFields;
			List<int> fieldIndexes = null;
			if (! string.IsNullOrEmpty(subFields) && subFields != "*")
			{
				fieldIndexes = GetFieldsIndexes(((ITableSchemaDef) _schema).TableFields,
				                                StringUtils.SplitAndTrim(subFields, ','));
			}

			foreach (GdbObjectMsg gdbObjMsg in foundGdbObjects)
			{
				yield return ProtobufConversionUtils.FromGdbObjectMsg(
					gdbObjMsg, _schema, fieldIndexes);
			}

			_msg.DebugStopTiming(watch, "Unpacked and yielded {0} objects",
			                     gdbData.GdbObjects.Count);
		}

		private static List<int> GetFieldsIndexes([NotNull] IReadOnlyList<ITableField> fields,
		                                          [CanBeNull] IEnumerable<string> fieldNames)
		{
			if (fieldNames == null)
			{
				return Enumerable.Range(0, fields.Count).ToList();
			}

			var fieldIndexesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < fields.Count; i++)
			{
				fieldIndexesByName[fields[i].Name] = i;
			}

			return fieldNames
			       .Select(fieldName => fieldIndexesByName[fieldName])
			       .ToList();
		}

		private DataRequest CreateDataRequest([CanBeNull] ITableFilter filter)
		{
			ShapeMsg searchGeoMsg =
				filter is IFeatureClassFilter spatialFilter
				&& spatialFilter.FilterGeometry != null
					? ProtobufGeometryUtils.ToShapeMsg(spatialFilter.FilterGeometry)
					: null;

			var dataRequest = new DataRequest
			                  {
				                  SubFields = filter?.SubFields ?? string.Empty,
				                  WhereClause = filter?.WhereClause ?? string.Empty,
				                  SearchGeometry = searchGeoMsg,
			                  };

			if (_queryDefinition != null)
			{
				dataRequest.RelQueryDef = _queryDefinition;
			}
			else
			{
				dataRequest.ClassDef = Assert.NotNull(_classDefinition);
			}

			return dataRequest;
		}

		[NotNull]
		private static GdbData ConfirmDataReceived([CanBeNull] DataVerificationRequest moreData,
		                                           [NotNull] DataRequest forRequest)
		{
			if (moreData == null)
			{
				throw new IOException(
					$"The client failed to provide more data upon request {forRequest}");
			}

			GdbData gdbData = moreData.Data;

			if (gdbData != null)
			{
				return gdbData;
			}

			string errorMessage = moreData.ErrorMessage;

			throw new DataAccessException(
				$"Error reading data for request: {errorMessage}. {Environment.NewLine}" +
				$"Request:{Environment.NewLine}{forRequest}");
		}

		private IEnumerable<VirtualRow> ProcessColumnarData(
			[NotNull] ColumnarGdbObjects columnarData)
		{
			_msg.DebugFormat("Processing {0} rows from columnar data", columnarData.RowCount);

			var fieldMapping = new ColumnarFieldMapping(
				((ITableSchemaDef) _schema).TableFields,
				columnarData);

			// Process each row
			for (int rowIndex = 0; rowIndex < columnarData.RowCount; rowIndex++)
			{
				var valueList = new ColumnarValueList(
					columnarData, rowIndex, fieldMapping,
					shapeMsg => ProtobufGeometryUtils.FromShapeMsg(
						shapeMsg, _schema.SpatialReference));

				// Get OID
				long oid = fieldMapping.GetOidForRow(columnarData, rowIndex, _schema.OIDFieldName);

				yield return _schema.CreateObject(oid, valueList);
			}
		}

		private IEnvelope GetExtent()
		{
			throw new NotImplementedException();
		}
	}
}
