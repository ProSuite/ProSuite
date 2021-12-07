using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// template for Feature derived from an implementation of VirtualRow 
	/// </summary>
	public class VirtualFeature : VirtualRow, IFeature { }

	/// <summary>
	/// see VirtualFeature for template for Feature derived from an implementation of VirtualRow 
	/// </summary>
	public class VirtualRow : IObject
	{
		void IRow.Store() => VirtualStore();

		void IObject.Store() => VirtualStore();

		public void Store() => VirtualStore();

		protected virtual void VirtualStore() =>
			throw new NotImplementedException("Implement in derived class");

		void IRow.Delete() => VirtualDelete();

		void IObject.Delete() => VirtualDelete();

		public void Delete() => VirtualDelete();

		protected virtual void VirtualDelete() =>
			throw new NotImplementedException("Implement in derived class");

		object IRow.get_Value(int index) => get_VirtualValue(index);

		object IObject.get_Value(int index) => get_VirtualValue(index);

		object IRowBuffer.get_Value(int index) => get_VirtualValue(index);

		public object get_Value(int index) => get_VirtualValue(index);

		void IRow.set_Value(int index, object value) => set_VirtualValue(index, value);

		void IObject.set_Value(int index, object value) => set_VirtualValue(index, value);

		void IRowBuffer.set_Value(int index, object value) => set_VirtualValue(index, value);

		public void set_Value(int index, object value) => set_VirtualValue(index, value);

		protected virtual object get_VirtualValue(int Index) =>
			throw new NotImplementedException("Implement in derived class");

		protected virtual void set_VirtualValue(int Index, object value) =>
			throw new NotImplementedException("Implement in derived class");

		IFields IRow.Fields => VirtualFields;
		IFields IObject.Fields => VirtualFields;
		IFields IRowBuffer.Fields => VirtualFields;
		public IFields Fields => VirtualFields;
		protected virtual IFields VirtualFields => VirtualTable.Fields;

		bool IRow.HasOID => VirtualHasOID;
		bool IObject.HasOID => VirtualHasOID;
		public bool HasOID => VirtualHasOID;
		protected virtual bool VirtualHasOID => VirtualTable.HasOID;

		int IRow.OID => VirtualOID;
		int IObject.OID => VirtualOID;
		public int OID => VirtualOID;

		protected virtual int VirtualOID =>
			throw new NotImplementedException("Implement in derived class");

		ITable IRow.Table => VirtualTable;
		ITable IObject.Table => VirtualTable;
		public ITable Table => VirtualTable;

		protected virtual ITable VirtualTable =>
			throw new NotImplementedException("Implement in derived class");

		IObjectClass IObject.Class => VirtualObjectClass;
		public IObjectClass Class => VirtualObjectClass;
		protected virtual IObjectClass VirtualObjectClass => (IObjectClass) VirtualTable;

		public IGeometry Shape
		{
			get => VirtualShape;
			set => VirtualShape = value;
		}

		protected virtual IGeometry VirtualShape
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		public IGeometry ShapeCopy => VirtualShapeCopy;
		protected virtual IGeometry VirtualShapeCopy => GeometryFactory.Clone(VirtualShape);
		public IEnvelope Extent => VirtualExtent;
		protected virtual IEnvelope VirtualExtent => VirtualShape.Envelope;

		public esriFeatureType FeatureType => VirtualFeatureType;

		protected esriFeatureType VirtualFeatureType =>
			throw new NotImplementedException("Implement in derived class");
	}
}
