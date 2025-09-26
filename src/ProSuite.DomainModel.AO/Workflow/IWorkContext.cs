using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public interface IWorkContext : IVerificationContext
	{
		ProductionModel Model { get; }

		string Name { get; }

		/// <summary>
		/// Gets the editable datasets of a given type.
		/// </summary>
		/// <typeparam name="T">The dataset type to return.</typeparam>
		/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the
		/// conditions of the datasets to search for.</param>
		/// <param name="includeDeleted">if set to <c>true</c> deleted datasets are included 
		/// in the result, otherwise they are excluded.</param>
		/// <returns></returns>
		[NotNull]
		IList<T> GetEditableDatasets<T>([CanBeNull] Predicate<T> match = null,
		                                bool includeDeleted = false) where T : class, IDdxDataset;

		bool IsEditable([NotNull] IDdxDataset dataset);

		[CanBeNull]
		IObjectDataset GetEditableDataset([NotNull] IObjectClass objectClass);

		[NotNull]
		IFeatureWorkspace OpenFeatureWorkspace();

		[NotNull]
		IWorkspace OpenWorkspace();
	}
}
