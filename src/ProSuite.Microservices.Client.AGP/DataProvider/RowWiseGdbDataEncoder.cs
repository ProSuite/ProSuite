using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

public class RowWiseGdbDataEncoder : IGdbDataEncoder
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public IEnumerable<GdbData> Encode(Table proTable, QueryFilter filter, long classHandle,
	                                   int maxRowsPerBatch, long maxBatchSizeBytes)
	{
		return ReadGdbData(proTable, filter, classHandle, maxRowsPerBatch, maxBatchSizeBytes,
		                   countOnly: false);
	}

	private static IEnumerable<GdbData> ReadGdbData([NotNull] Table proTable,
	                                                [CanBeNull] QueryFilter filter,
	                                                long resultClassHandle,
	                                                int maxRowCount,
	                                                long maxBatchSizeBytes,
	                                                bool countOnly)
	{
		GdbData featureData = new GdbData();

		if (countOnly)
		{
			featureData.GdbObjectCount = proTable.GetCount(filter);

			yield return featureData;
			yield break;
		}

		int rowCount = 0;
		int batchRowCount = 0;
		long batchSizeBytes = 0;

		string shapeFieldName = null;
		if (proTable is FeatureClass fc)
		{
			shapeFieldName = fc.GetDefinition().GetShapeField();
		}

		IReadOnlyList<Field> fields = proTable.GetDefinition().GetFields();

		HashSet<string> requestedFields = null;
		string subfieldsToRead = filter?.SubFields;
		if (! string.IsNullOrEmpty(subfieldsToRead) && subfieldsToRead != "*")
		{
			requestedFields =
				new HashSet<string>(StringUtils.SplitAndTrim(subfieldsToRead, ','));
		}

		// NOTE: We want to maintain the order of the fields as they are defined by the subfields string.
		List<int> fieldsToReturn =
			GdbObjectConversionUtils.GetFieldsIndexes(fields, requestedFields);

		int geometryFieldIndex =
			fieldsToReturn.FindIndex(fieldIndex => fields[fieldIndex].FieldType == FieldType.Geometry);

		// NOTE: Normal fields just return null if they have not been fetched due to sub-field restrictions.
		//       However, the Shape property E_FAILs.
		bool canGetShape =
			shapeFieldName != null &&
			(string.IsNullOrEmpty(subfieldsToRead) || subfieldsToRead == "*" ||
			 geometryFieldIndex >= 0);

		Stopwatch watch = new Stopwatch();

		foreach (Row row in GdbQueryUtils.GetRows<Row>(proTable, filter, true,
		                                               CancellationToken.None))
		{
			try
			{
				watch.Start();

				GdbObjectMsg objectMsg =
					GdbObjectConversionUtils.ToGdbObjectMsg(
						row, canGetShape, includeSpatialRef: false, fieldsToReturn,
						fields);

				objectMsg.ClassHandle = resultClassHandle;

				featureData.GdbObjects.Add(objectMsg);

				watch.Stop();

				rowCount++;
				batchRowCount++;
				if (maxBatchSizeBytes > 0)
				{
					batchSizeBytes += objectMsg.CalculateSize();
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error converting {GdbObjectUtils.ToString(row)} to object message",
				           e);
				throw;
			}

			bool rowCapReached = maxRowCount > 0 && batchRowCount >= maxRowCount;
			bool sizeCapReached = maxBatchSizeBytes > 0 && batchSizeBytes >= maxBatchSizeBytes;

			if (rowCapReached || sizeCapReached)
			{
				_msg.VerboseDebug(() =>
					                  $"Read {rowCount} rows from {proTable.GetName()} " +
					                  $"(batch of {batchRowCount} rows, ~{batchSizeBytes} bytes)");
				featureData.HasMoreData = true;

				yield return featureData;

				featureData = new GdbData(); // reset for next batch
				batchRowCount = 0;
				batchSizeBytes = 0;
			}
		}

		_msg.DebugStopTiming(watch, "Packed row objects");

		// Final (possibly empty) batch with HasMoreData == false marks the end of the stream.
		yield return featureData;
	}
}
