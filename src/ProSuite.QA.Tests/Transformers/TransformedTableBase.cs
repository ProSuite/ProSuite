using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Base class for transformation result tables whose <see cref="TransformedBackingData"/>
	/// implementation require access to other <see cref="InvolvedTables"/> that are potentially
	/// cached in the <see cref="DataContainer"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class TransformedTableBase<T> : GdbTable, IDataContainerAware
		where T : TransformedBackingData
	{
		protected TransformedTableBase(
			int objectClassId,
			[NotNull] string name,
			[NotNull] Func<GdbTable, T> createBackingDataset,
			[CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, null, createBackingDataset, workspace) { }

		public T BackingData => (T) BackingDataset;

		#region Implementation of IDataContainerAware

		public IList<IReadOnlyTable> InvolvedTables => BackingData.InvolvedTables;

		public IDataContainer DataContainer
		{
			get => BackingData.DataSearchContainer;
			set => BackingData.DataSearchContainer = value;
		}

		#endregion
	}

	/// <summary>
	/// Base class for transformation result feature classes whose <see cref="TransformedBackingData"/>
	/// implementation require access to other <see cref="InvolvedTables"/> that are potentially
	/// cached in the <see cref="DataContainer"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class TransformedFeatureClassBase<T> : GdbFeatureClass, IDataContainerAware
		where T : TransformedBackingData
	{
		protected TransformedFeatureClassBase(
			int objectClassId,
			[NotNull] string name,
			esriGeometryType shapeType,
			[NotNull] Func<GdbTable, T> createBackingDataset,
			[CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, shapeType, null, createBackingDataset, workspace) { }

		public T BackingData => (T) BackingDataset;

		#region Implementation of IDataContainerAware

		public IList<IReadOnlyTable> InvolvedTables => BackingData.InvolvedTables;

		public IDataContainer DataContainer
		{
			get => BackingData.DataSearchContainer;
			set => BackingData.DataSearchContainer = value;
		}

		#endregion
	}
}
