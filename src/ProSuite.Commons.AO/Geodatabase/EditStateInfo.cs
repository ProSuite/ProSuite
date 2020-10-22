using System;

namespace ProSuite.Commons.AO.Geodatabase
{
	[Flags]
	public enum EditStateInfo
	{
		/// <summary>
		/// Current State of IWorkspaceEdit2.IsBeingEdited() must be false
		/// </summary>
		MustNotBeEditing = 1,

		/// <summary>
		/// Current State of IWorkspaceEdit.IsBeingEdited() must be true
		/// </summary>
		MustBeEditing = 2,

		/// <summary>
		/// Current State of IWorkspaceEdit.IsInEditOperation must be false
		/// </summary>
		MustNotBeInOperation = 4,

		/// <summary>
		/// Current State of IWorkspaceEdit.IsInEditOperation must be true
		/// </summary>
		MustBeInOperation = 8,

		/// <summary>
		/// Close an existing Edit Session without saving
		/// </summary>
		AbortExistingEditing = 16,

		/// <summary>
		/// Close an existing edit session with saving
		/// </summary>
		StopExistingEditing = 32,

		/// <summary>
		/// Keep edit session open after operation. Otherwise it will have the same state as before
		/// </summary>
		KeepEditing = 64,

		/// <summary>
		/// Save edit session after operation. Otherwise it will have the same state as before
		/// </summary>
		StopEditing = 128,

		/// <summary>
		/// Abort an existing operation 
		/// </summary>
		AbortExistingOperation = 256,

		/// <summary>
		/// stop an existing operation
		/// </summary>
		StopExistingOperation = 512,

		/// <summary>
		/// Do not stop Operation after executing statement. Otherwise it will have the same state as before
		/// </summary>
		KeepOperation = 1024,

		/// <summary>
		/// Stop Operation after executing statement. Otherwise it will have the same state as before
		/// </summary>
		StopOperation = 2048,

		/// <summary>
		/// Do not start an operation for the executing statement
		/// </summary>
		DontStartOperation = 4096
	}
}
