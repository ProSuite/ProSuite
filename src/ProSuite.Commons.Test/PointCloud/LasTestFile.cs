using System;
using System.IO;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Test.PointCloud
{
	/// <summary>
	/// Writes small but genuine LAS files for tests: a complete 1.2 public header block plus point
	/// records in format 0. Committing binary fixtures would hide exactly what a header test is
	/// about, and the declared Z range - the thing under test - has to be settable per file.
	/// </summary>
	public static class LasTestFile
	{
		/// <summary>The size of a LAS 1.0 - 1.2 public header block.</summary>
		public const int HeaderSize = 227;

		/// <summary>The length of a point data record in format 0.</summary>
		public const int PointRecordLength = 20;

		private const double _scale = 0.001;

		/// <summary>
		/// Writes a valid LAS 1.2 file declaring the given bounding box, with one point at each of
		/// the four corners of the box (the lower two at <paramref name="minZ"/>, the upper two at
		/// <paramref name="maxZ"/>), so that the header and the points agree.
		/// </summary>
		/// <returns>The path that was written, for convenient inlining.</returns>
		[NotNull]
		public static string Write([NotNull] string filePath,
		                           double minX, double minY, double maxX, double maxY,
		                           double minZ, double maxZ)
		{
			var corners = new[]
			              {
				              (minX, minY, minZ),
				              (maxX, minY, minZ),
				              (minX, maxY, maxZ),
				              (maxX, maxY, maxZ)
			              };

			using (FileStream stream = File.Create(filePath))
			using (var writer = new BinaryWriter(stream))
			{
				WriteHeader(writer, corners.Length, minX, minY, maxX, maxY, minZ, maxZ);

				foreach ((double x, double y, double z) in corners)
				{
					WritePoint(writer, x, y, z);
				}
			}

			return filePath;
		}

		/// <summary>
		/// Writes a file of the right size whose first four bytes are not the LAS signature - the
		/// shape of a file that is simply not a LAS file (a renamed something else).
		/// </summary>
		[NotNull]
		public static string WriteWithWrongSignature([NotNull] string filePath)
		{
			var bytes = new byte[HeaderSize + PointRecordLength];

			Encoding.ASCII.GetBytes("NOPE").CopyTo(bytes, 0);

			File.WriteAllBytes(filePath, bytes);

			return filePath;
		}

		/// <summary>
		/// Writes a LAS file that stops in the middle of its header - the shape of a delivery that
		/// was interrupted.
		/// </summary>
		[NotNull]
		public static string WriteTruncated([NotNull] string filePath, int byteCount = 100)
		{
			if (byteCount >= HeaderSize)
			{
				throw new ArgumentOutOfRangeException(
					nameof(byteCount), byteCount,
					$@"A truncated header must be shorter than {HeaderSize} bytes");
			}

			var bytes = new byte[byteCount];

			Encoding.ASCII.GetBytes("LASF").CopyTo(bytes, 0);

			File.WriteAllBytes(filePath, bytes);

			return filePath;
		}

		/// <summary>
		/// Writes a valid-looking LAS file whose declared bounding box is inverted (min above max),
		/// which no reader can make sense of.
		/// </summary>
		[NotNull]
		public static string WriteWithInvertedBoundingBox([NotNull] string filePath)
		{
			using (FileStream stream = File.Create(filePath))
			using (var writer = new BinaryWriter(stream))
			{
				WriteHeader(writer, 0,
				            minX: 100, minY: 100, maxX: 0, maxY: 0,
				            minZ: 100, maxZ: 0);
			}

			return filePath;
		}

		private static void WriteHeader([NotNull] BinaryWriter writer, int pointCount,
		                                double minX, double minY, double maxX, double maxY,
		                                double minZ, double maxZ)
		{
			writer.Write(Encoding.ASCII.GetBytes("LASF")); // 0: signature
			writer.Write((ushort) 0); // 4: file source id
			writer.Write((ushort) 0); // 6: global encoding
			writer.Write(new byte[16]); // 8: project id GUID
			writer.Write((byte) 1); // 24: version major
			writer.Write((byte) 2); // 25: version minor
			writer.Write(new byte[32]); // 26: system identifier
			WriteFixed(writer, "ProSuite LasTestFile", 32); // 58: generating software
			writer.Write((ushort) 1); // 90: file creation day of year
			writer.Write((ushort) 2026); // 92: file creation year
			writer.Write((ushort) HeaderSize); // 94: header size
			writer.Write((uint) HeaderSize); // 96: offset to point data
			writer.Write((uint) 0); // 100: number of variable length records
			writer.Write((byte) 0); // 104: point data record format
			writer.Write((ushort) PointRecordLength); // 105: point data record length
			writer.Write((uint) pointCount); // 107: legacy number of point records
			writer.Write(new byte[20]); // 111: legacy number of points by return

			writer.Write(_scale); // 131: X scale factor
			writer.Write(_scale); // 139: Y scale factor
			writer.Write(_scale); // 147: Z scale factor
			writer.Write(0d); // 155: X offset
			writer.Write(0d); // 163: Y offset
			writer.Write(0d); // 171: Z offset

			writer.Write(maxX); // 179
			writer.Write(minX); // 187
			writer.Write(maxY); // 195
			writer.Write(minY); // 203
			writer.Write(maxZ); // 211
			writer.Write(minZ); // 219
		}

		private static void WritePoint([NotNull] BinaryWriter writer,
		                               double x, double y, double z)
		{
			writer.Write((int) Math.Round(x / _scale));
			writer.Write((int) Math.Round(y / _scale));
			writer.Write((int) Math.Round(z / _scale));
			writer.Write((ushort) 0); // intensity
			writer.Write((byte) 0); // return number / number of returns / scan flags
			writer.Write((byte) 2); // classification: ground
			writer.Write((sbyte) 0); // scan angle rank
			writer.Write((byte) 0); // user data
			writer.Write((ushort) 0); // point source id
		}

		private static void WriteFixed([NotNull] BinaryWriter writer, [NotNull] string value,
		                               int length)
		{
			var bytes = new byte[length];

			byte[] valueBytes = Encoding.ASCII.GetBytes(value);

			Array.Copy(valueBytes, bytes, Math.Min(valueBytes.Length, length));

			writer.Write(bytes);
		}
	}
}
