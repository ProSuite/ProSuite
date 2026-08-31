using System.IO;
using NUnit.Framework;
using ProSuite.Commons.PointCloud;

namespace ProSuite.Commons.Test.PointCloud
{
	/// <summary>
	/// Reading and validating the public header block of a LAS file, which is what a quality test
	/// looks at before it trusts anything else about the file.
	/// </summary>
	[TestFixture]
	public class LasHeaderInfoTest
	{
		private string _directory;

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(Path.GetTempPath(),
			                          $"{nameof(LasHeaderInfoTest)}_{Path.GetRandomFileName()}");

			Directory.CreateDirectory(_directory);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, true);
			}
		}

		[Test]
		public void Can_read_the_header_of_a_valid_file()
		{
			string path = LasTestFile.Write(
				GetPath("valid.las"),
				minX: 2600000, minY: 1200000, maxX: 2601000, maxY: 1201000,
				minZ: 400, maxZ: 1500);

			Assert.IsTrue(LasHeaderInfo.TryRead(path, out LasHeaderInfo header, out string message),
			              message);

			Assert.NotNull(header);
			Assert.AreEqual("1.2", header.Version);
			Assert.AreEqual(LasTestFile.HeaderSize, header.HeaderSize);
			Assert.AreEqual(LasTestFile.HeaderSize, header.OffsetToPointData);
			Assert.AreEqual(0, header.PointDataRecordFormat);
			Assert.AreEqual(LasTestFile.PointRecordLength, header.PointDataRecordLength);
			Assert.AreEqual(4, header.PointCount);

			Assert.AreEqual(2600000, header.MinX);
			Assert.AreEqual(2601000, header.MaxX);
			Assert.AreEqual(1200000, header.MinY);
			Assert.AreEqual(1201000, header.MaxY);
			Assert.AreEqual(400, header.MinZ);
			Assert.AreEqual(1500, header.MaxZ);
		}

		[Test]
		public void A_missing_file_is_reported_not_thrown()
		{
			Assert.IsFalse(LasHeaderInfo.TryRead(GetPath("absent.las"), out LasHeaderInfo header,
			                                     out string message));

			Assert.IsNull(header);
			Assert.IsNotNull(message);
			StringAssert.Contains("exist", message);
		}

		[Test]
		public void A_file_that_is_not_a_las_file_is_rejected()
		{
			string path = LasTestFile.WriteWithWrongSignature(GetPath("notlas.las"));

			Assert.IsFalse(LasHeaderInfo.TryRead(path, out LasHeaderInfo header,
			                                     out string message));

			Assert.IsNull(header);
			StringAssert.Contains("LASF", message);
		}

		[Test]
		public void A_truncated_header_is_rejected()
		{
			// The header alone is 227 bytes: an interrupted delivery cannot be read at all, and
			// must not be mistaken for a file whose bounding box happens to be zero.
			string path = LasTestFile.WriteTruncated(GetPath("truncated.las"), byteCount: 100);

			Assert.IsFalse(LasHeaderInfo.TryRead(path, out LasHeaderInfo header,
			                                     out string message));

			Assert.IsNull(header);
			StringAssert.Contains("Truncated", message);
		}

		[Test]
		public void An_inverted_bounding_box_is_rejected()
		{
			string path = LasTestFile.WriteWithInvertedBoundingBox(GetPath("inverted.las"));

			Assert.IsFalse(LasHeaderInfo.TryRead(path, out LasHeaderInfo header,
			                                     out string message));

			Assert.IsNull(header);
			StringAssert.Contains("inverted", message);
		}

		[Test]
		public void An_empty_file_is_rejected()
		{
			string path = GetPath("empty.las");

			File.WriteAllBytes(path, new byte[0]);

			Assert.IsFalse(LasHeaderInfo.TryRead(path, out LasHeaderInfo header,
			                                     out string message));

			Assert.IsNull(header);
			Assert.IsNotNull(message);
		}

		private string GetPath(string fileName)
		{
			return Path.Combine(_directory, fileName);
		}
	}
}
