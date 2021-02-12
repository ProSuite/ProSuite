using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.Properties;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueGeodatabaseCreator
	{
		[NotNull] private readonly IFeatureWorkspace _featureWorkspace;
		[NotNull] private readonly IIssueTableFields _fields;
		[NotNull] private readonly IssueRowWriter _rowWriter;

		[NotNull] private readonly IList<IssueFeatureWriter> _featureWriters =
			new List<IssueFeatureWriter>();

		public IssueGeodatabaseCreator([NotNull] IFeatureWorkspace featureWorkspace,
		                               [NotNull] IIssueTableFieldManagement fields,
		                               [CanBeNull] ISpatialReference spatialReference,
		                               double gridSize1 = 0d,
		                               double gridSize2 = 0d,
		                               double gridSize3 = 0d)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNull(fields, nameof(fields));

			_featureWorkspace = featureWorkspace;
			_fields = fields;

			_rowWriter = CreateRowWriter(IssueDatasetUtils.RowClassName, featureWorkspace,
			                             fields, LocalizableStrings.IssuesStandaloneTableName);

			if (spatialReference != null)
			{
				var spatialReferenceCopy =
					(ISpatialReference) ((IClone) spatialReference).Clone();
				if (! spatialReferenceCopy.HasZPrecision())
				{
					SpatialReferenceUtils.SetZDomain(spatialReferenceCopy,
					                                 -10000, 100000,
					                                 0.0001, 0.001);
				}

				_featureWriters.Add(
					CreateFeatureWriter(
						IssueDatasetUtils.PolygonClassName, featureWorkspace, fields,
						esriGeometryType.esriGeometryPolygon,
						spatialReferenceCopy, gridSize1, gridSize2, gridSize3,
						LocalizableStrings.RawIssuesLayerName_Polygon));

				_featureWriters.Add(
					CreateFeatureWriter(
						IssueDatasetUtils.PolylineClassName, featureWorkspace, fields,
						esriGeometryType.esriGeometryPolyline,
						spatialReferenceCopy, gridSize1, gridSize2, gridSize3,
						LocalizableStrings.RawIssuesLayerName_Polyline));

				_featureWriters.Add(
					CreateFeatureWriter(
						IssueDatasetUtils.MultipointClassName, featureWorkspace, fields,
						esriGeometryType.esriGeometryMultipoint,
						spatialReferenceCopy, gridSize1, gridSize2, gridSize3,
						LocalizableStrings.RawIssuesLayerName_Multipoint));

				_featureWriters.Add(
					CreateFeatureWriter(
						IssueDatasetUtils.MultiPatchClassName, featureWorkspace, fields,
						esriGeometryType.esriGeometryMultiPatch,
						spatialReferenceCopy, gridSize1, gridSize2, gridSize3,
						LocalizableStrings.RawIssuesLayerName_MultiPatch));
			}
		}

		[NotNull]
		private static IssueRowWriter CreateRowWriter(
			[NotNull] string className,
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] IIssueTableFieldManagement fields,
			[CanBeNull] string aliasName = null)
		{
			ITable table = CreateTable(className, featureWorkspace, fields);

			if (StringUtils.IsNotEmpty(aliasName))
			{
				DatasetUtils.TrySetAliasName(table, aliasName);
			}

			var attributeWriter = new IssueAttributeWriter(table, fields);

			return new IssueRowWriter((IObjectClass) table, attributeWriter);
		}

		[NotNull]
		private static IssueFeatureWriter CreateFeatureWriter(
			[NotNull] string className,
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] IIssueTableFieldManagement fields,
			esriGeometryType geometryType,
			[NotNull] ISpatialReference spatialReference,
			double gridSize1, double gridSize2, double gridSize3,
			[NotNull] string aliasName)
		{
			IFeatureClass featureClass = CreateFeatureClass(className, featureWorkspace,
			                                                fields, geometryType,
			                                                spatialReference,
			                                                gridSize1, gridSize2, gridSize3);

			DatasetUtils.TrySetAliasName(featureClass, aliasName);

			var attributeWriter = new IssueAttributeWriter((ITable) featureClass, fields);

			return new IssueFeatureWriter(featureClass, attributeWriter);
		}

		[NotNull]
		public IIssueRepository GetIssueRepository()
		{
			return new IssueRepository(_rowWriter,
			                           _featureWriters,
			                           _fields,
			                           _featureWorkspace);
		}

		[NotNull]
		private static ITable CreateTable(
			[NotNull] string name,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] IIssueTableFieldManagement fields)
		{
			return DatasetUtils.CreateTable(workspace, name, null,
			                                FieldUtils.CreateFields(
				                                CreateAttributeFields(fields)));
		}

		[NotNull]
		private static IFeatureClass CreateFeatureClass(
			[NotNull] string name,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] IIssueTableFieldManagement fields,
			esriGeometryType geometryType,
			[NotNull] ISpatialReference spatialReference,
			double gridSize1, double gridSize2, double gridSize3)
		{
			const bool zAware = true;

			var list = new List<IField>
			           {
				           FieldUtils.CreateShapeField(
					           geometryType, spatialReference,
					           gridSize1, gridSize2, gridSize3,
					           zAware)
			           };
			list.AddRange(CreateAttributeFields(fields));

			return DatasetUtils.CreateSimpleFeatureClass(workspace, name,
			                                             FieldUtils.CreateFields(list));
		}

		[NotNull]
		private static IEnumerable<IField> CreateAttributeFields(
			[NotNull] IIssueTableFieldManagement fields)
		{
			yield return FieldUtils.CreateOIDField();
			foreach (IField field in fields.CreateFields())
			{
				yield return field;
			}
		}
	}
}
