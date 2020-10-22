using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public abstract class EditOperationObserverBase : IEditOperationObserver
	{
		public virtual bool ObserveWorkspaceOperations => false;

		public virtual IEnumerable<IObjectClass> WorkspaceOperationObservableClasses
		{
			get { yield break; }
		}

		public bool IsCompletingOperation { get; set; }

		public virtual void StartedOperation() { }

		public virtual void Updating(IObject objectToBeStored) { }

		public virtual void Creating(IObject newObject) { }

		public virtual void Deleting(IObject deletedObject) { }

		public virtual void CompletingOperation() { }

		public virtual void CompletedOperation() { }
	}
}
