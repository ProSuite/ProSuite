using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Provides DDX datasets by their id. In contrast to a project workspace or an edit-session
	/// context - which only expose the datasets that are currently loaded or being edited - an
	/// implementation is expected to be able to resolve any dataset that may be referenced by a
	/// verification, including reference datasets that are not part of the current editing session.
	/// <para>
	/// Implementations may resolve from an in-memory set (such as the datasets registered in a
	/// <see cref="DdxModel"/> or a DatasetLookup) or, in the future, lazily load additional
	/// datasets on demand via the DDX service.
	/// </para>
	/// </summary>
	public interface IDatasetByIdProvider
	{
		/// <summary>
		/// Gets the dataset with the given DDX id, or <c>null</c> if no such dataset can be
		/// resolved.
		/// </summary>
		/// <param name="datasetId">The DDX id of the dataset.</param>
		[CanBeNull]
		Dataset GetDataset(int datasetId);
	}
}
