using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Workflow
{
	/// <summary>
	/// Abstraction for the editing application that allows the determination which DDX datasets
	/// can be edited. Low-level interface that is the base of the 'WorkUnit' concept.
	/// </summary>
	public interface IEditContext
	{
		/// <summary>
		/// Gets the editable datasets of a given type.
		/// </summary>
		/// <typeparam name="T">The dataset type to return.</typeparam>
		/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the
		/// conditions of the datasets to search for.</param>
		/// <returns></returns>
		IList<T> GetEditableDatasets<T>([CanBeNull] Predicate<T> match = null)
			where T : class, IDdxDataset;

		/// <summary>
		/// Whether the given dataset is editable or not.
		/// </summary>
		/// <param name="dataset"></param>
		/// <returns></returns>
		bool IsEditable([NotNull] IDdxDataset dataset);
	}
}
