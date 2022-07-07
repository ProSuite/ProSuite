using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Tests.Transformers
{
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

		void IRowsCache.Add(IReadOnlyRow row)
		{
			VirtualRow baseRow = (VirtualRow) ((row as IFeatureProxy)?.Inner ?? row);
			Assert.NotNull((TransformedBackingDataset) BackingDataset,
			               $"{nameof(BackingDataset)} not set").AddToCache(baseRow);
		}

		bool IRowsCache.Remove(int oid)
		{
			return Assert.NotNull((TransformedBackingDataset) BackingDataset,
			                      $"{nameof(BackingDataset)} not set").RemoveFromCache(oid);
		}
	}
}
