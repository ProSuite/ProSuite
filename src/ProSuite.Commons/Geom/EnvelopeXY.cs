using System.Diagnostics.CodeAnalysis;
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
			return Format(this);
		}

		[NotNull]
		public string Format([CanBeNull] EnvelopeXY envelope,
		                     int? significantDigits = null)
		{
			if (envelope == null)
			{
				return "<null>";
			}

			return $"XMin: {Format(envelope.XMin, significantDigits)} " +
			       $"YMin: {Format(envelope.YMin, significantDigits)} " +
			       $"XMax: {Format(envelope.XMax, significantDigits)} " +
			       $"YMax: {Format(envelope.YMax, significantDigits)}";
		}

		private static string Format(double coordinate, int? significantDigits)
		{
			string format = significantDigits == null ? "{0}" : $"{{0:N{significantDigits.Value}}}";

			return string.Format(format, coordinate);
		}
	}
}
