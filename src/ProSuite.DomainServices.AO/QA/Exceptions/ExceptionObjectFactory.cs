using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectFactory : ExceptionObjectFactoryBase
	{
		private readonly bool _tableSupportsNullValues;

		[CanBeNull] private readonly IAlternateKeyConverterProvider
			_alternateKeyConverterProvider;

		private readonly ShapeMatchCriterion _defaultShapeMatchCriterion;
		private readonly ExceptionObjectStatus _defaultStatus;
		[CanBeNull] private readonly IGeometry _areaOfInterest;

		[CanBeNull] private readonly IFeatureClass _featureClass;
		private readonly esriGeometryType? _shapeType;
		private readonly double? _xyTolerance;
		[NotNull] private readonly IEnvelope _envelopeTemplate = new EnvelopeClass();

		private readonly int _uuidIndex;
		private readonly int _versionUuidIndex;
		private readonly int _issueCodeIndex;
		private readonly int _affectedComponentIndex;
		private readonly int _involvedObjectsIndex;
		private readonly int _shapeMatchCriterionIndex;
		private readonly int _exceptionStatusIndex;
		private readonly int _doubleValue1Index;
		private readonly int _doubleValue2Index;
		private readonly int _textValueIndex;
		private readonly int _exceptionCategoryIndex;

		private readonly int _managedOriginIndex = -1;
		private readonly int _managedLineageUuidIndex = -1;
		private readonly int _managedVersionBeginDateIndex = -1;
		private readonly int _managedVersionEndDateIndex = -1;
		private readonly int _managedVersionUuidIndex = -1;
		private readonly int _managedVersionImportOriginIndex = -1;

		[NotNull] private readonly List<string> _fieldNames;

		[CLSCompliant(false)]
		public ExceptionObjectFactory(
			[NotNull] ITable table,
			[NotNull] IIssueTableFields fields,
			[CanBeNull] IAlternateKeyConverterProvider alternateKeyConverterProvider = null,
			ShapeMatchCriterion defaultShapeMatchCriterion = ShapeMatchCriterion.EqualEnvelope,
			ExceptionObjectStatus defaultStatus = ExceptionObjectStatus.Active,
			[CanBeNull] IGeometry areaOfInterest = null,
			bool includeManagedExceptionAttributes = false) : base(table, fields)
		{
			_alternateKeyConverterProvider = alternateKeyConverterProvider;
			_defaultShapeMatchCriterion = defaultShapeMatchCriterion;
			_defaultStatus = defaultStatus;
			_areaOfInterest = areaOfInterest;

			_tableSupportsNullValues =
				! WorkspaceUtils.IsShapefileWorkspace(DatasetUtils.GetWorkspace(table));

			_featureClass = table as IFeatureClass;
			_shapeType = _featureClass?.ShapeType;

			_xyTolerance = _featureClass == null
				               ? (double?) null
				               : GetXyTolerance(_featureClass);

			_uuidIndex = GetIndex(IssueAttribute.QualityConditionUuid);
			_versionUuidIndex = GetIndex(IssueAttribute.QualityConditionVersionUuid);
			_issueCodeIndex = GetIndex(IssueAttribute.IssueCode);
			_affectedComponentIndex = GetIndex(IssueAttribute.AffectedComponent);
			_involvedObjectsIndex = GetIndex(IssueAttribute.InvolvedObjects);
			_shapeMatchCriterionIndex = GetIndex(IssueAttribute.ExceptionShapeMatchCriterion,
			                                     optional: true);
			_exceptionStatusIndex = GetIndex(IssueAttribute.ExceptionStatus, optional: true);

			_doubleValue1Index = GetIndex(IssueAttribute.DoubleValue1, optional: true);
			_doubleValue2Index = GetIndex(IssueAttribute.DoubleValue2, optional: true);
			_textValueIndex = GetIndex(IssueAttribute.TextValue, optional: true);

			_exceptionCategoryIndex = GetIndex(IssueAttribute.ExceptionCategory,
			                                   optional: true);

			if (includeManagedExceptionAttributes)
			{
				// get managed exception attribute field indexes
				_managedOriginIndex = GetIndex(IssueAttribute.ManagedExceptionOrigin,
				                               optional: true);
				_managedLineageUuidIndex = GetIndex(IssueAttribute.ManagedExceptionLineageUuid,
				                                    optional: true);
				_managedVersionBeginDateIndex =
					GetIndex(IssueAttribute.ManagedExceptionVersionBeginDate,
					         optional: true);
				_managedVersionEndDateIndex =
					GetIndex(IssueAttribute.ManagedExceptionVersionEndDate,
					         optional: true);
				_managedVersionUuidIndex = GetIndex(IssueAttribute.ManagedExceptionVersionUuid,
				                                    optional: true);
				_managedVersionImportOriginIndex = GetIndex(
					IssueAttribute.ManagedExceptionVersionOrigin,
					optional: true);
			}

			_fieldNames = GetFieldNames(fields, table,
			                            GetIssueAttributes(includeManagedExceptionAttributes))
				.ToList();
		}

		private static double GetXyTolerance([NotNull] IFeatureClass featureClass)
		{
			ISpatialReference spatialReference =
				DatasetUtils.GetSpatialReference(featureClass);
			Assert.NotNull(spatialReference, "spatialReference");

			return ((ISpatialReferenceTolerance) spatialReference).XYTolerance;
		}

		[NotNull]
		public IEnumerable<string> FieldNames => _fieldNames;

		[NotNull]
		[CLSCompliant(false)]
		public ExceptionObject CreateExceptionObject([NotNull] IRow row)
		{
			Guid qualityConditionUuid = Assert.NotNull(GetGuid(row, _uuidIndex),
			                                           "UUID field is null")
			                                  .Value;

			Guid qualityConditionVersionUuid = Assert.NotNull(GetGuid(row, _versionUuidIndex),
			                                                  "Version UUID field is null")
			                                         .Value;

			string involvedObjects = Assert.NotNull(GetString(row, _involvedObjectsIndex),
			                                        "Involved tables field is null");

			IGeometry shape = GetShape(row);

			// TODO reduce box to within AOI box
			// TODO but deal with case that exception may be touching (from outside) or disjoint
			IBox box = GetBox(shape);
			IBox aoiBox = GetAreaOfInterestBox(shape);
			bool intersectsAreaOfInterest = IntersectsAreaOfInterest(shape);

			return new ExceptionObject(row.OID,
			                           qualityConditionUuid,
			                           qualityConditionVersionUuid,
			                           box, _xyTolerance, _shapeType,
			                           GetShapeMatchCriterion(row),
			                           GetString(row, _issueCodeIndex),
			                           GetString(row, _affectedComponentIndex),
			                           ParseInvolvedTables(involvedObjects,
			                                               qualityConditionUuid),
			                           involvedObjects,
			                           GetStatus(row),
			                           GetDoubleValue(row, _doubleValue1Index),
			                           GetDoubleValue(row, _doubleValue2Index),
			                           GetString(row, _textValueIndex),
			                           GetString(row, _exceptionCategoryIndex),
			                           intersectsAreaOfInterest,
			                           aoiBox,
			                           managedOrigin: GetString(row, _managedOriginIndex),
			                           managedLineageUuid: GetGuid(
				                           row, _managedLineageUuidIndex),
			                           managedVersionBeginDate: GetDateTime(
				                           row, _managedVersionBeginDateIndex),
			                           managedVersionEndDate: GetDateTime(
				                           row, _managedVersionEndDateIndex),
			                           managedVersionUuid: GetGuid(
				                           row, _managedVersionUuidIndex),
			                           managedVersionOrigin: GetString(
				                           row, _managedVersionImportOriginIndex));
		}

		[NotNull]
		private static IEnumerable<IssueAttribute> GetIssueAttributes(
			bool includeManagedExceptionAttributes)
		{
			var result = new List<IssueAttribute>
			             {
				             IssueAttribute.QualityConditionUuid,
				             IssueAttribute.QualityConditionVersionUuid,
				             IssueAttribute.IssueCode,
				             IssueAttribute.AffectedComponent,
				             IssueAttribute.InvolvedObjects,
				             IssueAttribute.ExceptionStatus,
				             IssueAttribute.DoubleValue1,
				             IssueAttribute.DoubleValue2,
				             IssueAttribute.TextValue,
				             IssueAttribute.ExceptionCategory
			             };

			if (includeManagedExceptionAttributes)
			{
				result.AddRange(new[]
				                {
					                IssueAttribute.ManagedExceptionLineageUuid,
					                IssueAttribute.ManagedExceptionOrigin,
					                IssueAttribute.ManagedExceptionVersionBeginDate,
					                IssueAttribute.ManagedExceptionVersionEndDate,
					                IssueAttribute.ManagedExceptionVersionUuid,
					                IssueAttribute.ManagedExceptionVersionOrigin
				                });
			}

			return result;
		}

		private bool IntersectsAreaOfInterest([CanBeNull] IGeometry shape)
		{
			if (shape == null)
			{
				// no shape --> always relevant
				return true;
			}

			if (_areaOfInterest == null)
			{
				// no aoi geometry --> unrestricted AOI
				return true;
			}

			GeometryUtils.AllowIndexing(shape);

			// false only if both the shape and the aoi are non-empty and disjoint
			return ! ((IRelationalOperator) _areaOfInterest).Disjoint(shape);
		}

		[NotNull]
		private IList<InvolvedTable> ParseInvolvedTables(
			[NotNull] string involvedTablesString, Guid qualityConditionGuid)
		{
			IAlternateKeyConverter keyConverter =
				_alternateKeyConverterProvider?.GetConverter(qualityConditionGuid);

			return IssueUtils.ParseInvolvedTables(involvedTablesString, keyConverter);
		}

		private ExceptionObjectStatus GetStatus([NotNull] IRow row)
		{
			if (_exceptionStatusIndex < 0)
			{
				return _defaultStatus;
			}

			string value = GetString(row, _exceptionStatusIndex);

			return ExceptionObjectUtils.ParseStatus(value, _defaultStatus);
		}

		private ShapeMatchCriterion GetShapeMatchCriterion([NotNull] IRow row)
		{
			if (_shapeMatchCriterionIndex < 0)
			{
				return _defaultShapeMatchCriterion;
			}

			string value = GetString(row, _shapeMatchCriterionIndex);

			if (StringUtils.IsNullOrEmptyOrBlank(value))
			{
				return _defaultShapeMatchCriterion;
			}

			if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase))
			{
				return _defaultShapeMatchCriterion;
			}

			if (string.Equals(value, "equalenvelope", StringComparison.OrdinalIgnoreCase))
			{
				return ShapeMatchCriterion.EqualEnvelope;
			}

			if (string.Equals(value, "withinenvelope", StringComparison.OrdinalIgnoreCase))
			{
				return ShapeMatchCriterion.WithinEnvelope;
			}

			if (string.Equals(value, "ignore", StringComparison.OrdinalIgnoreCase))
			{
				return ShapeMatchCriterion.IgnoreShape;
			}

			throw new InvalidConfigurationException(
				string.Format("Unsupported involved objects match criterion value: {0}", value));
		}

		[CanBeNull]
		private IBox GetBox([CanBeNull] IGeometry shape)
		{
			if (shape == null)
			{
				return null;
			}

			shape.QueryEnvelope(_envelopeTemplate);

			return GeometryUtils.Get2DBox(_envelopeTemplate);
		}

		[CanBeNull]
		private IBox GetAreaOfInterestBox([CanBeNull] IGeometry shape)
		{
			if (shape == null)
			{
				return null;
			}

			if (_areaOfInterest == null)
			{
				return null;
			}

			if (((IRelationalOperator) _areaOfInterest.Envelope).Contains(shape))
			{
				return null;
			}

			IEnvelope clipperEnvelope = _areaOfInterest.Envelope;

			IGeometry clipped = GetClipped(shape, clipperEnvelope);

			return GetBox(clipped);
		}

		private static IGeometry GetClipped([NotNull] IGeometry shape,
		                                    [NotNull] IEnvelope clipperEnvelope)
		{
			var polygon = shape as IPolygon;
			if (polygon != null)
			{
				// implements workarounds for QueryClipped issues on polygons
				return GeometryUtils.GetClippedPolygon(polygon, clipperEnvelope);
			}

			IGeometry clipped = GeometryFactory.CreateEmptyGeometry(shape);

			((ITopologicalOperator) shape).QueryClipped(clipperEnvelope, clipped);

			return clipped;
		}

		[CanBeNull]
		private IGeometry GetShape([NotNull] IRow row)
		{
			if (_featureClass == null)
			{
				return null;
			}

			var feature = (IFeature) row;

			IGeometry shape = feature.Shape;

			return shape == null || shape.IsEmpty
				       ? null
				       : shape;
		}

		private double? GetDoubleValue([NotNull] IRow row, int fieldIndex)
		{
			if (fieldIndex < 0)
			{
				return null;
			}

			object value = row.Value[fieldIndex];
			if (value == null || value is DBNull)
			{
				return null;
			}

			var doubleValue = (double) value;

			if (! _tableSupportsNullValues)
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (doubleValue == 0)
				{
					return null;
				}
			}

			return doubleValue;
		}

		[NotNull]
		private static IEnumerable<string> GetFieldNames(
			[NotNull] IIssueTableFields issueTableFields,
			[NotNull] ITable table,
			[NotNull] IEnumerable<IssueAttribute> attributes)
		{
			yield return table.OIDFieldName;

			var featureClass = table as IFeatureClass;
			if (featureClass != null)
			{
				yield return featureClass.ShapeFieldName;
			}

			foreach (IssueAttribute attribute in attributes)
			{
				string name = issueTableFields.GetName(attribute, optional: true);

				if (name == null)
				{
					// no field definition
					continue;
				}

				if (table.FindField(name) < 0)
				{
					// field does not exist in table
					continue;
				}

				yield return name;
			}
		}
	}
}
