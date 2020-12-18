using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Geodatabase
{
	public class RemoteDataset : BackingDataset
	{
		private readonly ITable _schema;
		[CanBeNull] private readonly ClassDef _classDefinition;
		[CanBeNull] private readonly RelationshipClassQuery _queryDefinition;

		private IEnvelope _extent;

		public RemoteDataset(
			[NotNull] ITable schema,
			[NotNull] Func<DataVerificationResponse, DataVerificationRequest> getRemoteDataFunc,
			[CanBeNull] ClassDef classDefinition,
			[CanBeNull] RelationshipClassQuery queryDefinition = null)
		{
			Assert.True(classDefinition != null | queryDefinition != null,
			            "Either the class definition or the query definition must be provided.");

			_schema = schema;
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

		public override IRow GetRow(int id)
		{
			Assert.True(_schema.HasOID, "The table {0} has no OID", DatasetUtils.GetName(_schema));
			Assert.False(string.IsNullOrEmpty(_schema.OIDFieldName),
			             "The table {0} has no OID Field Name", DatasetUtils.GetName(_schema));

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

		public override int GetRowCount(IQueryFilter filter)
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

			return moreData.Data.GdbObjectCount;
		}

		public override IEnumerable<IRow> Search(IQueryFilter filter, bool recycling)
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

			foreach (GdbObjectMsg gdbObjMsg in moreData.Data.GdbObjects)
			{
				yield return ProtobufConversionUtils.FromGdbObjectMsg(gdbObjMsg, _schema);
			}
		}

		private DataRequest CreateDataRequest(IQueryFilter filter)
		{
			ShapeMsg searchGeoMsg =
				filter is ISpatialFilter spatialFilter && spatialFilter.Geometry != null
					? ProtobufGeometryUtils.ToShapeMsg(spatialFilter.Geometry)
					: null;

			var dataRequest = new DataRequest
			                  {
				                  WhereClause = filter.WhereClause,
				                  SearchGeometry = searchGeoMsg,
				                  SubFields = filter.SubFields
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
