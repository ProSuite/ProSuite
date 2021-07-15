using System;
using System.ComponentModel;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class ExtentProperties
	{
		public ExtentProperties([NotNull] IEnvelope envelope)
		{
			XMin = envelope.XMin;
			YMin = envelope.YMin;
			XMax = envelope.XMax;
			YMax = envelope.YMax;

			Width = Math.Abs(XMax - XMin);
			Height = Math.Abs(YMax - YMin);
		}

		[DisplayName("X Minimum")]
		[UsedImplicitly]
		public double XMin { get; private set; }

		[DisplayName("Y Minimum")]
		[UsedImplicitly]
		public double YMin { get; private set; }

		[DisplayName("X Maximum")]
		[UsedImplicitly]
		public double XMax { get; private set; }

		[DisplayName("Y Maximum")]
		[UsedImplicitly]
		public double YMax { get; private set; }

		[DisplayName("Width")]
		[UsedImplicitly]
		public double Width { get; private set; }

		[DisplayName("Height")]
		[UsedImplicitly]
		public double Height { get; private set; }

		public override string ToString()
		{
			return string.Format("{0} x {1}", Width, Height);
		}
	}
}
