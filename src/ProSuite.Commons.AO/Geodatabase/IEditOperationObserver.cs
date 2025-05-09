using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Allows implementors to be notified of specific feature-level operations.
	/// The -ing methods can be used to modify the involved features to be stored.
	/// Hence, implementations are not necessarily side-effect-free (observer effect;-)
	/// </summary>
	public interface IEditOperationObserver : IEquatable<IEditOperationObserver>
	{
		/// <summary>
		/// Whether edits performed on the workspace level (outside an editor edit session)
		/// should be observed in addition to editor edit events. This is required for editable
		/// layers that are added to the map *after* starting the editor edit session (s. TOP-5261)
		/// </summary>
		bool ObserveWorkspaceOperations { get; }

		/// <summary>
		/// The object classes on which edits should be performed. This is required if
		/// <see cref="ObserveWorkspaceOperations"/> is true.
		/// </summary>
		IEnumerable<IObjectClass> WorkspaceOperationObservableClasses { get; }

		/// <summary>
		/// Whether the edit operation is currently in its 'completing' state. Used to signal
		/// other obervers that edits can currently be disregarded because they are not directly
		/// caused by the user.
		/// </summary>
		bool IsCompletingOperation { get; set; }

		/// <summary>
		/// Called after the edit operation has been started.
		/// </summary>
		void StartedOperation();

		void Creating([NotNull] IObject newObject);

		void Updating([NotNull] IObject objectToBeStored);

		void Deleting(IObject deletedObject);

		/// <summary>
		/// Called before the edit operation is finished.
		/// THIS IS NOT CALLED OUTSIDE AN EDITOR EDIT OPERATION!
		/// </summary>
		void CompletingOperation();

		/// <summary>
		/// Called when the edit operation is completed.
		/// </summary>
		void CompletedOperation();
	}
}
