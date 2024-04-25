using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	public class RemoteDataset : BackingDataset
	{
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

			foreach (GdbObjectMsg gdbObjMsg in moreData.Data.GdbObjects)
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

			if (moreData == null)
			{
				throw new IOException(
					$"No data provided by the client for data request {response.DataRequest}");
			}

			// TODO: Remove conversion at Server11
			return Convert.ToInt32(moreData.Data.GdbObjectCount);
		}

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			DataRequest dataRequest = CreateDataRequest(filter);

			DataVerificationResponse response =
				new DataVerificationResponse
				{
					DataRequest = dataRequest
				};

			DataVerificationRequest moreData = GetData(response);

			if (moreData == null)
			{
				throw new IOException(
					$"No data provided by the client for data request {response.DataRequest}");
			}

			RepeatedField<GdbObjectMsg> foundGdbObjects = moreData.Data?.GdbObjects;

			if (foundGdbObjects == null || foundGdbObjects.Count == 0)
			{
				yield break;
			}

			foreach (GdbObjectMsg gdbObjMsg in foundGdbObjects)
			{
				yield return ProtobufConversionUtils.FromGdbObjectMsg(gdbObjMsg, _schema);
			}
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

		private IEnvelope GetExtent()
		{
			// TODO: Package and wire along with schema - is way cheaper
			throw new NotImplementedException();
		}
	}
}
