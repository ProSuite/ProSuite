using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using Google.Protobuf;
using ProSuite.Microservices.Definitions.Shared.Commons;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

public abstract class ColumnBuilderBase<T> : IColumnBuilder
{
	private readonly string _name;
	private readonly WireFieldType _wireType;
	private readonly List<T> _values = new();
	private readonly NullBitmapBuilder _nulls = new();
	private long _estimatedByteSize;

	protected ColumnBuilderBase(Field field, WireFieldType wireType)
	{
		_name = field.Name;
		_wireType = wireType;
	}

	public long EstimatedByteSize => _estimatedByteSize;

	public abstract void AddValue(object value);

	public void AddNull()
	{
		// Protobuf defaults: string, bytes, and UUID must not be null in repeated fields.
		// Use empty placeholders for those types to avoid null entries in the Values list.
		T placeholder;
		if (typeof(T) == typeof(string))
		{
			placeholder = (T) (object) string.Empty;
		}
		else if (typeof(T) == typeof(byte[]))
		{
			placeholder = (T) (object) Array.Empty<byte>();
		}
		else if (typeof(T) == typeof(UUID))
		{
			// Create empty UUID with zero bytes
			placeholder = (T) (object) new UUID { Value = ByteString.Empty };
		}
		else
		{
			placeholder = default;
		}

		_values.Add(placeholder);
		_nulls.AddNull();
		_estimatedByteSize += EstimateSize(placeholder);
	}

	public virtual void FinalizeColumn(out Column columnMsg)
	{
		columnMsg = new Column
		            {
			            Name = _name,
			            Type = _wireType,
			            Nulls = ByteString.CopyFrom(_nulls.ToArray())
		            };
		SetValues(columnMsg, _values);
	}

	protected void AddNonNull(T v)
	{
		_values.Add(v);
		_nulls.AddNonNull();
		_estimatedByteSize += EstimateSize(v);
	}

	protected abstract void SetValues(Column columnMsg, List<T> values);

	/// <summary>
	/// Estimates the serialized size (in bytes, including field tag overhead) of a single
	/// value of this column. Used to enforce size-capped batching. Estimates may be rough;
	/// they only need to be good enough to bound batch memory.
	/// </summary>
	protected abstract long EstimateSize(T value);
}

internal sealed class NullBitmapBuilder
{
	private readonly List<byte> _bytes = new();
	private int _bitIndex;

	public void AddNull() => AddBit(true);

	public void AddNonNull() => AddBit(false);

	private void AddBit(bool isNull)
	{
		int byteIdx = _bitIndex / 8;
		if (byteIdx == _bytes.Count)
			_bytes.Add(0);

		if (isNull)
		{
			int bitInByte = _bitIndex % 8;
			_bytes[byteIdx] = (byte) (_bytes[byteIdx] | 1 << bitInByte);
		}

		_bitIndex++;
	}

	public byte[] ToArray() => _bytes.ToArray();
}

internal sealed class Int16ColumnBuilder : ColumnBuilderBase<int>
{
	public Int16ColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeInt16) { }

	public override void AddValue(object value) => AddNonNull(Convert.ToInt32(value));

	protected override void SetValues(Column c, List<int> v)
	{
		c.ShortIntValues = new Int16Column { Values = { v } };
	}

	protected override long EstimateSize(int value) => 5;
}

internal sealed class Int32ColumnBuilder : ColumnBuilderBase<int>
{
	public Int32ColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeInt32) { }

	public override void AddValue(object value) => AddNonNull(Convert.ToInt32(value));

	protected override void SetValues(Column c, List<int> v)
	{
		c.IntValues = new Int32Column { Values = { v } };
	}

	protected override long EstimateSize(int value) => 5;
}

internal sealed class Int64ColumnBuilder : ColumnBuilderBase<long>
{
	public Int64ColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeInt64) { }

	public override void AddValue(object value) => AddNonNull(Convert.ToInt64(value));

	protected override void SetValues(Column c, List<long> v)
	{
		c.BigIntValues = new Int64Column { Values = { v } };
	}

	protected override long EstimateSize(long value) => 10;
}

