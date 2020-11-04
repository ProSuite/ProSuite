using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public interface IGdbTransaction
	{
		event EventHandler Aborted;

		bool CanWrite([NotNull] IWorkspace workspace);

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace to carry out the transaction</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="description">The description.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		bool Execute([NotNull] IWorkspace workspace,
		             [NotNull] Action procedure,
		             [NotNull] string description);

		/// <summary>
		/// Executes the specified procedure in a transaction on the
		/// geodatabase.
		/// </summary>
		/// <param name="workspace">The workspace that the execute transaction is applied to</param>
		/// <param name="procedure">The procedure.</param>
		/// <param name="stateInfo">information about the pre and post transaction state of the workspace</param>
		/// <param name="description">The description.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		bool Execute([NotNull] IWorkspace workspace,
		             [NotNull] Action procedure,
		             EditStateInfo stateInfo,
		             [NotNull] string description);

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <param name="trackCancel">The cancel tracker, allowing the procedure to abort 
		/// the transaction (optional).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		bool Execute([NotNull] IWorkspace workspace,
		             [NotNull] Action<ITrackCancel> procedure,
		             [NotNull] string description,
		             [CanBeNull] ITrackCancel trackCancel);

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="state">The state.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <param name="trackCancel">The cancel tracker (optional).</param>
		/// <returns>
		/// 	<c>true</c> if the operation succeeded, <c>false</c> if it was aborted.
		/// </returns>
		bool Execute([NotNull] IWorkspace workspace,
		             [NotNull] Action<ITrackCancel> procedure,
		             EditStateInfo state,
		             [NotNull] string description,
		             [CanBeNull] ITrackCancel trackCancel);
	}
}
