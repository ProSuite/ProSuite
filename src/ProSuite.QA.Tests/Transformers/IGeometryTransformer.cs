using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	public interface IGeometryTransformer
	{
		IEnumerable<IFeature> Transform(IGeometry source);
	}

	public class TransformedFeatureClass : GdbFeatureClass, IRowsCache
	{
		public TransformedFeatureClass(int objectClassId,
		                               [NotNull] string name,
		                               esriGeometryType shapeType,
		                               [CanBeNull] string aliasName = null,
		                               [CanBeNull] Func<GdbTable, TransformedBackingDataset> createBackingDataset =
			                               null,
		                               [CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, shapeType, aliasName, createBackingDataset, workspace) { }

		void IRowsCache.Add(IRow row)
		{
			Assert.NotNull((TransformedBackingDataset) BackingDataset,
			               $"{nameof(BackingDataset)} not set").AddToCache(row);
		}

		bool IRowsCache.Remove(int oid)
		{
			return Assert.NotNull((TransformedBackingDataset) BackingDataset,
			                      $"{nameof(BackingDataset)} not set").RemoveFromCache(oid);
		}
	}

	public interface IRowsCache
	{
		bool Remove(int oid);

		void Add(IRow row);
	}
}
