using System;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry.Wkb
{
	[CLSCompliant(false)]
	public class BigEndianBinaryReader : BinaryReader
	{
		public BigEndianBinaryReader([NotNull] Stream input) : base(input) { }

		public BigEndianBinaryReader([NotNull] Stream input, [NotNull] Encoding encoding) : base(
			input, encoding) { }

		public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(
			input, encoding, leaveOpen) { }

		public override double ReadDouble()
		{
			return BitConverter.ToDouble(ReadReversedBytes(8), 0);
		}

		public override short ReadInt16()
		{
			return BitConverter.ToInt16(ReadReversedBytes(2), 0);
		}

		public override int ReadInt32()
		{
			return BitConverter.ToInt32(ReadReversedBytes(4), 0);
		}

		public override long ReadInt64()
		{
			return BitConverter.ToInt64(ReadReversedBytes(8), 0);
		}

		public override ushort ReadUInt16()
		{
			return BitConverter.ToUInt16(ReadReversedBytes(2), 0);
		}

		public override uint ReadUInt32()
		{
			return BitConverter.ToUInt32(ReadReversedBytes(4), 0);
		}

		public override ulong ReadUInt64()
		{
			return BitConverter.ToUInt64(ReadReversedBytes(8), 0);
		}

		private byte[] ReadReversedBytes(int count)
		{
			return ReadBytes(count).Reverse().ToArray();
		}
	}
}
