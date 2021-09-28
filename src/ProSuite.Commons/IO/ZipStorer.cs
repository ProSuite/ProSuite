using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.IO
{
	/// <summary>
	/// Represents a Zip file.
	/// Supports Deflate (default) and Store compression methods.
	/// </summary>
	[CLSCompliant(false)]
	[Obsolete("Use ZipArchive in System.IO.Compression. Keep this class for reference")]
	public class ZipStorer : IDisposable
	{
		#region Nested type: CompressionMethod

		/// <summary>
		/// Compression method enumeration
		/// </summary>
		public enum CompressionMethod : ushort
		{
			/// <summary>Uncompressed storage</summary> 
			Store = 0,
			/// <summary>Deflate compression method</summary>
			Deflate = 8
		}

		#endregion

		#region Nested type: ZipFileEntry

		/// <summary>
		/// Represents an entry in the Zip file's directory
		/// </summary>
		public class ZipFileEntry
		{
			/// <summary>Compression method</summary>
			public CompressionMethod Method;

			/// <summary>Full path and filename as stored in Zip</summary>
			public string FullName;

			/// <summary>Original file size</summary>
			public uint OriginalSize;

			/// <summary>Compressed file size</summary>
			public uint CompressedSize;

			/// <summary>Offset of header information inside Zip storage</summary>
			public uint HeaderOffset;

			/// <summary>32-bit checksum of entire file</summary>
			public uint Crc32;

			/// <summary>Last write time of file</summary>
			public DateTime LastWriteTime;

			/// <summary>User comment for file</summary>
			public string Comment;

			/// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
			public bool UseUTF8;

			public override string ToString()
			{
				return FullName;
			}
		}

		#endregion

		#region Private fields

		private Stream _zipStream;
		private readonly FileAccess _access;

		// List of files to store
		private readonly List<ZipFileEntry> _files;
		// Central dir image
		private byte[] _centralDirImage;
		// Existing files in zip
		private ushort _existingFiles;

		// Notice that the ZIP spec requires *no* BOM for UTF8!
		private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
		private static readonly Encoding DefaultEncoding = Encoding.GetEncoding(437);

		private static readonly uint[] CrcTable = CreateCrc32Table();

		#endregion

		#region Factory

		private ZipStorer(Stream zipStream, FileAccess access)
		{
			Assert.ArgumentNotNull(zipStream, nameof(zipStream));

			_zipStream = zipStream;
			_access = access;
			_files = new List<ZipFileEntry>();
		}

		/// <summary>
		/// Create a new empty Zip archive in the given file path.
		/// </summary>
		/// <param name="zipFilePath">File system path to Zip file.</param>
		public static ZipStorer Create(string zipFilePath)
		{
			Stream zipStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.ReadWrite);

			return Create(zipStream);
		}

		/// <summary>
		/// Create a new empty Zip archive in the given stream.
		/// </summary>
		/// <param name="zipStream">The stream (must be seekable).</param>
		public static ZipStorer Create(Stream zipStream)
		{
			Assert.ArgumentNotNull(zipStream, nameof(zipStream));
			Assert.ArgumentCondition(zipStream.CanSeek, "Stream not seekable");

			return new ZipStorer(zipStream, FileAccess.Write);
		}

		/// <summary>
		/// Open an existing Zip archive from a file.
		/// </summary>
		/// <param name="zipFilePath">File system path to Zip file.</param>
		/// <param name="fileAccess">File access mode (read-only or read-write).</param>
		public static ZipStorer Open(string zipFilePath, FileAccess fileAccess)
		{
			fileAccess |= FileAccess.Read; // need read access!
			Stream zipStream = new FileStream(zipFilePath, FileMode.Open, fileAccess);

			return Open(zipStream, fileAccess);
		}

		/// <summary>
		/// Open an existing Zip archive from a stream.
		/// </summary>
		/// <param name="zipStream">The stream.</param>
		/// <param name="access">Access mode (read-only or read-write).</param>
		public static ZipStorer Open(Stream zipStream, FileAccess access)
		{
			Assert.ArgumentNotNull(zipStream, nameof(zipStream));
			Assert.ArgumentCondition(zipStream.CanSeek, "Stream not seekable");

			if ((access & FileAccess.Read) != FileAccess.Read)
			{
				throw new InvalidOperationException("reading not allowed by file access");
			}

			var zip = new ZipStorer(zipStream, access);

			zip.ReadEndRecord();

			return zip;
		}

		#endregion

		/// <summary>
		/// Use UTF8 encoding for filenames and comments; default is CP 437.
		/// </summary>
		public bool UseUtf8 { get; set; }

		/// <summary>
		/// Force deflate algotithm even if it inflates the stored file. Off by default.
		/// </summary>
		public bool ForceDeflating { get; set; }

		/// <summary>
		/// General Zip archive comment. None by default.
		/// </summary>
		public string Comment { get; set; }

		public void AddStream(Stream sourceStream, string zippedFilePath, DateTime lastWriteTime)
		{
			AddStream(sourceStream, zippedFilePath, lastWriteTime, CompressionMethod.Deflate, string.Empty);
		}

		/// <summary>
		/// Add a stream to the Zip archive.
		/// </summary>
		/// <param name="sourceStream">The stream to add.</param>
		/// <param name="zippedFilePath">File name and path to use in Zip directory.</param>
		/// <param name="lastWriteTime">Last modification time to record in Zip directory.</param>
		/// <param name="compressionMethod">Compression method.</param>
		/// <param name="comment">Comment this Zip entry.</param>
		public void AddStream(Stream sourceStream, string zippedFilePath, DateTime lastWriteTime,
		                      CompressionMethod compressionMethod, string comment)
		{
			if (_access == FileAccess.Read)
			{
				throw new InvalidOperationException("Archive is read-only");
			}

			var entry = new ZipFileEntry();
			entry.Method = compressionMethod;
			entry.UseUTF8 = UseUtf8;
			entry.FullName = GetNormalizedFilename(zippedFilePath);
			entry.Comment = comment ?? string.Empty;
			entry.Crc32 = 0; // to be updated later
			entry.LastWriteTime = lastWriteTime;
			entry.HeaderOffset = (uint) _zipStream.Position;

			// Write the header now, knowing that we have to rewrite parts
			// of it later again (once we know compressed size and CRC sum).
			WriteLocalHeader(entry);

			WriteFileData(entry, sourceStream); // Copy source stream to zip file

			UpdateLocalHeader(entry);

			_files.Add(entry);
		}

		/// <summary>
		/// Read all the file records in the central directory 
		/// </summary>
		/// <returns>List of all entries in directory</returns>
		public List<ZipFileEntry> ReadCentralDir()
		{
			// TODO Why not read the image here instead of in ReadEndRecord?
			//      Or read both, the end record and central dir on opening.
			if (_centralDirImage == null)
			{
				throw new InvalidOperationException("Central directory currently does not exist");
			}

			var result = new List<ZipFileEntry>(_existingFiles);

			int byteIndex = 0;
			while (byteIndex < _centralDirImage.Length)
			{
				uint signature = ReadUInt32(_centralDirImage, byteIndex + 0);

				if (signature != 0x02014b50)
				{
					break;
				}

				ushort flags = ReadUInt16(_centralDirImage, byteIndex + 8);
				ushort method = ReadUInt16(_centralDirImage, byteIndex + 10);
				var modifyTime = ReadDateTime(_centralDirImage, byteIndex + 12);
				uint crc32 = ReadUInt32(_centralDirImage, byteIndex + 16);
				uint compressedSize = ReadUInt32(_centralDirImage, byteIndex + 20);
				uint originalSize = ReadUInt32(_centralDirImage, byteIndex + 24);
				ushort fileNameSize = ReadUInt16(_centralDirImage, byteIndex + 28);
				ushort extraFieldSize = ReadUInt16(_centralDirImage, byteIndex + 30);
				ushort commentSize = ReadUInt16(_centralDirImage, byteIndex + 32);
				uint headerOffset = ReadUInt32(_centralDirImage, byteIndex + 42);

				// var headerSize = 46 + fileNameSize + extraFieldSize + commentSize;

				bool useUtf8 = (flags & 0x0800) != 0;
				Encoding encoding = useUtf8 ? Utf8Encoding : DefaultEncoding;

				var entry = new ZipFileEntry();
				entry.Method = (CompressionMethod) method;
				entry.FullName = encoding.GetString(_centralDirImage, byteIndex + 46, fileNameSize);
				entry.OriginalSize = originalSize;
				entry.CompressedSize = compressedSize;
				entry.HeaderOffset = headerOffset;
				entry.Crc32 = crc32;
				entry.LastWriteTime = modifyTime;

				if (commentSize > 0)
				{
					int offset = byteIndex + 46 + fileNameSize + extraFieldSize;
					entry.Comment = encoding.GetString(_centralDirImage, offset, commentSize);
				}

				result.Add(entry);

				byteIndex += 46 + fileNameSize + extraFieldSize + commentSize;
			}

			return result;
		}

		/// <summary>
		/// Extract the given Zip file entry to a file.
		/// </summary>
		/// <param name="entry">The zip file entry to extract.</param>
		/// <param name="targetPath">File system path to the target file.</param>
		/// <remarks>Directories on the path to the target file
		/// will be created if they do not exist. The only supported
		/// compression methods are: Store and Deflate.</remarks>
		public void ExtractFile(ZipFileEntry entry, string targetPath)
		{
			Assert.ArgumentNotNull(entry, nameof(entry));
			Assert.ArgumentNotNullOrEmpty(targetPath, nameof(targetPath));

			// Make sure all directories on the target file path exist:

			string parentPath = Path.GetDirectoryName(targetPath);

			if (parentPath != null && !Directory.Exists(parentPath))
			{
				Directory.CreateDirectory(parentPath);
			}

			// Check it is directory. If so, do nothing
			if (Directory.Exists(targetPath))
			{
				return;
			}

			using (Stream stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
			{
				ExtractFile(entry, stream);
			}

			File.SetCreationTime(targetPath, entry.LastWriteTime);
			File.SetLastWriteTime(targetPath, entry.LastWriteTime);
		}

		/// <summary>
		/// Extract the given zip file entry to a stream.
		/// </summary>
		/// <param name="entry">The zip file entry to extract.</param>
		/// <param name="targetStream">The target stream.</param>
		/// <remarks>The only supported compression methods are:
		/// Store and Deflate.</remarks>
		public void ExtractFile(ZipFileEntry entry, Stream targetStream)
		{
			if (!targetStream.CanWrite)
			{
				throw new InvalidOperationException("Target stream not writable");
			}

			const int headerSize = 30;
			var header = new byte[headerSize];
			_zipStream.Seek(entry.HeaderOffset, SeekOrigin.Begin);
			_zipStream.Read(header, 0, header.Length);

			uint signature = ReadUInt32(header, 0);
			if (signature != 0x04034b50)
			{
				throw new InvalidDataException("Bad local file header signature");
			}

			int fileNameLength = ReadUInt16(header, 26);
			int extraFieldLength = ReadUInt16(header, 28);

			long dataOffset = entry.HeaderOffset + headerSize + fileNameLength + extraFieldLength;

			Stream sourceStream;
			switch (entry.Method)
			{
				case CompressionMethod.Store:
					sourceStream = _zipStream;
					break;
				case CompressionMethod.Deflate:
					sourceStream = new DeflateStream(_zipStream, CompressionMode.Decompress, true);
					break;
				default:
					throw new InvalidOperationException("Compression method is not supported");
			}

			// Buffered copy from zip file to target stream:
			var buffer = new byte[16384];
			_zipStream.Seek(dataOffset, SeekOrigin.Begin);
			uint bytesPending = entry.OriginalSize;
			while (bytesPending > 0)
			{
				var count = (int) Math.Min(bytesPending, buffer.Length);
				int bytesRead = sourceStream.Read(buffer, 0, count);
				targetStream.Write(buffer, 0, bytesRead);
				bytesPending -= (uint) bytesRead;
			}

			targetStream.Flush();

			if (entry.Method == CompressionMethod.Deflate)
			{
				sourceStream.Dispose();
			}
		}

		#region RemoveEntries

		///// <summary>
		///// Removes one or many files from Zip by creating a new Zip file
		///// and copying those entries that should not be removed.
		///// </summary>
		///// <param name="zipStorer">Reference to the current Zip object</param>
		///// <param name="zipFileEntries">List of Entries to remove from storage</param>
		///// <returns>True if success, false if not</returns>
		///// <remarks>This method only works for storage of type FileStream</remarks>
		//public static bool RemoveEntries(ref ZipStorer zipStorer,
		//								 List<ZipFileEntry> zipFileEntries)
		//{
		//	if (!(zipStorer._zipStream is FileStream))
		//	{
		//		throw new InvalidOperationException(
		//			"RemoveEntries is allowed just over streams of type FileStream");
		//	}

		//	List<ZipFileEntry> allEntries = zipStorer.ReadCentralDir();

		//	//In order to delete we need to create a copy of the zip file excluding the selected items
		//	string tempZipName = Path.GetTempFileName();
		//	string tempEntryName = Path.GetTempFileName();

		//	try
		//	{
		//		ZipStorer tempZip = Create(tempZipName, string.Empty);

		//		foreach (ZipFileEntry zfe in allEntries)
		//		{
		//			if (!zipFileEntries.Contains(zfe))
		//			{
		//				zipStorer.ExtractFile(zfe, tempEntryName);
		//				tempZip.AddFile(tempEntryName, zfe.FullName, zfe.Method, zfe.Comment);
		//			}
		//		}

		//		zipStorer.Close();
		//		tempZip.Close();

		//		File.Delete(zipStorer._zipFilePath);
		//		File.Move(tempZipName, zipStorer._zipFilePath);

		//		zipStorer = Open(zipStorer._zipFilePath, zipStorer._access);
		//	}
		//	catch
		//	{
		//		return false;
		//	}
		//	finally
		//	{
		//		if (File.Exists(tempZipName))
		//		{
		//			File.Delete(tempZipName);
		//		}

		//		if (File.Exists(tempEntryName))
		//		{
		//			File.Delete(tempEntryName);
		//		}
		//	}

		//	return true;
		//}

		#endregion

		/// <summary>
		/// Close the Zip archive.
		/// </summary>
		/// <remarks>Closing the Zip archive is mandatory, as it updates
		/// the central directory (if the archive was written to) and
		/// flushes and closes the underlying Zip stream.</remarks>
		public void Close()
		{
			if (_zipStream == null)
			{
				throw new InvalidOperationException("Already closed");
			}

			if (_access != FileAccess.Read)
			{
				var centralOffset = (uint) _zipStream.Position;
				uint centralSize = 0;

				if (_centralDirImage != null)
				{
					centralSize += (uint) _centralDirImage.Length;
					_zipStream.Write(_centralDirImage, 0, _centralDirImage.Length);
				}

				foreach (ZipFileEntry entry in _files)
				{
					long pos = _zipStream.Position;
					WriteCentralDirRecord(entry);
					centralSize += (uint) (_zipStream.Position - pos);
				}

				WriteEndRecord(centralSize, centralOffset);
			}

			_zipStream.Flush();
			_zipStream.Dispose();
			_zipStream = null;
		}

		public bool IsClosed => _zipStream == null;

		#region IDisposable

		public void Dispose()
		{
			// By contract, Dispose() may be called many times
			// and all but the first invocation are no-ops.
			if (!IsClosed)
			{
				Close();
			}
		}

		#endregion

		#region Private methods

		// Each local-file-header record:
		//
		// Offset Bytes Description
		//    0     4   Signature = 50 4B 03 04 = 0x04034B50
		//    4     2   Version needed to extract (minimum)
		//    6     2   General purpose bit flag
		//    8     2   Compression method (0=store, 8=deflate)
		//   10     2   File last modification time (MS-DOS)
		//   12     2   File last modification date (MS-DOS)
		//   14     4   CRC-32
		//   18     4   Compressed size (bytes)
		//   22     4   Original size (bytes)
		//   26     2   File name length (n)
		//   28     2   Extra field length (m)
		//   30     n   File name
		//  30+n    m   Extra field

		private void WriteLocalHeader(ZipFileEntry entry)
		{
			Encoding encoding = entry.UseUTF8 ? Utf8Encoding : DefaultEncoding;
			byte[] fileNameBytes = encoding.GetBytes(entry.FullName ?? string.Empty);

			var fileNameLength = (ushort) fileNameBytes.Length;

			var buffer = new byte[30 + fileNameBytes.Length];

			WriteUInt32(buffer, 0, 0x04034b50); // signature
			WriteBytes(buffer, 4, 20, 0); // version to extract
			WriteUInt16(buffer, 6, (ushort) (entry.UseUTF8 ? 0x0800 : 0)); // flags
			WriteUInt16(buffer, 8, (ushort) entry.Method);
			WriteDateTime(buffer, 10, entry.LastWriteTime);
			WriteUInt32(buffer, 14, 0); // CRC, updated later
			WriteUInt32(buffer, 18, 0); // compressed size, updated later
			WriteUInt32(buffer, 22, 0); // original size, updated later
			WriteUInt32(buffer, 26, fileNameLength);
			WriteUInt32(buffer, 28, 0); // extra field length
			WriteBytes(buffer, 30, fileNameBytes);

			_zipStream.Write(buffer, 0, buffer.Length);
		}

		private void UpdateLocalHeader(ZipFileEntry entry)
		{
			long lastPos = _zipStream.Position; // remember position

			var buffer = new byte[12];

			_zipStream.Position = entry.HeaderOffset + 8;
			WriteUInt16(buffer, 0, (ushort) entry.Method);
			_zipStream.Write(buffer, 0, 2);

			_zipStream.Position = entry.HeaderOffset + 14;
			WriteUInt32(buffer, 0, entry.Crc32);
			WriteUInt32(buffer, 4, entry.CompressedSize);
			WriteUInt32(buffer, 8, entry.OriginalSize);
			_zipStream.Write(buffer, 0, 12);

			_zipStream.Position = lastPos; // restore position
		}

		// Each central-directory-file-header record:
		//
		// Offset Bytes Description
		//    0     4   Signature = 50 4B 01 02 = 0x02014b50
		//    4     2   Version made by
		//    6     2   Version needed to extract (minimum)
		//    8     2   General purpose bit flag
		//   10     2   Compression method (0=store, 8=deflate)
		//   12     2   File last modification time (MS-DOS)
		//   14     2   File last modification date (MS-DOS)
		//   16     4   CRC-32
		//   20     4   Compressed size (bytes)
		//   24     4   Original size (bytes)
		//   28     2   File name length (n)
		//   30     2   Extra field length (m)
		//   32     2   File comment length (k)
		//   34     2   Disk number where file starts
		//   36     2   Internal file attributes
		//   38     4   External file attributes
		//   42     4   Relative offset of local file header
		//   46     n   File name
		//  46+n    m   Extra field
		// 46+n+m   k   File comment

		private void WriteCentralDirRecord(ZipFileEntry entry)
		{
			Encoding encoding = entry.UseUTF8 ? Utf8Encoding : DefaultEncoding;
			byte[] fileNameBytes = encoding.GetBytes(entry.FullName ?? string.Empty);
			byte[] commentBytes = encoding.GetBytes(entry.Comment ?? string.Empty);

			var fileNameLength = (ushort) fileNameBytes.Length;
			var commentLength = (ushort) commentBytes.Length;

			var buffer = new byte[46 + fileNameBytes.Length + commentBytes.Length];

			WriteUInt32(buffer, 0, 0x02014B50); // signature
			WriteBytes(buffer, 4, 23, 11); // version made by
			WriteBytes(buffer, 6, 20, 0); // version to extract
			WriteUInt16(buffer, 8, (ushort) (entry.UseUTF8 ? 0x0800 : 0)); // flags
			WriteUInt16(buffer, 10, (ushort) entry.Method);
			WriteDateTime(buffer, 12, entry.LastWriteTime);
			WriteUInt32(buffer, 16, entry.Crc32);
			WriteUInt32(buffer, 20, entry.CompressedSize);
			WriteUInt32(buffer, 24, entry.OriginalSize);
			WriteUInt16(buffer, 28, fileNameLength);
			WriteUInt16(buffer, 30, 0); // extra field length
			WriteUInt16(buffer, 32, commentLength);
			WriteUInt16(buffer, 34, 0); // disk num where file starts
			WriteUInt16(buffer, 36, 0); // internal file attrs
			WriteUInt32(buffer, 38, 0); // external file attrs (was 0x8100, but this is system dependent)
			WriteUInt32(buffer, 42, entry.HeaderOffset);
			WriteBytes(buffer, 46, fileNameBytes);
			WriteBytes(buffer, 46 + fileNameBytes.Length, commentBytes);

			_zipStream.Write(buffer, 0, buffer.Length);
		}

		// The end-of-central-dir record:
		//
		// Offset Bytes Description
		//    0     4   Signature = 50 4B 05 06 (hex bytes)
		//    4     2   Number of this disk
		//    6     2   Number of disk where central directory starts
		//    8     2   Number of central directory records on this disk
		//   10     2   Total number of central directory records
		//   12     4   Size of central directory in bytes
		//   16     4   Offset of start of central directory, relative to start of archive
		//   20     2   Comment length (n)
		//   22     n   Comment

		private void WriteEndRecord(uint size, uint offset)
		{
			Encoding encoding = UseUtf8 ? Utf8Encoding : DefaultEncoding;
			byte[] commentBytes = encoding.GetBytes(Comment ?? string.Empty);

			var entryCount = (ushort) (_files.Count + _existingFiles);
			var commentLength = (ushort) commentBytes.Length;

			var buffer = new byte[22 + commentBytes.Length];

			WriteUInt32(buffer, 0, 0x06054B50);
			WriteUInt16(buffer, 4, 0); // this disk
			WriteUInt16(buffer, 6, 0); // central dir disk
			WriteUInt16(buffer, 8, entryCount);
			WriteUInt16(buffer, 10, entryCount);
			WriteUInt32(buffer, 12, size); // size of central dir
			WriteUInt32(buffer, 16, offset); // start of central dir
			WriteUInt16(buffer, 20, commentLength);
			WriteBytes(buffer, 22, commentBytes);

			_zipStream.Write(buffer, 0, buffer.Length);
		}

		private void WriteFileData(ZipFileEntry entry, Stream sourceStream)
		{
			var buffer = new byte[16384];
			int bytesRead;
			uint totalRead = 0;

			long posStart = _zipStream.Position;
			long sourceStart = sourceStream.Position;

			Stream targetStream = entry.Method == CompressionMethod.Deflate
			                      	? new DeflateStream(_zipStream, CompressionMode.Compress, true)
			                      	: _zipStream;

			uint crc32 = 0 ^ 0xffffffff;

			do
			{
				bytesRead = sourceStream.Read(buffer, 0, buffer.Length);
				if (bytesRead > 0)
				{
					totalRead += (uint) bytesRead;

					targetStream.Write(buffer, 0, bytesRead);

					crc32 = UpdateCrc32(crc32, buffer, bytesRead);
				}
			} while (bytesRead == buffer.Length);

			targetStream.Flush();

			if (entry.Method == CompressionMethod.Deflate)
			{
				targetStream.Dispose();
			}

			entry.Crc32 = crc32 ^ 0xffffffff;
			entry.OriginalSize = totalRead;
			entry.CompressedSize = (uint) (_zipStream.Position - posStart);

			// Lossless compression algorithms may enlarge the data!
			// If this happened, try again without compression:

			if (entry.Method == CompressionMethod.Deflate && !ForceDeflating &&
			    sourceStream.CanSeek && entry.CompressedSize > entry.OriginalSize)
			{
				entry.Method = CompressionMethod.Store;
				_zipStream.Position = posStart; // rewind
				_zipStream.SetLength(posStart); // truncate
				sourceStream.Position = sourceStart;
				WriteFileData(entry, sourceStream);
			}
		}

		private static uint[] CreateCrc32Table()
		{
			var table = new uint[256];

			for (int i = 0; i < table.Length; i++)
			{
				var c = (uint) i;
				for (int j = 0; j < 8; j++)
				{
					if ((c & 1) != 0)
					{
						c = 3988292384 ^ (c >> 1);
					}
					else
					{
						c >>= 1;
					}
				}
				table[i] = c;
			}

			return table;
		}

		private static uint UpdateCrc32(uint value, byte[] bytes, int length)
		{
			for (int i = 0; i < length; i++)
			{
				value = CrcTable[(value ^ bytes[i]) & 0xFF] ^ (value >> 8);
			}

			return value;
		}

		/* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning
          is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value
          of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this
          field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central
          directory.
        */

		// Replaces backslashes with slashes to store in zip header
		private static string GetNormalizedFilename(string filename)
		{
			filename = filename.Replace('\\', '/');

			int pos = filename.IndexOf(':');
			if (pos >= 0)
			{
				filename = filename.Remove(0, pos + 1);
			}

			return filename.Trim('/');
		}

		private void ReadEndRecord()
		{
			if (_zipStream.Length < 22)
			{
				throw new InvalidDataException("Zip file must be at least 22 byte long");
			}

			var bufferSize = (int) Math.Min(8192, _zipStream.Length);

			var buffer = new byte[bufferSize];
			_zipStream.Seek(-bufferSize, SeekOrigin.End);
			_zipStream.Read(buffer, 0, bufferSize);

			int start = FindEndSignature(buffer);

			if (start < 0)
			{
				throw new InvalidDataException("End of central directory signature not found");
			}

			var entryCount = ReadUInt16(buffer, start + 10);
			var centralDirBytes = ReadUInt32(buffer, start + 12);
			var centralDirOffset = ReadUInt32(buffer, start + 16);
			var commentLength = ReadUInt16(buffer, start + 20);

			if (start + 22 + commentLength != bufferSize)
			{
				throw new InvalidDataException("End of central directory record seems corrupt");
			}

			if (centralDirBytes > int.MaxValue)
			{
				throw new NotSupportedException("Central directory is too large for us");
			}

			if (commentLength > 0)
			{
				Comment = DecodeGeneralComment(buffer, start+22, commentLength);
			}

			_existingFiles = entryCount;
			_centralDirImage = new byte[centralDirBytes];
			_zipStream.Seek(centralDirOffset, SeekOrigin.Begin);
			_zipStream.Read(_centralDirImage, 0, (int) centralDirBytes);

			// Position stream at beginning of central dir (to append new files)
			_zipStream.Seek(centralDirOffset, SeekOrigin.Begin);
		}

		private static string DecodeGeneralComment(byte[] bytes, int start, ushort length)
		{
			// How is the general file comment encoded?
			// Cannot find anything in the sepc, so I'll just try CP 437

			try
			{
				return DefaultEncoding.GetString(bytes, start, length);
			}
			catch
			{
				return string.Format("(cannot decode {0} comment byte{1})",
				                     length, length == 1 ? "" : "s");
			}
		}

		private static int FindEndSignature(byte[] buffer)
		{
			// The end signature, 0x06054B50, is stored little-endian
			// (as all Zip values) and at least 22 bytes from the end.

			const byte sig0 = 0x50;
			const byte sig1 = 0x4B;
			const byte sig2 = 0x05;
			const byte sig3 = 0x06;

			for (int index = buffer.Length - 22; index >= 0; index--)
			{
				int test = index;
				if (buffer[test] == sig0 &&
				    buffer[++test] == sig1 &&
				    buffer[++test] == sig2 &&
				    buffer[++test] == sig3)
				{
					return index;
				}
			}

			return -1; // not found
		}

		private static void WriteBytes(byte[] bytes, int index, params byte[] source)
		{
			for (int i = 0; i < source.Length; i++)
			{
				bytes[index + i] = source[i];
			}
		}

		private static ushort ReadUInt16(byte[] bytes, int index)
		{
			// Zip uses little-endian for multi-byte values
			byte b0 = bytes[index + 0];
			byte b1 = bytes[index + 1];
			return unchecked((ushort) (b0 | (b1 << 8)));
		}

		private static void WriteUInt16(byte[] bytes, int index, ushort value)
		{
			// Write the low 16 bits of value in little endian
			bytes[index + 0] = (byte)(value & 255);
			bytes[index + 1] = (byte)((value >> 8) & 255);
		}

		private static uint ReadUInt32(byte[] bytes, int index)
		{
			// Zip uses little-endian for multi-byte values
			uint b0 = bytes[index + 0];
			uint b1 = bytes[index + 1];
			uint b2 = bytes[index + 2];
			uint b3 = bytes[index + 3];
			return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
		}

		private static void WriteUInt32(byte[] bytes, int index, uint value)
		{
			// Write the low 32 bits of value in little endian
			bytes[index + 0] = (byte)(value & 255);
			bytes[index + 1] = (byte)((value >> 8) & 255);
			bytes[index + 2] = (byte)((value >> 16) & 255);
			bytes[index + 3] = (byte)((value >> 24) & 255);
		}

		private static DateTime ReadDateTime(byte[] bytes, int index)
		{
			// Zip uses DOS time stamps; time is first:
			//   bits 0..4 = seconds divided by 2
			//   bits 5..10 = minutes (0..59)
			//   bits 11..15 = hours (0..23)
			// followed by the date:
			//   bits 16..20 = day of month (1..31)
			//   bits 21..24 = month (1..12)
			//   bits 25..31 = years after 1980
			// Notice the two-second resolution and
			// the end of time in year 1980+127=2107

			int stamp = unchecked((int) ReadUInt32(bytes, index));

			int year = 1980 + ((stamp >> 25) & 127);
			int month = (stamp >> 21) & 15;
			int day = (stamp >> 16) & 31;
			int hour = (stamp >> 11) & 31;
			int minute = (stamp >> 5) & 63;
			int second = (stamp & 31) << 1;

			return new DateTime(year, month, day, hour, minute, second);
		}

		private static void WriteDateTime(byte[] bytes, int index, DateTime value)
		{
			uint second = (uint) ((value.Second >> 1) & 31);
			uint minute = (uint) (value.Minute & 63);
			uint hour = (uint) (value.Hour & 31);
			uint day = (uint) (value.Day & 31);
			uint month = (uint) (value.Month & 15);
			uint year = (uint) ((value.Year - 1980) & 127);

			uint stamp = second | minute << 5 | hour << 11 |
			             day << 16 | month << 21 | year << 25;

			WriteUInt32(bytes, index, stamp);
		}

		#endregion
	}
}
