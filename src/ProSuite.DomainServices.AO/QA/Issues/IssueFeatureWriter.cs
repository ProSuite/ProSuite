using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueFeatureWriter : IssueWriter
	{
		private readonly bool _hasM;
		private readonly bool _hasZ;

		public IssueFeatureWriter([NotNull] IFeatureClass featureClass,
		                          [NotNull] IIssueAttributeWriter issueAttributeWriter)
			: base(featureClass, issueAttributeWriter)
		{
			FeatureClass = featureClass;
			SpatialReference = ((IGeoDataset) featureClass).SpatialReference;
			_hasM = DatasetUtils.HasM(featureClass);
			_hasZ = DatasetUtils.HasZ(featureClass);
			GeometryType = featureClass.ShapeType;
		}

		[NotNull]
		public IFeatureClass FeatureClass { get; }

		public esriGeometryType GeometryType { get; }

		[NotNull]
		public ISpatialReference SpatialReference { get; }

		public void CreateSpatialIndex([CanBeNull] ITrackCancel trackCancel)
		{
			// Close the writer first before creating the indexes
			// (to prevent error executing CalculateDefaultGridIndex gp tool at 10.1)
			if (IsOpen)
			{
				Close();
			}

			// Using non-GP dependent creation of spatial index
			//DataManagementUtils.CreateDefaultSpatialIndex(FeatureClass, trackCancel);
			DatasetUtils.CreateSpatialIndex(FeatureClass);
		}

		#region Overrides of IssueWriter

		protected override IRowBuffer CreateRowBuffer()
		{
			return FeatureClass.CreateFeatureBuffer();
		}

		protected override void WriteCore(Issue issue,
		                                  IRowBuffer rowBuffer,
		                                  IGeometry issueGeometry)
		{
			Assert.ArgumentNotNull(issue, nameof(issue));
			Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));
			Assert.ArgumentNotNull(issueGeometry, nameof(issueGeometry));
			Assert.ArgumentCondition(! issueGeometry.IsEmpty, "geometry is empty");

			GeometryUtils.EnsureSchemaZM(issueGeometry, _hasZ, _hasM);

			var featureBuffer = (IFeatureBuffer) rowBuffer;

			featureBuffer.Shape = issueGeometry;
		}

		#endregion
	}
}
