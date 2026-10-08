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

			TestConfiguration(
				new Point(500, 500), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect,
				new Point(500, 500));
			TestConfiguration(
				new Point(500, 500), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect,
				new Point(500, 500));

			TestConfiguration(
				new Point(-10, 500), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(0, 500));

			TestConfiguration(
				new Point(500, -25), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect, new Point(499.5, 0));

			TestConfiguration(
				new Point(-50, -50), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft, new Point(5, 5), 5);
			TestConfiguration(
				new Point(-15, -25), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect, new Point(5, 5), 5);

			preferredAreas.Add(new Rect(0, 0, 500, 500));

			TestConfiguration(
				new Point(500, 500), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(390, 390), 10);

			TestConfiguration(
				new Point(400, 600), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(400, 400), 1e-7);

			TestConfiguration(
				new Point(700, 300), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(400, 300), 1e-7);
		}

		[Test]
		public void FindPositionWithAreasToAvoid()
		{
			List<Rect> preferredAreas = [new Rect(0, 0, 500, 500)];
			List<Rect> areasToAvoid = [new Rect(200, 200, 50, 50)];

			TestConfiguration(
				new Point(90, 90), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(90, 90));

			TestConfiguration(
				new Point(230, 230), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(251, 230));
			TestConfiguration(
				new Point(230, 230), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect,
				new Point(230, 250), 5);

			TestConfiguration(
				new Point(200, 125), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToTopLeft,
				new Point(206, 100), 1);
			TestConfiguration(
				new Point(200, 125), preferredAreas, areasToAvoid,
				WindowPositioner.EvaluationMethod.DistanceToRect, new Point(200, 25), 2);
		}

		// The monitors of the case in which the picker window appeared on the wrong monitor,
		// in device independent pixels (display scaling 150 %): The laptop monitor is the
		// primary monitor, the external monitor is to the right of it.
		private static readonly Rect _laptopMonitor = new Rect(0, 0, 1707, 1019);
		private static readonly Rect _externalMonitor = new Rect(1707, 0, 2560, 1392);

		[Test]
		public void OrderByDistancePutsMonitorContainingPositionFirst()
		{
			List<Rect> monitors = [_laptopMonitor, _externalMonitor];

			// The mouse location of the observed case: screen pixels 3364,1014 divided by 1.5.
			// In screen pixels the laptop monitor extends to x = 2560, which is why comparing
			// this position with monitors in screen pixels resulted in the laptop monitor.
			List<Rect> ordered =
				WindowPositioner.OrderByDistance(monitors, new Point(2242.67, 676));

			Assert.AreEqual(_externalMonitor, ordered[0]);
			Assert.AreEqual(_laptopMonitor, ordered[1]);

			ordered = WindowPositioner.OrderByDistance(monitors, new Point(800, 500));

			Assert.AreEqual(_laptopMonitor, ordered[0]);
			Assert.AreEqual(_externalMonitor, ordered[1]);
		}

		[Test]
		public void OrderByDistancePutsClosestMonitorFirstForPositionOutsideAllMonitors()
		{
			List<Rect> monitors = [_laptopMonitor, _externalMonitor];

			// Below the laptop monitor (its task bar or the area below the smaller monitor)
			List<Rect> ordered =
				WindowPositioner.OrderByDistance(monitors, new Point(800, 1200));

			Assert.AreEqual(_laptopMonitor, ordered[0]);

			// To the right of the external monitor
			ordered = WindowPositioner.OrderByDistance(monitors, new Point(5000, 500));

			Assert.AreEqual(_externalMonitor, ordered[0]);
		}

		[Test]
		public void OrderByDistanceKeepsInputOrderForEqualDistance()
		{
			// On the shared edge both monitors have the distance 0
			var positionOnSharedEdge = new Point(1707, 500);

			List<Rect> ordered = WindowPositioner.OrderByDistance(
				[_laptopMonitor, _externalMonitor], positionOnSharedEdge);

			Assert.AreEqual(_laptopMonitor, ordered[0]);

			ordered = WindowPositioner.OrderByDistance(
				[_externalMonitor, _laptopMonitor], positionOnSharedEdge);

			Assert.AreEqual(_externalMonitor, ordered[0]);
		}

		private static void TestConfiguration(Point initialPosition, List<Rect> preferredAreas,
		                                      List<Rect> areasToAvoid,
		                                      WindowPositioner.EvaluationMethod method,
		                                      Point expectedResult,
		                                      double tolerance = double.Epsilon)
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
				Assert.True(! areaToAvoid.Contains(resultRect));
			}

			Assert.True((result - expectedResult).Length < tolerance);
		}
	}
}
