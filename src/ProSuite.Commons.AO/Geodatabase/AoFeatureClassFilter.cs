using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

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
					else { }

					_subFieldSet = subFieldSet;
				}

				return _subFieldSet;
			}
		}

		protected virtual ITableFilter Clone()
		{
			AoTableFilter clone = (AoTableFilter) MemberwiseClone();
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
			AoFeatureClassFilter clone = (AoFeatureClassFilter) base.Clone();
			if (FilterGeometry != null)
			{
				clone.FilterGeometry = GeometryFactory.Clone(FilterGeometry);
			}

			if (TileExtent != null)
			{
				clone.TileExtent = GeometryFactory.Clone(TileExtent);
			}

			return clone;
		}

		#endregion

		#region Implementation of ITileFilter

		// TODO: Could this be unified with FilterHelper.FullGeometrySearch?
		//       Or should ITableFilter and FilterHelper be combined to a QaFilter superstructure?
		//       Currently, this property is only checked for null
		public IEnvelope TileExtent { get; set; }

		#endregion
	}
}
