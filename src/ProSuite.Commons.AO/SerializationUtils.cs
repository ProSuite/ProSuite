using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO
{
	/// <summary>
	/// Helper methods to serialize com types
	/// </summary>
	public static class SerializationUtils
	{
		/// <summary>
		/// Serializes a given COM object.
		/// </summary>
		/// <param name="persistableComObject">The persistable COM object (the object must implement <see cref="IPersistStream"></see>).</param>
		/// <returns></returns>
		[NotNull]
		public static byte[] SerializeComObject([NotNull] object persistableComObject)
		{
			Assert.ArgumentNotNull(persistableComObject, nameof(persistableComObject));
			Assert.ArgumentCondition(Marshal.IsComObject(persistableComObject),
			                         "not a com object");
			Assert.ArgumentCondition(persistableComObject is IPersistStream,
			                         "the object is not persistable");

			IMemoryBlobStream2 memoryBlobStream = new MemoryBlobStreamClass();
			IVariantStream variantStream = CreateVariantStream(memoryBlobStream);

			var memoryBlobStreamVariant = (IMemoryBlobStreamVariant) memoryBlobStream;

			variantStream.Write(persistableComObject);

			object bytes;
			memoryBlobStreamVariant.ExportToVariant(out bytes);

			return (byte[]) bytes;
		}

		/// <summary>
		/// Deserializes a COM object from a byte array created using <see cref="SerializeComObject"></see>.
		/// </summary>
		/// <param name="bytes">The bytes array.</param>
		/// <returns></returns>
		public static object DeserializeComObject([NotNull] byte[] bytes)
		{
			Assert.ArgumentNotNull(bytes, nameof(bytes));
			Assert.ArgumentCondition(bytes.Length > 0, "byte array is empty");

			IMemoryBlobStream2 memoryBlobStream = new MemoryBlobStreamClass();
			IVariantStream variantStream = CreateVariantStream(memoryBlobStream);

			var memoryBlobStreamVariant = (IMemoryBlobStreamVariant) memoryBlobStream;

			memoryBlobStreamVariant.ImportFromVariant(bytes);

			return variantStream.Read();
		}

		private static IVariantStream CreateVariantStream(IStream stream)
		{
			return new VariantStreamIOClass {Stream = stream};
		}
	}
}
