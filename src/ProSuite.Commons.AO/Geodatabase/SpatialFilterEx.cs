
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Runtime.InteropServices;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class SpatialFilterEx :
		ITileFilter,
		ISpatialFilter, IQueryFilter2, IQueryFilterDefinition3, IQueryFilterFIDSet,
		IClone, IPersistStream, IXMLSerialize
	{
		private readonly ISpatialFilter _baseFilter;
		public SpatialFilterEx(ISpatialFilter baseFilter)
		{
			_baseFilter = baseFilter;
		}

		public void COMReleaseBaseFilter()
		{
			Marshal.ReleaseComObject(_baseFilter);
		}

		public IEnvelope TileExtent { get; set; }

		public double SpatialResolution
		{
			get => ((IQueryFilter2) _baseFilter).SpatialResolution;
			set => ((IQueryFilter2) _baseFilter).SpatialResolution = value;
		}

		public void AddField(string subField)
		{
			_baseFilter.AddField(subField);
		}

		public void set_GeometryEx(IGeometry geom, bool filterOwnsGeometry)
		{
			_baseFilter.set_GeometryEx(geom, filterOwnsGeometry);
		}

		public string SubFields { get => _baseFilter.SubFields; set => _baseFilter.SubFields = value; }
		public string WhereClause { get => _baseFilter.WhereClause; set => _baseFilter.WhereClause = value; }

		public ISpatialReference get_OutputSpatialReference(string fieldName)
			=> _baseFilter.OutputSpatialReference[fieldName];
		public void set_OutputSpatialReference(string fieldName, ISpatialReference sr)
			=> _baseFilter.OutputSpatialReference[fieldName] = sr;

		public esriSearchOrder SearchOrder { get => _baseFilter.SearchOrder; set => _baseFilter.SearchOrder = value; }
		public esriSpatialRelEnum SpatialRel { get => _baseFilter.SpatialRel; set => _baseFilter.SpatialRel = value; }
		public IGeometry Geometry { get => _baseFilter.Geometry; set => _baseFilter.Geometry = value; }

		public bool FilterOwnsGeometry => _baseFilter.FilterOwnsGeometry;

		public string GeometryField { get => _baseFilter.GeometryField; set => _baseFilter.GeometryField = value; }
		public string SpatialRelDescription { get => _baseFilter.SpatialRelDescription; set => _baseFilter.SpatialRelDescription = value; }

		public void GetPaginationClause(out int offset, out int rowCount)
		{
			((IQueryFilterDefinition3) _baseFilter).GetPaginationClause(out offset, out rowCount);
		}

		public void SetPaginationClause(int offset, int rowCount)
		{
			((IQueryFilterDefinition3)_baseFilter).SetPaginationClause(offset, rowCount);
		}

		public string PostfixClause
		{
			get => ((IQueryFilterDefinition3) _baseFilter).PostfixClause;
			set => ((IQueryFilterDefinition3) _baseFilter).PostfixClause = value;
		}

		public IFilterDefs FilterDefs
		{
			get => ((IQueryFilterDefinition3) _baseFilter).FilterDefs;
			set => ((IQueryFilterDefinition3) _baseFilter).FilterDefs = value;
		}

		public string PrefixClause
		{
			get => ((IQueryFilterDefinition3) _baseFilter).PrefixClause;
			set => ((IQueryFilterDefinition3) _baseFilter).PrefixClause = value;
		}

		public IFIDSet FIDSet
		{
			get => ((IQueryFilterFIDSet)_baseFilter).FIDSet;
			set => ((IQueryFilterFIDSet)_baseFilter).FIDSet = value;
		}

		IClone IClone.Clone()
		{
			IClone baseClone = ((IClone) _baseFilter).Clone();
			return new SpatialFilterEx((ISpatialFilter) baseClone);
		}

		void IClone.Assign(IClone src)
		{
			throw new System.NotImplementedException();
		}

		bool IClone.IsEqual(IClone other)
		{
			throw new System.NotImplementedException();
		}

		bool IClone.IsIdentical(IClone other)
		{
			throw new System.NotImplementedException();
		}

		void IPersistStream.GetClassID(out Guid pClassID)
		{
			throw new NotImplementedException();
		}

		void IPersistStream.IsDirty()
		{
			throw new NotImplementedException();
		}

		void IPersistStream.Load(IStream pstm)
		{
			throw new NotImplementedException();
		}

		void IPersistStream.Save(IStream pstm, int fClearDirty)
		{
			throw new NotImplementedException();
		}

		void IPersistStream.GetSizeMax(out _ULARGE_INTEGER pcbSize)
		{
			throw new NotImplementedException();
		}

		void IPersist.GetClassID(out Guid pClassID)
		{
			throw new NotImplementedException();
		}

		void IXMLSerialize.Serialize(IXMLSerializeData data)
		{
			throw new NotImplementedException();
		}

		void IXMLSerialize.Deserialize(IXMLSerializeData data)
		{
			throw new NotImplementedException();
		}
	}

}
