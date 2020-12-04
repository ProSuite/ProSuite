using System;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	[CLSCompliant(false)]
	public static class TestRunnerUtils
	{
		public static void RunTests([NotNull] ITest test,
		                            int expectedErrorCount,
		                            double tileSize)
		{
			var containerRunner = new QaContainerTestRunner(tileSize, test);
			containerRunner.Execute();

			Assert.AreEqual(expectedErrorCount, containerRunner.Errors.Count,
			                "ErrorCount");
		}

		public static void RunTests([NotNull] ITest test,
		                            [NotNull] IEnvelope extent,
		                            int expectedErrorCount,
		                            double tileSize)
		{
			var containerRunner = new QaContainerTestRunner(tileSize, test);
			containerRunner.Execute(extent);

			Assert.AreEqual(expectedErrorCount, containerRunner.Errors.Count,
			                "ErrorCount");
		}

		public static void PrintError([NotNull] QaError error)
		{
			Console.WriteLine(error.ToString());
			if (error.Values != null && error.Values.Count > 0)
			{
				Console.WriteLine(@"- Values: {0}",
				                  StringUtils.Concatenate(error.Values, ","));
			}

			Console.WriteLine(@"- {0}", GetGeometryDescription(error));
			const string newLine = "\n";
			// r# unit test output adds 2 lines for Environment.NewLine
			Console.Write(newLine);
		}

		[NotNull]
		private static string GetGeometryDescription([NotNull] QaError error)
		{
			IGeometry geometry = error.Geometry;

			if (geometry == null)
			{
				return "no geometry";
			}

			var zAware = GeometryUtils.IsZAware(geometry);

			try
			{
				var sb = new StringBuilder();

				string envelope = GeometryUtils.Format(geometry.Envelope,
				                                       allSignificantDigits: true);

				var point = geometry as IPoint;
				if (point != null)
				{
					AppendPoint(sb, point, zAware);
				}
				else
				{
					var multiPoint = geometry as IMultipoint;
					if (multiPoint != null)
					{
						int count = GeometryUtils.GetPointCount(multiPoint);
						if (count != 1)
						{
							sb.AppendFormat("Multipoint: {0} point(s); envelope:{1}",
							                count, envelope);
						}
						else
						{
							point = ((IPointCollection) multiPoint).Point[0];
							AppendPoint(sb, point, zAware);
						}
					}
					else
					{
						var polyline = geometry as IPolyline;
						if (polyline != null)
						{
							sb.AppendFormat(
								"Line: length={0}; parts={1}; vertices={2}; envelope:{3}",
								polyline.Length,
								GeometryUtils.GetPartCount(polyline),
								GeometryUtils.GetPointCount(polyline),
								envelope);
						}
						else
						{
							var polygon = geometry as IPolygon;
							if (polygon != null)
							{
								sb.AppendFormat(
									"Polygon: area={0}; length={1}; parts={2}; vertices={3}; envelope:{4}",
									((IArea) polygon).Area,
									polygon.Length,
									GeometryUtils.GetPartCount(polygon),
									GeometryUtils.GetPointCount(polygon),
									envelope);
							}
							else
							{
								var multipatch = geometry as IMultiPatch;
								if (multipatch != null)
								{
									sb.AppendFormat(
										"MultiPatch: parts={0}; vertices={1}; envelope:{2}",
										GeometryUtils.GetPartCount(multipatch),
										GeometryUtils.GetPointCount(multipatch),
										envelope);
								}
							}
						}
					}
				}

				return sb.ToString();
			}
			catch (Exception ex)
			{
				return $"Error getting geometry description: {ex.Message}";
			}
		}

		private static void AppendPoint([NotNull] StringBuilder sb,
		                                [NotNull] IPoint point,
		                                bool zAware)
		{
			if (zAware)
			{
				sb.AppendFormat("Point: X={0} Y={1} Z={2}",
				                point.X, point.Y, point.Z);
			}
			else
			{
				sb.AppendFormat("Point: X={0} Y={1}", point.X, point.Y);
			}
		}
	}
}
