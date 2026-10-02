using System;
using System.IO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.PointCloud
{
	/// <summary>
	/// The public header block of a LAS file, as far as it can be read without any LAS library:
	/// the format signature, the version, the point count and the bounding box the file declares.
	/// <para>
	/// Deliberately a dependency-free reader. It lives here rather than in the full-featured LAS
	/// data access library because its consumers are quality tests, which see nothing above
	/// ProSuite.Commons - and because validating a header must not require the file to be
	/// decodable: reading the first 227 bytes is the whole job, and a file that fails here is
	/// exactly the file a test wants to report.
	/// </para>
	/// </summary>
	/// <remarks>
	/// Header layout per the ASPRS LAS specification, identical in 1.0 through 1.4 for every field
	/// read here. The legacy 32-bit point count at offset 107 is zero in 1.4 files that either
	/// exceed its range or use a point format above 5; those carry the 64-bit count at offset 247.
	/// </remarks>
	public class LasHeaderInfo
	{
		/// <summary>The file signature every LAS file starts with.</summary>
		public const string Signature = "LASF";

		/// <summary>The size of the smallest public header block (LAS 1.0 - 1.2).</summary>
		public const int MinimumHeaderSize = 227;

		private LasHeaderInfo() { }

		public int VersionMajor { get; private set; }

		public int VersionMinor { get; private set; }

		[NotNull]
		public string Version => $"{VersionMajor}.{VersionMinor}";

		public int HeaderSize { get; private set; }

		public long OffsetToPointData { get; private set; }

		public int PointDataRecordFormat { get; private set; }

		public int PointDataRecordLength { get; private set; }

		/// <summary>The number of point records the header declares.</summary>
		public ulong PointCount { get; private set; }

		public double MinX { get; private set; }
		public double MaxX { get; private set; }
		public double MinY { get; private set; }
		public double MaxY { get; private set; }
		public double MinZ { get; private set; }
		public double MaxZ { get; private set; }

		/// <summary>
		/// Reads and validates the public header block of the given LAS file.
		/// </summary>
		/// <param name="filePath">The path of the .las file.</param>
		/// <param name="header">The header that was read, or null if it could not be read.</param>
		/// <param name="message">Why the header could not be read, or null on success.</param>
		/// <returns>Whether a valid header was read.</returns>
		public static bool TryRead([NotNull] string filePath,
		                           [CanBeNull] out LasHeaderInfo header,
		                           [CanBeNull] out string message)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			header = null;

			if (! File.Exists(filePath))
			{
				message = "File does not exist";
				return false;
			}

			try
			{
				using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read,
				                                     FileShare.ReadWrite))
				{
					return TryRead(stream, out header, out message);
				}
			}
			catch (IOException e)
			{
				message = $"Cannot read the file: {e.Message}";
				return false;
			}
			catch (UnauthorizedAccessException e)
			{
				message = $"Cannot read the file: {e.Message}";
				return false;
			}
		}

		private static bool TryRead([NotNull] Stream stream,
		                            [CanBeNull] out LasHeaderInfo header,
		                            [CanBeNull] out string message)
		{
			header = null;

			long length = stream.Length;

			if (length < MinimumHeaderSize)
			{
				message =
					$"Truncated: {length} bytes, a LAS header alone is {MinimumHeaderSize} bytes";
				return false;
			}

			using (var reader = new BinaryReader(stream))
			{
				// Read the signature as bytes: a decoder would happily consume more than four
				// bytes of a file that is not a LAS file at all.
				byte[] signatureBytes = reader.ReadBytes(4);

				if (! IsSignature(signatureBytes))
				{
					message = $"Not a LAS file: the signature is '{Escape(signatureBytes)}', " +
					          $"expected '{Signature}'";
					return false;
				}

				var result = new LasHeaderInfo();

				Seek(stream, 24);
				result.VersionMajor = reader.ReadByte();
				result.VersionMinor = reader.ReadByte();

				Seek(stream, 94);
				result.HeaderSize = reader.ReadUInt16();
				result.OffsetToPointData = reader.ReadUInt32();

				Seek(stream, 104);
				result.PointDataRecordFormat = reader.ReadByte();
				result.PointDataRecordLength = reader.ReadUInt16();
				result.PointCount = reader.ReadUInt32();

				Seek(stream, 179);
				result.MaxX = reader.ReadDouble();
				result.MinX = reader.ReadDouble();
				result.MaxY = reader.ReadDouble();
				result.MinY = reader.ReadDouble();
				result.MaxZ = reader.ReadDouble();
				result.MinZ = reader.ReadDouble();

				// LAS 1.4 carries the authoritative point count as a 64 bit value, and zeroes the
				// legacy one whenever it cannot represent the file.
				if (result.PointCount == 0 && IsAtLeast(result, 1, 4) &&
				    result.HeaderSize >= 375 && length >= 255)
				{
					Seek(stream, 247);
					result.PointCount = reader.ReadUInt64();
				}

				if (! IsValid(result, length, out message))
				{
					return false;
				}

				header = result;
				message = null;
				return true;
			}
		}

		private static bool IsValid([NotNull] LasHeaderInfo header, long fileLength,
		                            [CanBeNull] out string message)
		{
			if (header.VersionMajor != 1)
			{
				message = $"Unsupported LAS version {header.Version}";
				return false;
			}

			if (header.HeaderSize < MinimumHeaderSize || header.HeaderSize > fileLength)
			{
				message = $"Header size {header.HeaderSize} is out of range " +
				          $"(file is {fileLength} bytes)";
				return false;
			}

			if (header.OffsetToPointData < header.HeaderSize ||
			    header.OffsetToPointData > fileLength)
			{
				message = $"Offset to point data {header.OffsetToPointData} is out of range " +
				          $"(header size {header.HeaderSize}, file is {fileLength} bytes)";
				return false;
			}

			if (header.MinX > header.MaxX || header.MinY > header.MaxY ||
			    header.MinZ > header.MaxZ)
			{
				message = "The declared bounding box is inverted " +
				          $"(X {header.MinX}..{header.MaxX}, Y {header.MinY}..{header.MaxY}, " +
				          $"Z {header.MinZ}..{header.MaxZ})";
				return false;
			}

			message = null;
			return true;
		}

		private static bool IsAtLeast([NotNull] LasHeaderInfo header, int major, int minor)
		{
			return header.VersionMajor > major ||
			       (header.VersionMajor == major && header.VersionMinor >= minor);
		}

		private static void Seek([NotNull] Stream stream, long position)
		{
			stream.Seek(position, SeekOrigin.Begin);
		}

		private static bool IsSignature([NotNull] byte[] bytes)
		{
			if (bytes.Length != Signature.Length)
			{
				return false;
			}

			for (var i = 0; i < Signature.Length; i++)
			{
				if (bytes[i] != Signature[i])
				{
					return false;
				}
			}

			return true;
		}

		[NotNull]
		private static string Escape([NotNull] byte[] bytes)
		{
			var result = new char[bytes.Length];

			for (var i = 0; i < bytes.Length; i++)
			{
				var c = (char) bytes[i];

				result[i] = bytes[i] > 126 || char.IsControl(c) ? '?' : c;
			}

			return new string(result);
		}

		public override string ToString()
		{
			return $"LAS {Version}, point format {PointDataRecordFormat}, " +
			       $"{PointCount} points, Z {MinZ}..{MaxZ}";
		}
	}
}
