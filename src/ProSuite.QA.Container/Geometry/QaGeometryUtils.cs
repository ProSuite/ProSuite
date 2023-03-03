using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.QA.Container.Geometry
{
	[Obsolete("move to ProSuite.Commons.Ao rename")]
	public static class QaGeometryUtils_
	{
		// always access by property
		[CanBeNull] [ThreadStatic] private static IEnvelope _envelopeTemplate;

		[NotNull]
		private static IEnvelope EnvelopeTemplate
			=> _envelopeTemplate ?? (_envelopeTemplate = new EnvelopeClass());

		[NotNull]
		public static Box CreateBox([NotNull] IGeometry geometry,
		                            double expansionDistance = 0)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var envelope = geometry as IEnvelope;
			if (envelope == null)
			{
				geometry.QueryEnvelope(EnvelopeTemplate);
				envelope = EnvelopeTemplate;
			}

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			return new Box(
				new Pnt2D(xMin - expansionDistance, yMin - expansionDistance),
				new Pnt2D(xMax + expansionDistance, yMax + expansionDistance));
		}

		[NotNull]
		internal static Pnt2D CreatePoint2D([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			return new Pnt2D(x, y);
		}

		[NotNull]
		public static Pnt CreatePoint3D([NotNull] IPoint point)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			return new Pnt3D(x, y, point.Z);
		}

		public static WKSPointZ GetWksPoint(IPnt p)
		{
			var wks = new WKSPointZ
			          {
				          X = p.X,
				          Y = p.Y
			          };

			var point3D = p as Pnt3D;
			if (point3D != null)
			{
				wks.Z = point3D.Z;
			}

			return wks;
		}

		internal static IEnumerable<PatchProxy> GetPatchProxies(
			[NotNull] IMultiPatch multiPatch)
		{
			int minPartIndex = 0;
			var parts = multiPatch as IGeometryCollection;
			if (parts == null)
			{
				// no geometry collection
				yield return new PatchProxy(0, 0, (IPointCollection4) multiPatch);
			}
			else
			{
				int partCount = parts.GeometryCount;

				for (int partIndex = 0; partIndex < partCount; partIndex++)
				{
					var patch = (IPointCollection4) parts.Geometry[partIndex];
					try
					{
						var patchProxy = new PatchProxy(partIndex, minPartIndex, patch);
						minPartIndex += patchProxy.PlanesCount;

						yield return patchProxy;
					}
					finally
					{
						Marshal.ReleaseComObject(patch);
					}
				}
			}
		}
	}
}
