using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class AreaPrediction
	{
		/// <summary>
		/// Exterior polygon shell in the uploaded TIFF's map coordinate system.
		/// </summary>
		public AreaPrediction([NotNull] IList<PredictionPoint> shell)
		{
			Assert.ArgumentNotNull(shell, nameof(shell));
			Assert.ArgumentCondition(shell.Count >= 3,
			                         "Prediction polygon shell must contain at least three points");

			foreach (PredictionPoint point in shell)
			{
				Assert.ArgumentCondition(IsFinite(point.X) && IsFinite(point.Y),
				                         "Prediction polygon shell contains a non-finite coordinate");
			}

			Shell = shell;
		}

		[NotNull]
		public IList<PredictionPoint> Shell { get; }

		private static bool IsFinite(double value)
		{
			return ! double.IsNaN(value) && ! double.IsInfinity(value);
		}
	}

	public struct PredictionPoint
	{
		public PredictionPoint(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; }

		public double Y { get; }
	}
}
