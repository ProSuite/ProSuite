using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcQueryFilter : IQueryFilter
	{
		private readonly QueryFilter _proQueryFilter;

		public ArcQueryFilter(QueryFilter proQueryFilter)
		{
			_proQueryFilter = proQueryFilter;
		}

		public QueryFilter ProQueryFilter => _proQueryFilter;

		#region Implementation of IQueryFilter

		public string SubFields
		{
			get => _proQueryFilter.SubFields;
			set => _proQueryFilter.SubFields = value;
		}

		public void AddField(string subField)
		{
			string trimmed = subField.Trim();

			if (string.IsNullOrEmpty(_proQueryFilter.SubFields))
			{
				// This changes the meaning!?
				_proQueryFilter.SubFields = trimmed;
			}
			else
			{
				_proQueryFilter.SubFields += ", " + trimmed;
			}
		}

		public string WhereClause
		{
			get => _proQueryFilter.WhereClause;
			set => _proQueryFilter.WhereClause = value;
		}

		public string PostfixClause
		{
			get => _proQueryFilter.PostfixClause;
			set => _proQueryFilter.PostfixClause = value;
		}

		public ISpatialReference get_OutputSpatialReference(string fieldName)
		{
			return new ArcSpatialReference(_proQueryFilter.OutputSpatialReference);
		}

		public void set_OutputSpatialReference(
			string fieldName,
			ISpatialReference outputSpatialReference)
		{
			var arcSpatialReference = outputSpatialReference as ArcSpatialReference;

			Assert.NotNull(arcSpatialReference);

			_proQueryFilter.OutputSpatialReference =
				arcSpatialReference.ProSpatialReference;
		}

		#endregion
	}

	public class ArcSpatialFilter : ArcQueryFilter, ISpatialFilter
	{
		private readonly SpatialQueryFilter _proSpatialFilter;

		public ArcSpatialFilter(SpatialQueryFilter proSpatialFilter) : base(proSpatialFilter)
		{
			_proSpatialFilter = proSpatialFilter;
		}

		public SpatialQueryFilter ProSpatialFilter => _proSpatialFilter;

		#region Implementation of ISpatialFilter

		public esriSpatialRelEnum SpatialRel
		{
			get => (esriSpatialRelEnum) _proSpatialFilter.SpatialRelationship;
			set => _proSpatialFilter.SpatialRelationship = (SpatialRelationship) value;
		}

		public IGeometry Geometry
		{
			get => ArcGeometry.Create(_proSpatialFilter.FilterGeometry);
			set => _proSpatialFilter.FilterGeometry = ArcGeometryUtils.CreateProGeometry(value);
		}

		public string SpatialRelDescription
		{
			get => _proSpatialFilter.SpatialRelationshipDescription;
			set => _proSpatialFilter.SpatialRelationshipDescription = value;
		}

		#endregion
	}
}
