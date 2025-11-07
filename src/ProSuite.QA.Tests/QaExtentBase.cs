using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Base class providing common extent evaluation logic for maximum/minimum extent tests.
	/// </summary>
	public abstract class QaExtentBase : ContainerTest
	{
		protected readonly double Limit;

		private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		// Internal per-part flag that derived classes can expose as needed
		protected bool UsePerPart { get; set; }

		protected QaExtentBase([NotNull] IReadOnlyFeatureClass featureClass,
		                       double limit)
			: base(featureClass)
		{
			Limit = limit;

			// NOTE:
			// It would probably be useful to set the ProcessBase.LinearUnits from the spatial.
			// However, this would be breaking backwards compatibility with the allowed errors.
			// Also, the double blank in the length description has been there for a long time:
			// Length 45.28  < 50.00
			// Possible solutions:
			// - Add NumericValue fields and affected component to central issue tables (and use for comparisons)
			// - Add a description comparison to each Test class (or TestDefinition) that can apply
			//   legacy / fallback comparison logic
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = (IReadOnlyFeature) row;
			IGeometry shape = feature.Shape;

			if (! UsePerPart)
			{
				return ExecutePartForRow(row, shape);
			}

			int errorCount = 0;

			foreach (IGeometry part in GetParts(shape))
			{
				int partErrors = ExecutePartForRow(row, part);
				errorCount += partErrors;

				if (part != shape && partErrors == 0)
				{
					// The part is some sub-component of the feature, either 
					// a ring, path or a connected-component polygon
					// -> release it if no error was reported for this part (otherwise it is used as error geometry).
					Marshal.ReleaseComObject(part);
				}
			}

			return errorCount;
		}

		/// <summary>
		/// Implement the extent check and reporting for a single geometry/part.
		/// </summary>
		protected abstract int ExecutePartForRow([NotNull] IReadOnlyRow row,
		                                         [NotNull] IGeometry geometry);

		[NotNull]
		protected IEnvelope GetEnvelope([NotNull] IGeometry geometry)
		{
			try
			{
				geometry.QueryEnvelope(_envelopeTemplate);
			}
			catch (COMException)
			{
				// empty geometry?
				_envelopeTemplate.SetEmpty();
			}

			return _envelopeTemplate;
		}

		[NotNull]
		protected static IEnumerable<IGeometry> GetParts([NotNull] IGeometry shape)
		{
			var polygon = shape as IPolygon;
			if (polygon != null)
			{
				foreach (IGeometry part in
				         TestUtils.GetParts(polygon, PolygonPartType.ExteriorRing))
				{
					yield return part;
				}
			}
			else
			{
				foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) shape))
				{
					yield return part;
				}
			}
		}
	}
}
