extern alias EsriGeodatabase;

using System;
using ESRI.ArcGIS.Geometry;

namespace ESRI.ArcGIS.Geodatabase.AO
{
	public class ArcQueryFilter : IQueryFilter
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IQueryFilter _aoQueryFilter;

		public ArcQueryFilter(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IQueryFilter aoQueryFilter)
		{
			_aoQueryFilter = aoQueryFilter;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IQueryFilter AoQueryFilter
			=> _aoQueryFilter;

		#region Implementation of IQueryFilter

		public string SubFields
		{
			get => _aoQueryFilter.SubFields;
			set => _aoQueryFilter.SubFields = value;
		}

		public void AddField(string subField)
		{
			_aoQueryFilter.AddField(subField);
		}

		public string WhereClause
		{
			get => _aoQueryFilter.WhereClause;
			set => _aoQueryFilter.WhereClause = value;
		}

		public ISpatialReference get_OutputSpatialReference(string fieldName)
		{
			throw new NotImplementedException();
			//return _aoQueryFilter.get_OutputSpatialReference(fieldName);
		}

		public void set_OutputSpatialReference(
			string fieldName,
			ISpatialReference outputSpatialReference)
		{
			throw new NotImplementedException();
			//_aoQueryFilter.set_OutputSpatialReference(fieldName, outputSpatialReference);
		}

		#endregion
	}
}
