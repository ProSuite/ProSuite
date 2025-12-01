using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Encapsulates the coordinates of a 2D envelope for high-performance use cases.
	/// Box provides more functionality but allocates arrays when created.
	/// </summary>
	public class EnvelopeXY : IBoundedXY
	{
		public double XMin { get; set; }
		public double YMin { get; set; }
		public double XMax { get; set; }
		public double YMax { get; set; }

		// TODO: Support and manage empty-ness and invalidity

		public EnvelopeXY(double xMin, double yMin, double xMax, double yMax)
		{
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
		}

		public EnvelopeXY(IBoundedXY geometry) : this(geometry.XMin, geometry.YMin,
		                                              geometry.XMax, geometry.YMax) { }

		public double Width => XMax - XMin;
		public double Height => YMax - YMin;

		public void EnlargeToInclude(IBoundedXY other)
		{
			if (other.XMin < XMin)
				XMin = other.XMin;

			if (other.YMin < YMin)
				YMin = other.YMin;

			if (other.XMax > XMax)
				XMax = other.XMax;

			if (other.YMax > YMax)
				YMax = other.YMax;
		}

		public Pnt2D GetCenterPoint()
		{
			double centerX = (XMin + XMax) / 2;
			double centerY = (YMin + YMax) / 2;

			return new Pnt2D(centerX, centerY);
		}

		public Pnt2D GetLowerLeftPoint()
		{
			return new Pnt2D(XMin, YMin);
		}

		public Pnt2D GetUpperRightPoint()
		{
			return new Pnt2D(XMax, YMax);
		}

        public void Expand(double dx, double dy, bool asRatio)
        {
            if (asRatio)
            {
                double width = Width;
                double height = Height;

                XMin -= dx * width;
                XMax += dx * width;
                YMin -= dy * height;
                YMax += dy * height;
            }
            else
            {
                XMin -= dx;
                XMax += dx;
                YMin -= dy;
                YMax += dy;
            }
        }

		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		public bool Equals([CanBeNull] EnvelopeXY other,
		                   double tolerance)
		{
			if (other == null)
			{
				return false;
			}

			return MathUtils.AreEqual(XMin, other.XMin, tolerance) &&
			       MathUtils.AreEqual(YMin, other.YMin, tolerance) &&
			       MathUtils.AreEqual(XMax, other.XMax, tolerance) &&
			       MathUtils.AreEqual(YMax, other.YMax, tolerance);
		}

		protected bool Equals(EnvelopeXY other)
		{
			return XMin.Equals(other.XMin) && YMin.Equals(other.YMin) && XMax.Equals(other.XMax) &&
			       YMax.Equals(other.YMax);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((EnvelopeXY) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = XMin.GetHashCode();
				hashCode = (hashCode * 397) ^ YMin.GetHashCode();
				hashCode = (hashCode * 397) ^ XMax.GetHashCode();
				hashCode = (hashCode * 397) ^ YMax.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			// For debugging: Use invariant, without thousand-separator, same as double.ToString()

			return FormatBounds(this, null);
		}

		public string ToString([CanBeNull] CultureInfo cultureInfo,
		                       int? decimalDigits = null)
		{
			if (decimalDigits == null)
			{
				decimalDigits = EstimateSignificantDigits(this);
			}

			if (cultureInfo == null)
			{
				// Manually emulating the default double.ToString() with no thousand-separators and . as decimal point
				return FormatBounds(this, null, decimalDigits);
			}
			else
			{
				cultureInfo = (CultureInfo) cultureInfo.Clone();
			}

			// Apply the significant digits to a clone of the culture info
			cultureInfo.NumberFormat.NumberDecimalDigits = decimalDigits.Value;

			IFormatProvider formatProvider = cultureInfo.NumberFormat;

			// For debugging: Typically called using invariant culture, use default significant digits:
			return FormatBounds(this, formatProvider, decimalDigits);
		}

		[NotNull]
		private static string FormatBounds([CanBeNull] IBoundedXY envelope,
		                                   [CanBeNull] IFormatProvider formatProvider,
		                                   int? significantDigits = null)
		{
			if (envelope == null)
			{
				return "<null>";
			}

			return $"XMin: {Format(formatProvider, envelope.XMin, significantDigits)} " +
			       $"YMin: {Format(formatProvider, envelope.YMin, significantDigits)} " +
			       $"XMax: {Format(formatProvider, envelope.XMax, significantDigits)} " +
			       $"YMax: {Format(formatProvider, envelope.YMax, significantDigits)}";
		}

		private static string Format([CanBeNull] IFormatProvider formatProvider,
		                             double coordinate,
		                             int? significantDigits)
		{
			string numberFormat;
			if (formatProvider != null)
			{
				numberFormat = significantDigits == null
					               ? ":N"
					               : $":N{significantDigits.Value}";
			}
			else
			{
				numberFormat = significantDigits == null
					               ? string.Empty
					               : $":F{significantDigits.Value}";
			}

			string format = significantDigits == null ? "{0}" : $"{{0{numberFormat}}}";

			return string.Format(formatProvider, format, coordinate);
		}

		private static int EstimateSignificantDigits(IBoundedXY envelopeXY)
		{
			if (envelopeXY.XMin > -180 && envelopeXY.XMax < 180 &&
			    envelopeXY.YMin > -90 && envelopeXY.YMax < 90)
			{
				// Most likely geographic coordinates
				return 7;
			}

			return 3;
		}
	}
}
