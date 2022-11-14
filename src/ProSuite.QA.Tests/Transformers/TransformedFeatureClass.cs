using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Simple, concrete implementation of a transformed feature class that can be
	/// re-used for standard transformers with no special functionality.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TransformedFeatureClass<T> : TransformedFeatureClassBase<T>
		where T : TransformedBackingData
	{
		public TransformedFeatureClass(
			int? objectClassId,
			[NotNull] string name,
			esriGeometryType shapeType,
			[NotNull] Func<GdbTable, T> createBackingDataset,
			[CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, shapeType, createBackingDataset, workspace) { }

		#region Overrides of TransformedFeatureClassBase<T>

		// TODO: NoCaching == true results in wrong results!
		public override bool NoCaching { get; internal set; } = false;

		#endregion
	}

	// TODO: Consider renaming to CachedTransformedFeatureClass, derive from TransformedFeatureClassBase?
	public class TransformedFeatureClass : GdbFeatureClass, IRowsCache
	{
		public TransformedFeatureClass(
			int? objectClassId,
			[NotNull] string name,
			esriGeometryType shapeType,
			[CanBeNull] string aliasName = null,
			[CanBeNull] Func<GdbTable, TransformedBackingDataset> createBackingDataset = null,
			[CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, shapeType, aliasName, createBackingDataset, workspace) { }

		void IRowsCache.Add(IReadOnlyRow row)
		{
			VirtualRow baseRow = (VirtualRow) ((row as IFeatureProxy)?.Inner ?? row);
			Assert.NotNull((TransformedBackingDataset) BackingDataset,
			               $"{nameof(BackingDataset)} not set").AddToCache(baseRow);
		}

		bool IRowsCache.Remove(long oid)
		{
			return Assert.NotNull((TransformedBackingDataset) BackingDataset,
			                      $"{nameof(BackingDataset)} not set").RemoveFromCache(oid);
		}
	}
}
