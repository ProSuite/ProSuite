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
		void IRow.Store() => Store();

		void IObject.Store() => Store();

		public virtual void Store() =>
			throw new NotImplementedException("Implement in derived class");

		void IRow.Delete() => Delete();

		void IObject.Delete() => Delete();

		public virtual void Delete() =>
			throw new NotImplementedException("Implement in derived class");

		object IRow.get_Value(int index) => get_Value(index);

		object IObject.get_Value(int index) => get_Value(index);

		object IRowBuffer.get_Value(int index) => get_Value(index);

		void IRow.set_Value(int index, object value) => set_Value(index, value);

		void IObject.set_Value(int index, object value) => set_Value(index, value);

		void IRowBuffer.set_Value(int index, object value) => set_Value(index, value);

		public virtual object get_Value(int Index) =>
			throw new NotImplementedException("Implement in derived class");

		public virtual void set_Value(int Index, object value) =>
			throw new NotImplementedException("Implement in derived class");

		IFields IRow.Fields => Fields;
		IFields IObject.Fields => Fields;
		IFields IRowBuffer.Fields => Fields;
		public virtual IFields Fields => Table.Fields;

		bool IRow.HasOID => HasOID;
		bool IObject.HasOID => HasOID;
		public virtual bool HasOID => Table.HasOID;

		int IRow.OID => OID;
		int IObject.OID => OID;
		public virtual int OID =>
			throw new NotImplementedException("Implement in derived class");

		ITable IRow.Table => Table;
		ITable IObject.Table => Table;
		public virtual ITable Table =>
			throw new NotImplementedException("Implement in derived class");

		IObjectClass IObject.Class => Class;
		public virtual IObjectClass Class => (IObjectClass) Table;

		public virtual IGeometry Shape
		{
			get => throw new NotImplementedException("Implement in derived class");
			set => throw new NotImplementedException("Implement in derived class");
		}

		public virtual IGeometry ShapeCopy => GeometryFactory.Clone(Shape);
		public virtual IEnvelope Extent => Shape.Envelope;

		public esriFeatureType FeatureType =>
			throw new NotImplementedException("Implement in derived class");
	}
}