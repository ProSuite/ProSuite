using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainServices.AO.Properties;

namespace ProSuite.DomainServices.AO.QA
{
	public class AreaOfInterestWriter
	{
		[NotNull] private readonly IFeatureWorkspace _featureWorkspace;
		[NotNull] private readonly IDictionary<AttributeRole, string> _fieldNames;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private enum AttributeRole
		{
			Description,
			FeatureSource,
			WhereClause,
			BufferDistance,
			GeneralizationTolerance
		}

		public AreaOfInterestWriter([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			_featureWorkspace = featureWorkspace;
			_fieldNames = GetFieldNames(featureWorkspace);
		}

		[NotNull]
		public IFeatureClass WriteAreaOfInterest(
			[NotNull] AreaOfInterest aoi,
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(aoi, nameof(aoi));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));
			Assert.ArgumentCondition(! aoi.IsEmpty, "area of interest must not be empty");

			IPolygon polygon = GetPolygon(aoi, spatialReference);

			const bool hasM = false;
			bool hasZ = GeometryUtils.IsZAware(polygon) &&
			            ! GeometryUtils.HasUndefinedZValues(polygon);

			// create table
			IFeatureClass featureClass = DatasetUtils.CreateSimpleFeatureClass(
				_featureWorkspace, "AreaOfInterest",
				FieldUtils.CreateFields(GetFields(spatialReference, hasZ, hasM)));
			DatasetUtils.TrySetAliasName(featureClass, LocalizableStrings.AoiLayerName);

			IFeature feature = featureClass.CreateFeature();

			GeometryUtils.EnsureSchemaZM(polygon, hasZ, hasM);
			feature.Shape = polygon;

			Write(feature, AttributeRole.Description, aoi.Description);
			Write(feature, AttributeRole.FeatureSource, aoi.FeatureSource);
			Write(feature, AttributeRole.WhereClause, aoi.WhereClause);
			Write(feature, AttributeRole.BufferDistance, aoi.BufferDistance);
			Write(feature, AttributeRole.GeneralizationTolerance, aoi.GeneralizationTolerance);

			feature.Store();

			Marshal.ReleaseComObject(feature);

			return featureClass;
		}

		[NotNull]
		private static IPolygon GetPolygon([NotNull] AreaOfInterest aoi,
		                                   [NotNull] ISpatialReference spatialReference)
		{
			IPolygon result = aoi.CreatePolygon();

			GeometryUtils.EnsureSpatialReference(result, spatialReference);
			return result;
		}

		private void Write([NotNull] IFeature feature,
		                   AttributeRole role,
		                   [CanBeNull] object value)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			int fieldIndex;
			IField field = GetField(feature.Fields, role, out fieldIndex);

			if (value == null || value is DBNull)
			{
				if (field.IsNullable)
				{
					feature.Value[fieldIndex] = DBNull.Value;
				}

				return;
			}

			if (field.Type != esriFieldType.esriFieldTypeString)
			{
				feature.Value[fieldIndex] = value;
				return;
			}

			var stringValue = value as string;
			Assert.NotNull(stringValue, "Unexpected value for text field: {0}", value);

			bool requiresTrim = stringValue.Length > field.Length;

			if (requiresTrim)
			{
				_msg.WarnFormat("Text is too long for field '{0}', cutting off: {1}",
				                field.Name, stringValue);
			}

			string writeValue = requiresTrim
				                    ? stringValue.Substring(0, field.Length)
				                    : stringValue;

			feature.Value[fieldIndex] = writeValue;
		}

		[NotNull]
		private IField GetField([NotNull] IFields fields,
		                        AttributeRole role,
		                        out int fieldIndex)
		{
			string name = _fieldNames[role];
			fieldIndex = fields.FindField(name);

			return fields.Field[fieldIndex];
		}

		[NotNull]
		private IEnumerable<IField> GetFields([NotNull] ISpatialReference spatialReference,
		                                      bool hasZ = false,
		                                      bool hasM = false)
		{
			yield return FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon,
			                                         spatialReference,
			                                         hasZ: hasZ,
			                                         hasM: hasM);

			yield return
				FieldUtils.CreateTextField(_fieldNames[AttributeRole.Description], 2000);
			yield return
				FieldUtils.CreateTextField(_fieldNames[AttributeRole.FeatureSource], 500);
			yield return
				FieldUtils.CreateTextField(_fieldNames[AttributeRole.WhereClause], 2000);
			yield return
				FieldUtils.CreateDoubleField(_fieldNames[AttributeRole.BufferDistance]);
			yield return
				FieldUtils.CreateDoubleField(_fieldNames[AttributeRole.GeneralizationTolerance]);
		}

		[NotNull]
		private static IDictionary<AttributeRole, string> GetFieldNames(
			[NotNull] IFeatureWorkspace featureWorkspace)
		{
			var workspace = (IWorkspace) featureWorkspace;

			if (WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return new Dictionary<AttributeRole, string>
				       {
					       {AttributeRole.Description, "Description"},
					       {AttributeRole.FeatureSource, "DataSource"},
					       {AttributeRole.WhereClause, "WhereClause"},
					       {AttributeRole.BufferDistance, "BufferDistance"},
					       {AttributeRole.GeneralizationTolerance, "GeneralizationTolerance"}
				       };
			}

			if (WorkspaceUtils.IsShapefileWorkspace(workspace))
			{
				return new Dictionary<AttributeRole, string>
				       {
					       {AttributeRole.Description, "Descript"},
					       {AttributeRole.FeatureSource, "DataSource"},
					       {AttributeRole.WhereClause, "Where"},
					       {AttributeRole.BufferDistance, "BufferDist"},
					       {AttributeRole.GeneralizationTolerance, "GeneralTol"}
				       };
			}

			throw new ArgumentException(
				string.Format(
					"Unsupported workspace for writing area of interest feature class: {0}",
					WorkspaceUtils.GetConnectionString(workspace, true)));
		}
	}
}