internal sealed class FloatColumnBuilder : ColumnBuilderBase<float>
{
	public FloatColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeFloat) { }

	public override void AddValue(object value) => AddNonNull(Convert.ToSingle(value));

	protected override void SetValues(Column c, List<float> v)
	{
		c.FloatValues = new FloatColumn { Values = { v } };
	}

	protected override long EstimateSize(float value) => 5;
}

internal sealed class DoubleColumnBuilder : ColumnBuilderBase<double>
{
	public DoubleColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeDouble) { }

	public override void AddValue(object value) => AddNonNull(Convert.ToDouble(value));

	protected override void SetValues(Column c, List<double> v)
	{
		c.DoubleValues = new DoubleColumn { Values = { v } };
	}

	protected override long EstimateSize(double value) => 9;
}

internal sealed class StringColumnBuilder : ColumnBuilderBase<string>
{
	public StringColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeString) { }

	public override void AddValue(object value) => AddNonNull((string) value);

	protected override void SetValues(Column c, List<string> v)
	{
		c.StringValues = new StringColumn { Values = { v } };
	}

	// Rough UTF-8 upper bound: up to 3 bytes/char plus tag + length prefix.
	protected override long EstimateSize(string value) =>
		value == null ? 1 : (long) value.Length * 3 + 3;
}

internal sealed class BytesColumnBuilder : ColumnBuilderBase<byte[]>
{
	public BytesColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeBytes) { }

	public override void AddValue(object value) => AddNonNull((byte[]) value);

	protected override void SetValues(Column c, List<byte[]> v)
	{
		var col = new BytesColumn();
		col.Values.AddRange(v.Select(ByteString.CopyFrom));
		c.ByteValues = col;
	}

	protected override long EstimateSize(byte[] value) =>
		value == null ? 1 : value.Length + 3;
}

internal sealed class UuidColumnBuilder : ColumnBuilderBase<UUID>
{
	public UuidColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeUuid) { }

	public override void AddValue(object value)
	{
		// In AGP fields, GUID/GlobalID typically come as string -> Verify this!
		string guidString = Convert.ToString(value) ?? string.Empty;

		var bytes = new Guid(guidString).ToByteArray();

		AddNonNull(new UUID { Value = ByteString.CopyFrom(bytes) });
	}

	protected override void SetValues(Column c, List<UUID> v)
	{
		c.UuidValues = new UuidColumn { Values = { v } };
	}

	protected override long EstimateSize(UUID value) =>
		(value?.Value.Length ?? 0) + 3;
}

internal sealed class DateTicksColumnBuilder : ColumnBuilderBase<long>
{
	public DateTicksColumnBuilder(Field f) : base(f, WireFieldType.FieldTypeDateticks) { }

	public override void AddValue(object value) => AddNonNull(((DateTime) value).Ticks);

	protected override void SetValues(Column c, List<long> v)
	{
		c.DateTimeTicksValues = new DateTicksColumn { Values = { v } };
	}

	protected override long EstimateSize(long value) => 10;
}

internal sealed class GeometryColumnBuilder : IColumnBuilder
{
	private readonly string _name;
	private readonly List<ShapeMsg> _shapes = new();
	private readonly NullBitmapBuilder _nulls = new();
	private long _estimatedByteSize;

	public GeometryColumnBuilder(Field field)
	{
		_name = field.Name;
	}

	public long EstimatedByteSize => _estimatedByteSize;

	public void AddValue(object value) =>
		throw new NotSupportedException("Use Add(ShapeMsg) for geometry");

	public void AddNull()
	{
		_shapes.Add(null);
		_nulls.AddNull();
		_estimatedByteSize += 1;
	}

	public void Add(ShapeMsg shapeMsg)
	{
		_shapes.Add(shapeMsg);
		_nulls.AddNonNull();
		_estimatedByteSize += (shapeMsg?.CalculateSize() ?? 0) + 3;
	}

	public void FinalizeColumn(out Column columnMsg)
	{
		columnMsg = new Column
		            {
			            Name = _name,
			            Type = WireFieldType.FieldTypeGeometry,
			            Nulls = ByteString.CopyFrom(_nulls.ToArray()),
			            Geometries = new GeometryColumn
			                         {
				                         Shapes = { _shapes }
			                         }
		            };
	}
}
