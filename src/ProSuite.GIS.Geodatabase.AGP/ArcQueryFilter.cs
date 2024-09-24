using System;
using ArcGIS.Core.Data;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.API;

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
