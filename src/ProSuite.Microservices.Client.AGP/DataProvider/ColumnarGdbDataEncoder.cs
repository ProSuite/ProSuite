using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

public sealed class ColumnarGdbDataEncoder : IGdbDataEncoder
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public IEnumerable<GdbData> Encode(Table proTable, QueryFilter filter, long classHandle,
	                                   int maxRowsPerBatch, long maxBatchSizeBytes)
	{
		var def = proTable.GetDefinition();
		var fields = def.GetFields();
		string subfields = filter?.SubFields;

		HashSet<string> requested = null;
		if (! string.IsNullOrEmpty(subfields) && subfields != "*")
			requested = new HashSet<string>(StringUtils.SplitAndTrim(subfields, ','));

		var fieldIndexes = GdbObjectConversionUtils.GetFieldsIndexes(fields, requested);

		int geometryFieldIndex =
			fieldIndexes.FindIndex(fieldIndex => fields[fieldIndex].FieldType == FieldType.Geometry);

		bool includeGeom = proTable is FeatureClass &&
		                   (string.IsNullOrEmpty(subfields) || subfields == "*" ||
		                    geometryFieldIndex >= 0);

		return ReadGdbDataColumnar(proTable, filter, fields, fieldIndexes, includeGeom,
		                           includeSpatialRef: false, maxRowCount: maxRowsPerBatch,
		                           maxBatchSizeBytes: maxBatchSizeBytes);
	}

	private static IEnumerable<GdbData> ReadGdbDataColumnar(
		[NotNull] Table proTable,
		[CanBeNull] QueryFilter filter,
		[NotNull] IReadOnlyList<Field> allFields,
		[NotNull] List<int> fieldsToReturn,
		bool includeGeometry,
		bool includeSpatialRef,
		int maxRowCount,
		long maxBatchSizeBytes)
	{
		List<IColumnBuilder> columnBuilders = CreateColumnBuilders(allFields, fieldsToReturn);

		int totalRowCount = 0;
		int batchRowCount = 0;

		foreach (Row row in GdbQueryUtils.GetRows<Row>(
			         proTable, filter, recycle: false, CancellationToken.None))
		{
			// Append values of each field to its column
			for (int col = 0; col < fieldsToReturn.Count; col++)
			{
				int fieldIdx = fieldsToReturn[col];
				Field field = allFields[fieldIdx];

				// Geometry is read via Feature.GetShape to avoid E_FAIL when not in subfields
				if (field.FieldType == FieldType.Geometry)
				{
					if (row is Feature feature && includeGeometry)
					{
						var shape = feature.GetShape();

						var shapeMsg =
							ProtobufConversionUtils.ToShapeMsg(
								shape, useSpatialRefWkId: ! includeSpatialRef);
						((GeometryColumnBuilder) columnBuilders[col]).Add(shapeMsg);
					}
					else
					{
						columnBuilders[col].AddNull();
					}

					continue;
				}

				object value = row[fieldIdx];
				if (value == null || value == DBNull.Value)
				{
					columnBuilders[col].AddNull();
					continue;
				}

				columnBuilders[col].AddValue(value);
			}

			totalRowCount++;
			batchRowCount++;

			bool rowCapReached = maxRowCount > 0 && batchRowCount >= maxRowCount;
			bool sizeCapReached = maxBatchSizeBytes > 0 &&
			                      EstimatedBatchSize(columnBuilders) >= maxBatchSizeBytes;

			if (rowCapReached || sizeCapReached)
			{
				yield return BuildBatch(columnBuilders, batchRowCount, hasMoreData: true);

				columnBuilders = CreateColumnBuilders(allFields, fieldsToReturn);
				batchRowCount = 0;
			}
		}

		// Final (possibly empty) batch marks the end of the stream.
		yield return BuildBatch(columnBuilders, batchRowCount, hasMoreData: false);

		_msg.DebugFormat("Encoded {0} rows into columnar data.", totalRowCount);
	}

	private static List<IColumnBuilder> CreateColumnBuilders(
		[NotNull] IReadOnlyList<Field> allFields,
		[NotNull] List<int> fieldsToReturn)
	{
		var columnBuilders = new List<IColumnBuilder>(fieldsToReturn.Count);

		// Create column builders in subfields order
		foreach (int fieldIdx in fieldsToReturn)
		{
			Field f = allFields[fieldIdx];
			columnBuilders.Add(CreateColumnBuilderForField(f));
		}

		return columnBuilders;
	}

	private static long EstimatedBatchSize([NotNull] List<IColumnBuilder> columnBuilders)
	{
		long sum = 0;
		foreach (IColumnBuilder builder in columnBuilders)
		{
			sum += builder.EstimatedByteSize;
		}

		return sum;
	}

	private static GdbData BuildBatch([NotNull] List<IColumnBuilder> columnBuilders,
	                                  int rowCount, bool hasMoreData)
	{
		var columnarData = new ColumnarGdbObjects { RowCount = rowCount };

		foreach (IColumnBuilder builder in columnBuilders)
		{
			builder.FinalizeColumn(out Column colMsg);
			columnarData.Columns.Add(colMsg);
		}

		// Return as GdbData with columnar set (no row-wise objects)
		return new GdbData
		       {
			       GdbColumnarData = columnarData,
			       GdbObjectCount = rowCount,
			       HasMoreData = hasMoreData
		       };
	}

	private static IColumnBuilder CreateColumnBuilderForField(Field field)
	{
		return field.FieldType switch
		{
			FieldType.SmallInteger => new Int16ColumnBuilder(field),
			FieldType.Integer => new Int32ColumnBuilder(field),
			FieldType.BigInteger or FieldType.OID => new Int64ColumnBuilder(field),
			FieldType.Single => new FloatColumnBuilder(field),
			FieldType.Double => new DoubleColumnBuilder(field),
			FieldType.String => new StringColumnBuilder(field),
			FieldType.Blob => new BytesColumnBuilder(field),
			FieldType.GUID or FieldType.GlobalID => new UuidColumnBuilder(field),
			FieldType.Date => new DateTicksColumnBuilder(field),
			FieldType.Geometry => new GeometryColumnBuilder(field),
			_ => throw new NotSupportedException(
				     $"Columnar transfer not supported for field type {field.FieldType} ({field.Name})")
		};
	}
}
