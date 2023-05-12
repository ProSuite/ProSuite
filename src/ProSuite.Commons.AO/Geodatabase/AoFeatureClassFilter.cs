using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class AoTableFilter : ITableFilter
	{
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
		public string PostfixClause { get; set; }

		ITableFilter ITableFilter.Clone() => Clone();

		public bool AddField(string field)
		{
			string trimmed = field.Trim();
			if (SubfieldSet.Add(trimmed))
			{
				_subFields = $"{_subFields},{trimmed}".TrimStart(',');
				return true;
			}

			return false;
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
					else
					{
						
					}

					_subFieldSet = subFieldSet;
				}

				return _subFieldSet;
			}
		}

		protected virtual ITableFilter Clone()
		{
			AoTableFilter clone = (AoTableFilter) Activator.CreateInstance(GetType());
			clone.SubFields = SubFields;
			clone.WhereClause = WhereClause;
			clone.PostfixClause = PostfixClause;
			return clone;
		}
	}

	public class AoFeatureClassFilter : AoTableFilter, IFeatureClassFilter, ITileFilter
	{
		public AoFeatureClassFilter()
		{
			SpatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects;
		}

		public AoFeatureClassFilter(
			[NotNull] IGeometry filterGeometry,
			esriSpatialRelEnum spatialRelationship = esriSpatialRelEnum.esriSpatialRelIntersects)
		{
			FilterGeometry = filterGeometry;
			SpatialRelationship = spatialRelationship;
		}

		#region Implementation of IFeatureClassFilter

		public esriSpatialRelEnum SpatialRelationship { get; set; }
		public string SpatialRelDescription { get; set; }

		public IGeometry FilterGeometry { get; set; }

		protected override ITableFilter Clone()
		{
			AoFeatureClassFilter clone = (AoFeatureClassFilter)base.Clone();
			if (FilterGeometry != null)
			{
				clone.FilterGeometry = GeometryFactory.Clone(FilterGeometry);
			}

			clone.SpatialRelationship = SpatialRelationship;
			clone.SpatialRelDescription = SpatialRelDescription;
			return clone;
		}

		#endregion

		#region Implementation of ITileFilter

		public IEnvelope TileExtent { get; set; }

		#endregion
	}
}
