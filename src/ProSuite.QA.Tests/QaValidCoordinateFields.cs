using System;
using System.Globalization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaValidCoordinateFields : ContainerTest
	{
		private readonly double _xyTolerance;
		private readonly double _zTolerance;
		private readonly double _xyToleranceSquared;
		private readonly CultureInfo _cultureInfo;
		private readonly CoordinateField _xCoordinateField;
		private readonly CoordinateField _yCoordinateField;
		private readonly CoordinateField _zCoordinateField;
		private readonly ISpatialReference _spatialReference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string XYFieldCoordinateValueTooFarFromShape =
				"XYFieldCoordinateValueTooFarFromShape";

			public const string ZFieldCoordinateTooFarFromShape =
				"ZFieldCoordinateTooFarFromShape";

			public const string XYFieldCoordinatesTooFarFromShape =
				"XYFieldCoordinatesTooFarFromShape";

			public const string ShapeIsDefinedButCoordinateFieldHasNoValue =
				"ShapeIsDefinedButCoordinateFieldHasNoValue";

			public const string ShapeIsUndefinedButCoordinateFieldHasValue =
				"ShapeIsUndefinedButCoordinateFieldHasValue";

			public const string ErrorReadingFieldValue = "ErrorReadingFieldValue";

			public const string TextFieldValueIsNotNumeric = "TextFieldValueIsNotNumeric";

			public Code() : base("ValidCoordinateFields") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaValidCoordinateFields_0))]
		public QaValidCoordinateFields(
			[Doc(nameof(DocStrings.QaValidCoordinateFields_featureClass))] [NotNull]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_xCoordinateFieldName))] [CanBeNull]
			string
				xCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_yCoordinateFieldName))] [CanBeNull]
			string
				yCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_zCoordinateFieldName))] [CanBeNull]
			string
				zCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_xyTolerance))]
			double xyTolerance,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_zTolerance))]
			double zTolerance,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_culture))] [CanBeNull]
			string culture)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(
				featureClass.ShapeType == esriGeometryType.esriGeometryPoint,
				$"{featureClass.ShapeType}, only point feature classes are supported");

			if (xCoordinateFieldName != null &&
			    StringUtils.IsNotEmpty(xCoordinateFieldName))
			{
				_xCoordinateField =
					GetCoordinateField(featureClass, xCoordinateFieldName);
			}

			if (yCoordinateFieldName != null &&
			    StringUtils.IsNotEmpty(yCoordinateFieldName))
			{
				_yCoordinateField =
					GetCoordinateField(featureClass, yCoordinateFieldName);
			}

			if (zCoordinateFieldName != null &&
			    StringUtils.IsNotEmpty(zCoordinateFieldName))
			{
				_zCoordinateField =
					GetCoordinateField(featureClass, zCoordinateFieldName);
			}

			if (_xCoordinateField != null || _yCoordinateField != null)
			{
				_xyTolerance = xyTolerance;

				if (Math.Abs(_xyTolerance) < double.Epsilon)
				{
					double srefXyTolerance;
					if (DatasetUtils.TryGetXyTolerance(featureClass.SpatialReference,
					                                   out srefXyTolerance))
					{
						_xyTolerance = srefXyTolerance;
					}
				}
			}

			if (_zCoordinateField != null)
			{
				_zTolerance = zTolerance;

				if (Math.Abs(_zTolerance) < double.Epsilon)
				{
					double srefZTolerance;
					if (DatasetUtils.TryGetZTolerance(featureClass.SpatialReference,
					                                  out srefZTolerance))
					{
						_zTolerance = srefZTolerance;
					}
				}

				if (! DatasetUtils.GetGeometryDef(featureClass).HasZ)
				{
					throw new InvalidConfigurationException(
						string.Format(
							"Feature class '{0}' does not have Z values, unable to verify Z coordinate field",
							featureClass.Name));
				}
			}

			_xyToleranceSquared = _xyTolerance * _xyTolerance;

			_cultureInfo = culture == null || StringUtils.IsNullOrEmptyOrBlank(culture)
				               ? CultureInfo.InvariantCulture
				               : CultureInfo.GetCultureInfo(culture);
			_spatialReference = featureClass.SpatialReference;
		}

		[InternallyUsedTest]
		public QaValidCoordinateFields([NotNull] QaValidCoordinateFieldsDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.XCoordinateFieldName,
			       definition.YCoordinateFieldName, definition.ZCoordinateFieldName,
			       definition.XyTolerance, definition.ZTolerance, definition.Culture)
		{
			AllowXYFieldValuesForUndefinedShape = definition.AllowXYFieldValuesForUndefinedShape;
			AllowZFieldValueForUndefinedShape = definition.AllowZFieldValueForUndefinedShape;
			AllowMissingZFieldValueForDefinedShape =
				definition.AllowMissingZFieldValueForDefinedShape;
			AllowMissingXYFieldValueForDefinedShape =
				definition.AllowMissingXYFieldValueForDefinedShape;
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowXYFieldValuesForUndefinedShape))]
		public bool AllowXYFieldValuesForUndefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowZFieldValueForUndefinedShape))]
		public bool AllowZFieldValueForUndefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowMissingZFieldValueForDefinedShape))]
		public bool AllowMissingZFieldValueForDefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowMissingXYFieldValueForDefinedShape))]
		public bool AllowMissingXYFieldValueForDefinedShape { get; set; }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			var feature = row as IReadOnlyFeature;

			return feature == null
				       ? NoError
				       : CheckFeature(feature);
		}

		private int CheckFeature([NotNull] IReadOnlyFeature feature)
		{
			IGeometry shape = feature.Shape;

			IPoint point = shape == null || shape.IsEmpty
				               ? null
				               : (IPoint) shape;

			int errorCount =
				CheckXYValues(feature, point, _xCoordinateField, _yCoordinateField);

			if (_zCoordinateField != null)
			{
				errorCount += CheckZValue(feature, point, _zCoordinateField);
			}

			return errorCount;
		}

		private int CheckXYValues([NotNull] IReadOnlyFeature feature,
		                          [CanBeNull] IPoint point,
		                          [CanBeNull] CoordinateField xCoordinateField,
		                          [CanBeNull] CoordinateField yCoordinateField)
		{
			if (xCoordinateField == null && yCoordinateField == null)
			{
				return NoError;
			}

			int errorCount = 0;
			bool errorReadingXValue = false;
			bool errorReadingYValue = false;
			double? xFieldValue = null;
			if (xCoordinateField != null)
			{
				errorCount += TryReadValue(feature, xCoordinateField, point,
				                           out xFieldValue, out errorReadingXValue);
			}

			double? yFieldValue = null;
			if (yCoordinateField != null)
			{
				errorCount += TryReadValue(feature, yCoordinateField, point,
				                           out yFieldValue, out errorReadingYValue);
			}

			if (point == null)
			{
				if (! AllowXYFieldValuesForUndefinedShape)
				{
					if (xFieldValue != null)
					{
						errorCount += ReportFieldValueForUndefinedShape(feature,
							xCoordinateField,
							xFieldValue
								.Value);
					}

					if (yFieldValue != null)
					{
						errorCount += ReportFieldValueForUndefinedShape(feature,
							yCoordinateField,
							yFieldValue
								.Value);
					}
				}
			}
			else
			{
				// the shape is defined
				if (xCoordinateField != null && xFieldValue == null &&
				    ! errorReadingXValue)
				{
					// ... but the x field is NULL/empty text
					if (! AllowMissingXYFieldValueForDefinedShape)
					{
						errorCount += ReportMissingFieldValueForDefinedShape(feature,
							point,
							xCoordinateField);
					}
				}

				if (yCoordinateField != null && yFieldValue == null &&
				    ! errorReadingYValue)
				{
					// ... but the y field is NULL/empty text
					if (! AllowMissingXYFieldValueForDefinedShape)
					{
						errorCount += ReportMissingFieldValueForDefinedShape(feature,
							point,
							yCoordinateField);
					}
				}

				double shapeX;
				double shapeY;
				point.QueryCoords(out shapeX, out shapeY);

				if (xFieldValue != null && yFieldValue != null)
				{
					double xyDistanceSquared = GetDistanceSquared(
						shapeX, shapeY, xFieldValue.Value, yFieldValue.Value);

					if (xyDistanceSquared > _xyToleranceSquared)
					{
						double xyDistance = Math.Sqrt(xyDistanceSquared);
						errorCount += ReportXYFieldCoordinatesTooFarFromShape(
							feature, point, xyDistance,
							xCoordinateField, xFieldValue.Value, shapeX,
							yCoordinateField, yFieldValue.Value, shapeY);
					}
				}
				else if (xFieldValue != null)
				{
					// y field value is null
					if (yCoordinateField == null)
					{
						// only the x field is specified for the test
						errorCount += CheckSingleXYFieldCoordinate(
							feature, point, xCoordinateField, xFieldValue.Value, shapeX,
							'X');
					}
					else if (! errorReadingYValue)
					{
						// the y field is defined, but it contains NULL/empty text
						if (! AllowMissingXYFieldValueForDefinedShape)
						{
							errorCount += ReportMissingFieldValueForDefinedShape(
								feature, point, yCoordinateField);
						}
						else
						{
							// missing field values are ok, compare what is there
							errorCount += CheckSingleXYFieldCoordinate(
								feature, point, xCoordinateField, xFieldValue.Value,
								shapeX, 'X');
						}
					}
				}
				else if (yFieldValue != null)
				{
					// x field value is null
					if (xCoordinateField == null)
					{
						// only the y field is specified for the test
						errorCount += CheckSingleXYFieldCoordinate(
							feature, point, yCoordinateField, yFieldValue.Value, shapeY,
							'Y');
					}
					else if (! errorReadingXValue)
					{
						// the x field is defined, but it contains NULL/empty text
						if (! AllowMissingXYFieldValueForDefinedShape)
						{
							errorCount += ReportMissingFieldValueForDefinedShape(
								feature, point, xCoordinateField);
						}
						else
						{
							// missing field values are ok, compare what is there
							errorCount += CheckSingleXYFieldCoordinate(
								feature, point, yCoordinateField, yFieldValue.Value,
								shapeY, 'Y');
						}
					}
				}
			}

			return errorCount;
		}

		private int CheckSingleXYFieldCoordinate([NotNull] IReadOnlyFeature feature,
		                                         [NotNull] IPoint point,
		                                         [NotNull] CoordinateField coordinateField,
		                                         double fieldValue,
		                                         double shapeValue,
		                                         char coordinateAxis)
		{
			double distance = Math.Abs(fieldValue - shapeValue);

			if (distance <= _xyTolerance)
			{
				return NoError;
			}

			return ReportSingleXYFieldCoordinateTooFarFromShape(
				feature, point, distance, coordinateField,
				fieldValue, shapeValue, coordinateAxis);
		}

		private int CheckZValue([NotNull] IReadOnlyFeature feature,
		                        [CanBeNull] IPoint point,
		                        [NotNull] CoordinateField zCoordinateField)
		{
			int errorCount = 0;
			double? zFieldValue;
			bool errorReadingValue;
			errorCount += TryReadValue(feature, zCoordinateField, point, out zFieldValue,
			                           out errorReadingValue);

			if (point == null)
			{
				if (! AllowZFieldValueForUndefinedShape && zFieldValue != null)
				{
					errorCount += ReportFieldValueForUndefinedShape(feature,
						zCoordinateField,
						zFieldValue.Value);
				}
			}
			else
			{
				if (zFieldValue != null)
				{
					double shapeZ = point.Z;

					double zDistance = Math.Abs(zFieldValue.Value - shapeZ);

					if (zDistance > _zTolerance)
					{
						errorCount += ReportZFieldCoordinateTooFarFromShape(
							feature, point,
							zCoordinateField,
							zFieldValue.Value,
							shapeZ, zDistance);
					}
				}
				else if (! errorReadingValue)
				{
					// the field value is null, but the shape has a Z value

					if (! AllowMissingZFieldValueForDefinedShape)
					{
						errorCount += ReportMissingFieldValueForDefinedShape(feature,
							point,
							zCoordinateField);
					}
				}
			}

			return errorCount;
		}

		private int ReportSingleXYFieldCoordinateTooFarFromShape(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IPoint point,
			double distance,
			[NotNull] CoordinateField coordinateField,
			double fieldValue,
			double shapeValue,
			char coordinateAxis)
		{
			string description =
				string.Format(
					"The distance between the value {0} in field '{1}' and " +
					"the shape {2} value {3} is larger than the tolerance ({4})",
					fieldValue, coordinateField.FieldName,
					coordinateAxis, shapeValue,
					FormatLengthComparison(distance, ">", _xyTolerance,
					                       _spatialReference).Trim());

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), GetErrorGeometry(point),
				Codes[Code.XYFieldCoordinateValueTooFarFromShape], coordinateField.FieldName);
		}

		private string GetNonEqualValueMessage([NotNull] CoordinateField coordinateField,
		                                       double fieldValue, double shapeValue)
		{
			return string.Format("Field {0}: {1}",
			                     coordinateField.FieldName,
			                     FormatLengthComparison(fieldValue, "<>", shapeValue,
			                                            _spatialReference).Trim());
		}

		private int ReportXYFieldCoordinatesTooFarFromShape(
			[NotNull] IReadOnlyFeature feature, [NotNull] IPoint point, double xyDistance,
			[NotNull] CoordinateField xCoordinateField, double xFieldValue, double shapeX,
			[NotNull] CoordinateField yCoordinateField, double yFieldValue, double shapeY)
		{
			bool xDifferent = Math.Abs(xFieldValue - shapeX) > _xyTolerance;
			bool yDifferent = Math.Abs(yFieldValue - shapeY) > _xyTolerance;

			if (xDifferent && ! yDifferent)
			{
				return ReportSingleXYFieldCoordinateTooFarFromShape(
					feature, point, xyDistance,
					xCoordinateField, xFieldValue,
					shapeX, 'X');
			}

			if (! xDifferent && yDifferent)
			{
				return ReportSingleXYFieldCoordinateTooFarFromShape(
					feature, point, xyDistance,
					yCoordinateField, yFieldValue,
					shapeY, 'Y');
			}

			// either both differences are individually larger than the tolerance, 
			// OR the 2D distance is larger than the tolerance but the individual distances are not
			// (in this case BOTH distances must be just below tolerance)
			string comparisonMessage = string.Format(" {0}; {1}",
			                                         GetNonEqualValueMessage(
				                                         xCoordinateField,
				                                         xFieldValue,
				                                         shapeX),
			                                         GetNonEqualValueMessage(
				                                         yCoordinateField,
				                                         yFieldValue,
				                                         shapeY));
			string affectedComponent = string.Format("{0},{1}",
			                                         xCoordinateField.FieldName,
			                                         yCoordinateField.FieldName);

			string description =
				string.Format(
					"The distance between the XY field coordinates and the shape is larger than the tolerance ({0}).{1}",
					FormatLengthComparison(xyDistance, ">", _xyTolerance,
					                       _spatialReference).Trim(),
					comparisonMessage);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), GetErrorGeometry(point),
				Codes[Code.XYFieldCoordinatesTooFarFromShape], affectedComponent);
		}

		private int ReportZFieldCoordinateTooFarFromShape(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IPoint point,
			[NotNull] CoordinateField zCoordinateField,
			double zFieldValue,
			double shapeZ,
			double zDistance)
		{
			string description =
				string.Format(
					"The distance between value {0:N3} in field '{1}' and the shape Z value {2:N3} is larger than the tolerance ({3})",
					zFieldValue, zCoordinateField.FieldName, shapeZ,
					FormatLengthComparison(zDistance, ">", _zTolerance,
					                       _spatialReference).Trim());

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), GetErrorGeometry(point),
				Codes[Code.ZFieldCoordinateTooFarFromShape], zCoordinateField.FieldName);
		}

		private int ReportFieldValueForUndefinedShape(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] CoordinateField coordinateField,
			double value)
		{
			string description =
				string.Format(
					"The shape is not defined, but the field '{0}' has a value ({1})",
					coordinateField.FieldName, value);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), null,
				Codes[Code.ShapeIsUndefinedButCoordinateFieldHasValue], coordinateField.FieldName);
		}

		private int ReportMissingFieldValueForDefinedShape(
			[NotNull] IReadOnlyFeature feature,
			[NotNull] IPoint point,
			[NotNull] CoordinateField coordinateField)
		{
			string description =
				string.Format(
					"The shape is defined, but the field '{0}' does not contain a value",
					coordinateField.FieldName);

			return ReportError(
				description, InvolvedRowUtils.GetInvolvedRows(feature), GetErrorGeometry(point),
				Codes[Code.ShapeIsDefinedButCoordinateFieldHasNoValue], coordinateField.FieldName);
		}

		private static double GetDistanceSquared(double x0, double y0,
		                                         double x1, double y1)
		{
			double dx = Math.Abs(x0 - x1);
			double dy = Math.Abs(y0 - y1);

			return dx * dx + dy * dy;
		}

		[CanBeNull]
		private static IPoint GetErrorGeometry([CanBeNull] IPoint point)
		{
			return point == null || point.IsEmpty
				       ? null
				       : GeometryFactory.Clone(point);
		}

		private int TryReadValue([NotNull] IReadOnlyFeature feature,
		                         [NotNull] CoordinateField coordinateField,
		                         [CanBeNull] IPoint point,
		                         out double? value,
		                         out bool errorReadingValue)
		{
			string message;
			IssueCode issueCode;
			if (! TryReadValue(feature, coordinateField,
			                   out value, out message, out issueCode))
			{
				errorReadingValue = true;
				return ReportError(
					message, InvolvedRowUtils.GetInvolvedRows(feature), GetErrorGeometry(point),
					issueCode, coordinateField.FieldName);
			}

			errorReadingValue = false;
			return NoError;
		}

		private bool TryReadValue([NotNull] IReadOnlyRow row,
		                          [NotNull] CoordinateField coordinateField,
		                          out double? value,
		                          [NotNull] out string message,
		                          [CanBeNull] out IssueCode issueCode)
		{
			object rawValue = row.get_Value(coordinateField.FieldIndex);

			if (rawValue == null || rawValue is DBNull)
			{
				value = null;
				message = string.Empty;
				issueCode = null;
				return true;
			}

			if (coordinateField.IsText)
			{
				var text = (string) rawValue;

				double doubleValue;
				if (! double.TryParse(text, NumberStyles.Number, _cultureInfo,
				                      out doubleValue))
				{
					message = string.Format(
						"Unable to read coordinate value from text field '{0}'; text value: '{1}'",
						coordinateField.FieldName, text);
					issueCode = Codes[Code.TextFieldValueIsNotNumeric];
					value = null;
					return false;
				}

				message = string.Empty;
				issueCode = null;
				value = doubleValue;
				return true;
			}

			try
			{
				message = string.Empty;
				issueCode = null;
				value = Convert.ToDouble(rawValue);
				return true;
			}
			catch (Exception e)
			{
				message = string.Format(
					"Error reading coordinate value from field '{0}' (value: {1}): {2}",
					coordinateField.FieldName, rawValue, e.Message);
				issueCode = Codes[Code.ErrorReadingFieldValue];
				value = null;
				return false;
			}
		}

		[NotNull]
		private static CoordinateField GetCoordinateField(
			[NotNull] IReadOnlyFeatureClass featureClass,
			[NotNull] string fieldName)
		{
			int fieldIndex = featureClass.FindField(fieldName);

			if (fieldIndex < 0)
			{
				throw new InvalidConfigurationException(
					string.Format("Field '{0}' does not exist in feature class '{1}'",
					              fieldName,
					              featureClass));
			}

			IField field = featureClass.Fields.Field[fieldIndex];

			bool isSupported = IsFieldTypeSupported(field.Type);

			if (! isSupported)
			{
				throw new InvalidConfigurationException(
					string.Format(
						"Type {0} of field '{1}' is not supported for coordinate fields",
						FieldUtils.GetFieldTypeDisplayText(field.Type),
						field.Name));
			}

			return new CoordinateField(field, fieldIndex);
		}

		private static bool IsFieldTypeSupported(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
				case esriFieldType.esriFieldTypeInteger:
				case esriFieldType.esriFieldTypeSingle:
				case esriFieldType.esriFieldTypeDouble:
				case esriFieldType.esriFieldTypeString:
					return true;

				case esriFieldType.esriFieldTypeDate:
				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
				case esriFieldType.esriFieldTypeXML:
					return false;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(fieldType), fieldType, @"Unsupported field type");
			}
		}

		private class CoordinateField
		{
			public CoordinateField([NotNull] IField field, int fieldIndex)
			{
				FieldIndex = fieldIndex;
				FieldName = field.Name;
				IsText = field.Type == esriFieldType.esriFieldTypeString;
			}

			public bool IsText { get; }

			public int FieldIndex { get; }

			[NotNull]
			public string FieldName { get; }
		}
	}
}
