using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client
{
	public static class ProtobufGeomUtils
	{
		[CanBeNull]
		public static EnvelopeMsg ToEnvelopeMsg([CanBeNull] IBoundedXY envelope)
		{
			if (envelope == null)
			{
				return null;
			}

			var result = new EnvelopeMsg
			             {
				             XMin = envelope.XMin,
				             YMin = envelope.YMin,
				             XMax = envelope.XMax,
				             YMax = envelope.YMax
			             };

			return result;
		}

		/// <summary>
		/// Return null, if the specified string is empty (i.e. the default value for string
		/// protocol buffers), or the input string otherwise.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string EmptyToNull(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}

		/// <summary>
		/// Return the empty string, if the specified string is null, or the input string otherwise.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string NullToEmpty(string value)
		{
			return value ?? string.Empty;
		}
	}
}
