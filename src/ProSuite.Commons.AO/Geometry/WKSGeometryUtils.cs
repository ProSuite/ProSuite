using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry
{
	public static class WksGeometryUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static double GetXyDistance(WKSPointZ a, WKSPointZ b)
		{
			return Math.Sqrt(GetXyDistanceSquared(a, b));
		}

		public static double GetXyDistanceSquared(WKSPointZ a, WKSPointZ b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;

			return dx * dx + dy * dy;
		}

		public static WKSPointVA CreateWksPointVa(WKSPointZ wksPointZ)
		{
			var wksPoint = new WKSPointVA
			               {
				               m_x = wksPointZ.X,
				               m_y = wksPointZ.Y,
				               m_z = wksPointZ.Z
			               };

			return wksPoint;
		}

		public static WKSPointVA CreateWksPointVa(IPoint point)
		{
			var wksPoint = new WKSPointVA
			               {
				               m_x = point.X,
				               m_y = point.Y,
				               m_z = point.Z,
				               m_m = point.M
			               };

			return wksPoint;
		}

		public static WKSEnvelope GetWksEnvelope([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var envelope = geometry as IEnvelope;

			if (envelope != null)
			{
				return GetWksEnvelope(envelope);
			}

			// TODO: Consider a ThreadLocal field to use QueryEnvelope
			envelope = geometry.Envelope;

			WKSEnvelope result = GetWksEnvelope(envelope);

			Marshal.ReleaseComObject(envelope);

			return result;
		}

		public static WKSEnvelope GetWksEnvelope([NotNull] IEnvelope envelope)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			WKSEnvelope result;

			if (envelope.IsEmpty)
			{
				// make it very clear - normally some of the coordinates could also be initialized as 0
				return CreateEmptyEnvelope();
			}

			envelope.QueryWKSCoords(out result);

			return result;
		}

		/// <summary>
		/// Creates an empty envelope that will be recognizable as empty geometry.
		/// ArcObjects creates empty wks envelopes with non-initialized double values, i.e. some are NaN, some are 0.
		/// </summary>
		/// <returns></returns>
		public static WKSEnvelope CreateEmptyEnvelope()
		{
			return CreateWksEnvelope(double.NaN, double.NaN, double.NaN, double.NaN);
		}

		public static WKSEnvelope CreateWksEnvelope(double xMin, double yMin,
		                                            double xMax, double yMax)
		{
			var result = new WKSEnvelope
			             {
				             XMin = xMin,
				             YMin = yMin,
				             XMax = xMax,
				             YMax = yMax
			             };

			return result;
		}

		public static WKSEnvelope Expand(WKSEnvelope envelope, double dx, double dy,
		                                 bool asRatio = false)
		{
			if (asRatio)
			{
				double width = envelope.XMax - envelope.XMin;
				double height = envelope.YMax - envelope.YMin;

				// same logic as IEnvelope.Expand: move each side by half of the specified percentage.
				dx = width * (dx - 1) / 2;
				dy = height * (dy - 1) / 2;
			}

			envelope.XMin = envelope.XMin - dx;
			envelope.YMin = envelope.YMin - dy;
			envelope.XMax = envelope.XMax + dx;
			envelope.YMax = envelope.YMax + dy;

			return envelope;
		}

		/// <summary>
		/// Determines whether an envelope contains a point, i.e. the point is not disjoint and not
		/// on the envelope's boundary. No tolerance is applied.
		/// </summary>
		/// <param name="envelope">The envelope (containing)</param>
		/// <param name="point">The point (contained)</param>
		/// <returns>
		/// 	<c>true</c> if the envelope contains the point; otherwise, <c>false</c>.
		/// </returns>
		public static bool Contains2D(WKSEnvelope envelope, WKSPointVA point)
		{
			if (point.m_x < envelope.XMin)
			{
				return false;
			}

			if (point.m_y < envelope.YMin)
			{
				return false;
			}

			if (point.m_x > envelope.XMax)
			{
				return false;
			}

			if (point.m_y > envelope.YMax)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns a <see cref="System.String"></see> that represents the specified <see cref="WKSEnvelope"></see>.
		/// </summary>
		/// <param name="wksEnvelope">The envelope to convert to a string.</param>
		/// <param name="numberOfDecimals">The number of decimals for the printed coordinate values.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"></see> that represents the specified <see cref="WKSEnvelope"></see>.
		/// </returns>
		[NotNull]
		public static string ToString(WKSEnvelope wksEnvelope, int numberOfDecimals = 3)
		{
			try
			{
				string format =
					string.Format(
						"XMin: {{0:N{0}}} YMin: {{1:N{0}}} XMax: {{2:N{0}}} YMax: {{3:N{0}}}",
						numberOfDecimals);

				return string.Format(format, wksEnvelope.XMin, wksEnvelope.YMin,
				                     wksEnvelope.XMax, wksEnvelope.YMax);
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		public static string ToString(WKSPointVA point, int numberOfDecimals = 3)
		{
			try
			{
				string format =
					string.Format(
						"X: {{0:N{0}}} Y: {{1:N{0}}} Z: {{2:N{0}}} M: {{3:N{0}}}",
						numberOfDecimals);

				return string.Format(format, point.m_x, point.m_y,
				                     point.m_z, point.m_m);
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		[NotNull]
		private static string HandleToStringException(Exception e)
		{
			string msg = string.Format("Error converting to string: {0}",
			                           e.Message);
			_msg.Debug(msg, e);
			return msg;
		}

		public static double Area(WKSEnvelope envelope)
		{
			return Width(envelope) * Height(envelope);
		}

		public static double Width(WKSEnvelope envelope)
		{
			return envelope.XMax - envelope.XMin;
		}

		public static double Height(WKSEnvelope envelope)
		{
			return envelope.YMax - envelope.YMin;
		}

		public static bool IsEmpty(WKSEnvelope wksEnvelope)
		{
			// Typically an empty envelope from ArcObjects can also have some (all?) coordinates initialized as 0
			// Empty envelopes created here only have NaN coordinates. 
			if (double.IsNaN(wksEnvelope.XMin) || double.IsNaN(wksEnvelope.XMin) ||
			    double.IsNaN(wksEnvelope.XMax) || double.IsNaN(wksEnvelope.YMax))
			{
				return true;
			}

			return false;
		}
	}
}
