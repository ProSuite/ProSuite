using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client
{
	public static class ProtobufConversionUtils
	{
		[CanBeNull]
		public static EnvelopeMsg ToEnvelopeMsg([CanBeNull] EnvelopeXY envelope)
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
	}
}
