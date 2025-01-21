using System.Collections.Generic;
using System.Windows;
using NUnit.Framework;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Test
{
	[TestFixture]
	public class WindowPositionerTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void FindPositionWithoutAreasToAvoid()
		{
			List<Rect> preferredAreas = [];
			List<Rect> areasToAvoid = [];

			TestConfiguration(new Point(500, 500), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect,
			                  new Point(500, 500));
			TestConfiguration(new Point(500, 500), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect,
			                  new Point(500, 500));

			TestConfiguration(new Point(-10, 500), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(0, 500));

			TestConfiguration(new Point(500, -25), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect, new Point(499.5, 0));

			TestConfiguration(new Point(-50, -50), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft, new Point(5, 5), 5);
			TestConfiguration(new Point(-15, -25), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect, new Point(5, 5), 5);

			preferredAreas.Add(new Rect(0, 0, 500, 500));

			TestConfiguration(new Point(500, 500), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(390, 390), 10);

			TestConfiguration(new Point(400, 600), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(400, 400), 1e-7);

			TestConfiguration(new Point(700, 300), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(400, 300), 1e-7);
		}

		[Test]
		public void FindPositionWithAreasToAvoid()
		{
			List<Rect> preferredAreas = [new Rect(0, 0, 500, 500)];
			List<Rect> areasToAvoid = [new Rect(200, 200, 50, 50)];

			TestConfiguration(new Point(90, 90), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(90, 90));

			TestConfiguration(new Point(230, 230), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(251, 230));
			TestConfiguration(new Point(230, 230), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect,
			                  new Point(230, 250), 5);

			TestConfiguration(new Point(200, 125), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToTopLeft,
			                  new Point(206, 100), 1);
			TestConfiguration(new Point(200, 125), preferredAreas, areasToAvoid,
			                  WindowPositioner.EvaluationMethod.DistanceToRect, new Point(200, 25), 2);
		}

		private static void TestConfiguration(Point initialPosition, List<Rect> preferredAreas,
		                                      List<Rect> areasToAvoid,
		                                      WindowPositioner.EvaluationMethod method,
		                                      Point expectedResult, double tolerance = double.Epsilon)
		{
			const double rectWidth = 100;
			const double rectHeight = 100;

			// Calculate position
			WindowPositioner positioner =
				new WindowPositioner(preferredAreas, areasToAvoid, method);
			var result = positioner.FindSuitablePosition(initialPosition, rectWidth, rectHeight);

			// Verify result
			Rect resultRect = new Rect(result.X, result.Y, rectWidth, rectHeight);
			foreach (Rect preferredArea in preferredAreas)
			{
				Assert.True(preferredArea.Contains(resultRect));
			}
			foreach (Rect areaToAvoid in areasToAvoid)
			{
				Assert.True(!areaToAvoid.Contains(resultRect));
			}
			Assert.True((result - expectedResult).Length < tolerance);
		}
	}
}
