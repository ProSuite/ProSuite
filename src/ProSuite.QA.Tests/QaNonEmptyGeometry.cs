using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaNonEmptyGeometry : NonContainerTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

			int previousOid = -1;
			bool tryFallbackImplementation = false;

			// New with 12.5 (probably 10.8.1 x64 too?) for multipatch features:
			// COMException (errorCode -2147220959) with various messages, such as
			// - Insufficient permissions [ORA-00942: table or view does not exist]
			// - The operation was attempted on an empty geometry (when in SDE schema/user)
			// when an empty geometry is encountered!

			try
			{
				foreach (IFeature feature in
					GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
				{
					errorCount += TestFeature(feature);
					previousOid = feature.OID;
				}
			}
			catch (COMException e)
			{
				_msg.Debug($"Error getting feature from {DatasetUtils.GetName(featureClass)}. " +
				           $"Previous successful object id: {previousOid}", e);

				if (e.ErrorCode == -2147220959)
				{
					_msg.Debug(
						"Error getting feature with presumably empty geometry. Using fall-back implementation (slow) to identify object id.");
					tryFallbackImplementation = true;
				}
				else
				{
					throw;
				}
			}

			if (! tryFallbackImplementation)
			{
				return errorCount;
			}

			// Read all features without geometry, get geometry separately for each feature:
			filter.SubFields = featureClass.OIDFieldName;

			foreach (IFeature feature in
				GdbQueryUtils.GetFeatures(featureClass, filter, recycling))
			{
				try
				{
					IFeature featureWithGeometry = featureClass.GetFeature(feature.OID);
					Marshal.ReleaseComObject(featureWithGeometry);
				}
				catch (Exception e)
				{
					errorCount += ReportError($"Feature geometry cannot be loaded ({e.Message})",
					                          null,
					                          Codes[Code.GeometryEmpty], _shapeFieldName,
					                          feature);
				}
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