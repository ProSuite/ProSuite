using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Base class for tests that check the length of polylines / polygon perimeters
	/// </summary>
	[CLSCompliant(false)]
	public abstract class QaLengthBase : ContainerTest
	{
		private readonly bool _is3D;
		private readonly double _limit;
		private readonly bool _perPart;
		private readonly int _lengthFieldIndex;
		private readonly bool _useField;
		private readonly string _shapeFieldName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QaLengthBase"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="limit">The length limit.</param>
		/// <param name="is3D">if set to <c>true</c> [is3 D].</param>
		protected QaLengthBase([NotNull] IFeatureClass featureClass,
		                       double limit,
		                       bool is3D)
			: this(featureClass, limit, is3D, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="QaLengthBase"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="limit">The limit.</param>
		protected QaLengthBase([NotNull] IFeatureClass featureClass,
		                       double limit)
			: this(featureClass, limit, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="QaLengthBase"/> class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <param name="limit">The limit.</param>
		/// <param name="is3D">if set to <c>true</c> [is3 D].</param>
		/// <param name="perPart">if set to <c>true</c> [per part].</param>
		protected QaLengthBase([NotNull] IFeatureClass featureClass,
		                       double limit,
		                       bool is3D,
		                       bool perPart)
			: base((ITable) featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_limit = limit;
			_is3D = is3D;
			_perPart = perPart;
			_shapeFieldName = featureClass.ShapeFieldName;

			IField lengthField = DatasetUtils.GetLengthField(featureClass);

			_lengthFieldIndex = lengthField == null
				                    ? -1
				                    : featureClass.FindField(lengthField.Name);

			_useField = _lengthFieldIndex >= 0 &&
			            (! is3D || ! DatasetUtils.HasZ(featureClass));
		}

		#endregion

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;

			if (feature == null)
			{
				return 0;
			}

			IGeometry shape = feature.Shape;

			var curve = shape as ICurve;
			if (curve == null)
			{
				return 0;
			}

			var geometryCollection = (IGeometryCollection) shape;

			if (! _perPart || geometryCollection.GeometryCount == 1)
			{
				if (_useField)
				{
					// get length from field value

					double? lengthValue =
						GdbObjectUtils.ReadRowValue<double>(row, _lengthFieldIndex);

					if (lengthValue != null)
					{
						return CheckLength(lengthValue.Value, curve, row);
					}
				}

				return CheckLength(row, curve);
			}

			// need to check the individual rings / paths

			int errorCount = 0;

			foreach (IGeometry part in GeometryUtils.GetParts(geometryCollection))
			{
				errorCount += CheckLength(row, (ICurve) part);

				Marshal.ReleaseComObject(part);
			}

			return errorCount;
		}

		protected double Limit => _limit;

		protected abstract int CheckLength(double length,
		                                   [NotNull] ICurve curve,
		                                   [NotNull] IRow row);

		protected int ReportError([NotNull] ICurve curve,
		                          [NotNull] string description,
		                          [CanBeNull] IssueCode issueCode,
		                          [NotNull] IRow row)
		{
			return ReportError(description, GetErrorGeometry(curve),
			                   issueCode, _shapeFieldName,
			                   row);
		}

		private int CheckLength([NotNull] IRow row, [NotNull] ICurve curve)
		{
			double length = GeometryUtils.GetLength(curve, _is3D);

			return CheckLength(length, curve, row);
		}

		[NotNull]
		private static IGeometry GetErrorGeometry([NotNull] ICurve shape)
		{
			return shape is IPolyline || shape is IPolygon
				       ? GeometryFactory.Clone(shape)
				       : GeometryFactory.CreatePolyline(shape);
		}
	}
}
