using System;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class MathUtilsTest
	{
		[Test]
		public void CanCalculateSecondDerivative()
		{
			Assert.AreEqual(0, GeometryMathUtils.CalculateAngleSecondDerivative(10, 10, 10,
				                100, 100, 100,
				                100));

			Console.WriteLine(GeometryMathUtils.CalculateAngleSecondDerivative(10, 10, 10,
				                  200, 100, 100,
				                  200)); // 0 ??

			Console.WriteLine(GeometryMathUtils.CalculateAngleSecondDerivative(10, 1, 10,
				                  200, 100, 100,
				                  200)); // 0 ??

			Console.WriteLine(GeometryMathUtils.CalculateAngleSecondDerivative(10, 10, 10,
				                  0, 100, 100, 200));
			// 0.0294 ??
		}

		[Test]
		public void CanCalculateMaximumCurvature()
		{
			Assert.AreEqual(0, GeometryMathUtils.CalculateMaximumCurvature(10, 10, 10,
				                100, 100, 100, 100));

			Console.WriteLine(GeometryMathUtils.CalculateMaximumCurvature(10, 10, 10,
				                  200, 100, 100, 200));

			Console.WriteLine(GeometryMathUtils.CalculateMaximumCurvature(10, 1, 10,
				                  200, 100, 100, 200));

			Console.WriteLine(GeometryMathUtils.CalculateMaximumCurvature(10, 10, 10,
				                  0, 100, 100, 200));
			// 0.0294 ??
		}
	}
}
