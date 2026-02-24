using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A polynomial surface that can be fitted to 3D points and used to interpolate Z values.
	/// The polynomial is of the form: z = a₀ + a₁x + a₂y + a₃x² + a₄xy + a₅y² + ...
	/// </summary>
	public class Polynomial3DSurface : ISurface
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// The coefficients of the polynomial, stored in order:
		/// [a₀, a₁, a₂, a₃, a₄, a₅, ...] corresponding to
		/// [1, x, y, x², xy, y², x³, x²y, xy², y³, ...]
		/// </summary>
		[NotNull]
		public double[] Coefficients { get; }

		/// <summary>
		/// The degree of the polynomial (1 = linear, 2 = quadratic, 3 = cubic, etc.)
		/// </summary>
		public int Degree { get; }

		/// <summary>
		/// Centroid used for coordinate normalization to improve numerical stability
		/// </summary>
		[NotNull]
		public Pnt3D Centroid { get; }

		/// <summary>
		/// Scale factor used for coordinate normalization
		/// </summary>
		public double Scale { get; }

		public double Epsilon { get; set; }

		public bool IsDefined => Coefficients != null && Coefficients.Length > 0;

		private Polynomial3DSurface([NotNull] double[] coefficients, int degree,
									[NotNull] Pnt3D centroid, double scale)
		{
			Coefficients = coefficients;
			Degree = degree;
			Centroid = centroid;
			Scale = scale;

			Epsilon = MathUtils.GetDoubleSignificanceEpsilon(coefficients);
		}

		/// <summary>
		/// Attempts to fit a polynomial surface to the given points.
		/// Returns null if insufficient points are provided.
		/// </summary>
		/// <param name="points">The points to fit the polynomial to</param>
		/// <param name="degree">The degree of the polynomial (1 = linear, 2 = quadratic, etc.)</param>
		/// <param name="isRing">Whether the points form a closed ring (last point equals first)</param>
		/// <returns>A Polynomial3D object or null if fitting is not possible</returns>
		[CanBeNull]
		public static Polynomial3DSurface TryFitPolynomial([NotNull] IList<Pnt3D> points,
														   int degree = 2,
														   bool isRing = false)
		{
			int usablePointCount = isRing ? points.Count - 1 : points.Count;
			int requiredCoefficients = GetCoefficientCount(degree);

			if (usablePointCount < requiredCoefficients)
			{
				return null;
			}

			return FitPolynomial(points, degree, isRing);
		}

		/// <summary>
		/// Fits a polynomial surface to the given points using least squares regression
		/// with Tikhonov (ridge) regularization for improved numerical stability.
		/// Coordinates are normalized to the centroid before fitting.
		/// </summary>
		/// <param name="points">The points to fit the polynomial to</param>
		/// <param name="degree">The degree of the polynomial (1 = linear, 2 = quadratic, etc.)</param>
		/// <param name="isRing">Whether the points form a closed ring (last point equals first)</param>
		/// <param name="regularizationLambda">Ridge regression parameter for smoothness.
		/// Set to 0 to disable regularization.</param>
		/// <returns>A Polynomial3DSurface object</returns>
		[NotNull]
		public static Polynomial3DSurface FitPolynomial([NotNull] IList<Pnt3D> points,
														int degree = 2,
														bool isRing = false,
														double regularizationLambda = 1e-6)
		{
			int n = isRing ? points.Count - 1 : points.Count;
			int coeffCount = GetCoefficientCount(degree);

			Assert.ArgumentCondition(n >= coeffCount,
									 $"At least {coeffCount} points are required to fit a polynomial of degree {degree}.");
			Assert.ArgumentCondition(degree >= 1, "Degree must be at least 1.");

			// Calculate centroid for normalization
			var sum = new Pnt3D();
			for (var i = 0; i < n; i++)
			{
				sum += points[i];
			}

			Pnt3D centroid = sum / n;

			// Calculate scale for normalization (use max distance from centroid)
			double maxDist = 0;
			for (var i = 0; i < n; i++)
			{
				Pnt3D point = points[i];
				double dist = Math.Max(Math.Abs(point.X - centroid.X),
									   Math.Abs(point.Y - centroid.Y));
				maxDist = Math.Max(maxDist, dist);
			}

			double scale = maxDist > 0 ? maxDist : 1.0;

			// Build the design matrix A and observation vector b
			// For each point, we have: z = a₀ + a₁x + a₂y + a₃x² + a₄xy + a₅y² + ...
			double[,] AtA = new double[coeffCount, coeffCount];
			double[] Atb = new double[coeffCount];

			for (var i = 0; i < n; i++)
			{
				Pnt3D point = points[i];

				// Normalize coordinates
				double xNorm = (point.X - centroid.X) / scale;
				double yNorm = (point.Y - centroid.Y) / scale;
				double z = point.Z;

				// Generate polynomial terms for this point
				double[] terms = GeneratePolynomialTerms(xNorm, yNorm, degree);

				// Build normal equations: A^T * A * coeffs = A^T * b
				for (int j = 0; j < coeffCount; j++)
				{
					for (int k = 0; k < coeffCount; k++)
					{
						AtA[j, k] += terms[j] * terms[k];
					}

					Atb[j] += terms[j] * z;
				}
			}

			// Apply Tikhonov (ridge) regularization: add λI to AᵀA.
			// This biases toward smaller coefficients, producing smoother surfaces
			// and preventing blow-up when AᵀA is ill-conditioned.
			if (regularizationLambda > 0)
			{
				for (int j = 0; j < coeffCount; j++)
				{
					AtA[j, j] += regularizationLambda;
				}
			}

			// Solve the normal equations using Gaussian elimination
			double[] coefficients = SolveLinearSystem(AtA, Atb);

			var result = new Polynomial3DSurface(coefficients, degree, centroid, scale);

			return result;
		}

		/// <summary>
		/// Gets the Z value at the specified X and Y coordinates.
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>The interpolated Z value</returns>
		public double GetZ(double x, double y)
		{
			if (!IsDefined)
			{
				throw new InvalidOperationException("Polynomial is not defined.");
			}

			// Normalize coordinates
			double xNorm = (x - Centroid.X) / Scale;
			double yNorm = (y - Centroid.Y) / Scale;

			// Generate polynomial terms
			double[] terms = GeneratePolynomialTerms(xNorm, yNorm, Degree);

			// Calculate z value
			double z = 0;
			for (int i = 0; i < Coefficients.Length; i++)
			{
				z += Coefficients[i] * terms[i];
			}

			return z;
		}

		/// <summary>
		/// Gets the Z value at the specified point (uses X and Y coordinates).
		/// </summary>
		/// <param name="point">The point</param>
		/// <returns>The interpolated Z value</returns>
		public double GetZ([NotNull] Pnt3D point)
		{
			return GetZ(point.X, point.Y);
		}

		/// <summary>
		/// Calculates the residual (difference between actual and fitted Z) for a point.
		/// </summary>
		/// <param name="point">The point to evaluate</param>
		/// <returns>The residual (actual Z - fitted Z)</returns>
		public double GetResidual([NotNull] Pnt3D point)
		{
			return point.Z - GetZ(point.X, point.Y);
		}

		/// <summary>
		/// Calculates the root mean square error of the fit for the given points.
		/// </summary>
		/// <param name="points">Points to evaluate</param>
		/// <param name="isRing">Whether the points form a closed ring</param>
		/// <returns>The RMSE value</returns>
		public double GetRMSE([NotNull] IList<Pnt3D> points, bool isRing = false)
		{
			int n = isRing ? points.Count - 1 : points.Count;

			if (n == 0)
			{
				return 0;
			}

			double sumSquaredResiduals = 0;
			for (int i = 0; i < n; i++)
			{
				double residual = GetResidual(points[i]);
				sumSquaredResiduals += residual * residual;
			}

			return Math.Sqrt(sumSquaredResiduals / n);
		}

		public override string ToString()
		{
			return $"Polynomial3D (Degree: {Degree}, Coefficients: {Coefficients.Length})";
		}

		/// <summary>
		/// Calculates the number of coefficients needed for a polynomial of given degree.
		/// For degree d, the number of terms is (d+1)(d+2)/2
		/// </summary>
		private static int GetCoefficientCount(int degree)
		{
			return (degree + 1) * (degree + 2) / 2;
		}

		/// <summary>
		/// Generates all polynomial terms up to the specified degree.
		/// Terms are ordered: 1, x, y, x², xy, y², x³, x²y, xy², y³, ...
		/// Uses direct multiplication instead of Math.Pow for better precision.
		/// </summary>
		private static double[] GeneratePolynomialTerms(double x, double y, int degree)
		{
			int count = GetCoefficientCount(degree);
			double[] terms = new double[count];

			// Pre-compute powers via direct multiplication
			double[] xPow = new double[degree + 1];
			double[] yPow = new double[degree + 1];
			xPow[0] = 1.0;
			yPow[0] = 1.0;
			for (int p = 1; p <= degree; p++)
			{
				xPow[p] = xPow[p - 1] * x;
				yPow[p] = yPow[p - 1] * y;
			}

			// Generate terms by total degree: 1, x, y, x², xy, y², ...
			int index = 0;
			for (int d = 0; d <= degree; d++)
			{
				for (int xDeg = d; xDeg >= 0; xDeg--)
				{
					int yDeg = d - xDeg;
					terms[index++] = xPow[xDeg] * yPow[yDeg];
				}
			}

			return terms;
		}

		/// <summary>
		/// Solves a linear system Ax = b using Gaussian elimination with partial pivoting.
		/// </summary>
		private static double[] SolveLinearSystem(double[,] A, double[] b)
		{
			int n = b.Length;
			double[,] augmented = new double[n, n + 1];

			// Create augmented matrix [A|b]
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					augmented[i, j] = A[i, j];
				}

				augmented[i, n] = b[i];
			}

			// Forward elimination with partial pivoting
			for (int k = 0; k < n; k++)
			{
				// Find pivot
				int maxRow = k;
				double maxVal = Math.Abs(augmented[k, k]);
				for (int i = k + 1; i < n; i++)
				{
					double val = Math.Abs(augmented[i, k]);
					if (val > maxVal)
					{
						maxVal = val;
						maxRow = i;
					}
				}

				// Swap rows
				if (maxRow != k)
				{
					for (int j = k; j <= n; j++)
					{
						double temp = augmented[k, j];
						augmented[k, j] = augmented[maxRow, j];
						augmented[maxRow, j] = temp;
					}
				}

				// Check for singular matrix
				if (Math.Abs(augmented[k, k]) < 1e-10)
				{
					_msg.Warn("Matrix is singular or nearly singular. Cannot solve the system.");
				}

				// Eliminate column
				for (int i = k + 1; i < n; i++)
				{
					double factor = augmented[i, k] / augmented[k, k];
					for (int j = k; j <= n; j++)
					{
						augmented[i, j] -= factor * augmented[k, j];
					}
				}
			}

			// Back substitution
			double[] x = new double[n];
			for (int i = n - 1; i >= 0; i--)
			{
				double sum = augmented[i, n];
				for (int j = i + 1; j < n; j++)
				{
					sum -= augmented[i, j] * x[j];
				}

				x[i] = sum / augmented[i, i];
			}

			return x;
		}
	}
}
