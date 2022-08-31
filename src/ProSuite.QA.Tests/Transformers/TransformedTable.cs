using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Simple, concrete implementation of a transformed table that can be
	/// re-used for standard transformers with no special functionality.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TransformedTable<T> : TransformedTableBase<T>
		where T : TransformedBackingData
	{
		public TransformedTable(
			int objectClassId,
			[NotNull] string name,
			[NotNull] Func<GdbTable, T> createBackingDataset,
			[CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, createBackingDataset, workspace) { }
	}
}
