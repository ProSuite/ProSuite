using System;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Tests
{
	internal class AzimuthSegment
	{
		private readonly double _xyResolution;

		public AzimuthSegment([NotNull] SegmentProxy segment, double xyResolution)
		{
			Segment = segment;
			_xyResolution = xyResolution;
			Pnt start = Segment.GetStart(false);
			Pnt end = Segment.GetEnd(false);

			double dx = end.X - start.X;
			double dy = end.Y - start.Y;

			Azimuth = Math.Atan2(dx, dy); // Azimuth is the angle to north

			if (Azimuth < 0) // the orientation must be ignored
			{
				Azimuth += Math.PI;
			}
		}

		public double Azimuth { get; }

		[NotNull]
		public SegmentProxy Segment { get; }

		public override string ToString()
		{
			return string.Format("{0:N3}°", MathUtils.ToDegrees(Azimuth));
		}

		public double GetAzimuthResolution()
		{
			double length = Segment.Length;

			double azimuthResolution = Math.Atan2(_xyResolution, length);
			return azimuthResolution;
		}

		public static int CompareAzimuth(AzimuthSegment x, AzimuthSegment y)
		{
			return x.Azimuth.CompareTo(y.Azimuth);
		}
	}
}
