using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[GeometryTest]
	public class QaNonEmptyGeometry : NonContainerTest
	{
		private readonly IFeatureClass _featureClass;
		private readonly bool _dontFilterPolycurvesByZeroLength;
		private readonly string _shapeFieldName;
		private readonly ISpatialReference _spatialReference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string GeometryNull = "GeometryNull";
			public const string GeometryEmpty = "GeometryEmpty";

			public Code() : base("EmptyGeometry") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometry(
				[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
				IFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, false) { }

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometry(
			[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
			IFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNonEmptyGeometry_dontFilterPolycurvesByZeroLength))]
			bool
				dontFilterPolycurvesByZeroLength)
			: base(new[] {(ITable) featureClass})
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_featureClass = featureClass;

			// NOTE: querying on shape_length was observed to hang for some feature classes (BUG-000095040)
			//       This could be reproduced with "Select By Attributes" also.
			//       Allow disabling the use of this filter globally, by means of environment variable.
			_dontFilterPolycurvesByZeroLength =
				dontFilterPolycurvesByZeroLength ||
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					"PROSUITE_QA_NONEMPTYGEOMETRY_DONTFILTERBYSHAPELENGTH");

			_shapeFieldName = featureClass.ShapeFieldName;
			_spatialReference = ((IGeoDataset) featureClass).SpatialReference;
		}

		#region ITest Members

		public override int Execute()
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IPolygon area)
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IEnumerable<IRow> selectedRows)
		{
			var errorCount = 0;

			foreach (IRow row in selectedRows)
			{
				if (CancelTestingRow(row))
				{
					continue;
				}

				errorCount += Execute(row);
			}

			return errorCount;
		}

		public override int Execute(IRow row)
		{
			var feature = row as IFeature;

			// if row is not a feature: no error
			return feature == null
				       ? NoError
				       : TestFeature(feature);
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return _spatialReference;
		}

		#endregion

		private int TestFeatures([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			var errorCount = 0;

			const bool recycling = true;
			IQueryFilter filter = CreateFilter(featureClass, GetConstraint(0));

			foreach (IFeature feature in
				GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
			{
				errorCount += TestFeature(feature);
			}

			return errorCount;
		}

		private int TestFeature([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeometry geometry = feature.Shape;

			if (geometry == null)
			{
				return ReportError("Feature has no geometry", null,
				                   Codes[Code.GeometryNull], _shapeFieldName,
				                   feature);
			}

			return geometry.IsEmpty
				       ? ReportError("Feature has empty geometry", null,
				                     Codes[Code.GeometryEmpty], _shapeFieldName,
				                     feature)
				       : NoError;
		}

		[NotNull]
		private IQueryFilter CreateFilter([NotNull] IFeatureClass featureClass,
		                                  [CanBeNull] string filterExpression)
		{
			IQueryFilter filter =
				new QueryFilterClass
				{
					SubFields = _shapeFieldName,
					WhereClause =
						GetWhereClause(featureClass, filterExpression,
						               _dontFilterPolycurvesByZeroLength)
				};

			var subfields = new List<string> {_shapeFieldName};
			if (featureClass.HasOID)
			{
				subfields.Add(featureClass.OIDFieldName);
			}

			GdbQueryUtils.SetSubFields(filter, subfields);

			return filter;
		}

		[NotNull]
		private static string GetWhereClause([NotNull] IFeatureClass featureClass,
		                                     [CanBeNull] string filterExpression,
		                                     bool dontFilterPolycurvesByZeroLength)
		{
			string emptyGeometryWhereClause = dontFilterPolycurvesByZeroLength
				                                  ? null
				                                  : GetEmptyGeometryWhereClause(featureClass);

			if (StringUtils.IsNullOrEmptyOrBlank(filterExpression))
			{
				return emptyGeometryWhereClause ?? string.Empty;
			}

			return emptyGeometryWhereClause == null
				       ? filterExpression
				       : string.Format("({0}) AND ({1})", emptyGeometryWhereClause,
				                       filterExpression);
		}

		[CanBeNull]
		private static string GetEmptyGeometryWhereClause(
			[NotNull] IFeatureClass featureClass)
		{
			esriGeometryType shapeType = featureClass.ShapeType;

			if (shapeType == esriGeometryType.esriGeometryPolygon ||
			    shapeType == esriGeometryType.esriGeometryPolyline)
			{
				IField lengthField = DatasetUtils.GetLengthField(featureClass);

				if (lengthField != null)
				{
					return string.Format("{0} IS NULL OR {0} <= 0", lengthField.Name);
				}
			}

			return null;
		}
	}
}
