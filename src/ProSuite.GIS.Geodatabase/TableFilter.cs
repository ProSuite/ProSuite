using System;
using System.Collections.Generic;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geodatabase
{
	public class TableFilter : IQueryFilter
	{
		// TODO: Merge with AoTableFilter
		private string _subFields;
		private HashSet<string> _subFieldSet;

		public string SubFields
		{
			get => _subFields;
			set
			{
				_subFields = value;
				_subFieldSet = null;
			}
		}

		public string WhereClause { get; set; }

		public ISpatialReference get_OutputSpatialReference(string fieldName)
		{
			throw new NotImplementedException();
		}

		public void set_OutputSpatialReference(string fieldName,
		                                       ISpatialReference outputSpatialReference)
		{
			throw new NotImplementedException();
		}

		public string PostfixClause { get; set; }

		//TableFilter TableFilter.Clone() => Clone();

		public void AddField(string field)
		{
			string trimmed = field.Trim();
			if (SubfieldSet.Add(trimmed))
			{
				_subFields = $"{_subFields},{trimmed}".TrimStart(',');
				//return true;
			}

			//return false;
		}

		private HashSet<string> SubfieldSet
		{
			get
			{
				if (_subFieldSet == null)
				{
					var subFieldSet =
						new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
					if (! string.IsNullOrWhiteSpace(_subFields))
					{
						foreach (string subfield in _subFields.Split(','))
						{
							subFieldSet.Add(subfield.Trim());
						}
					}
					else { }

					_subFieldSet = subFieldSet;
				}

				return _subFieldSet;
			}
		}
	}
}
