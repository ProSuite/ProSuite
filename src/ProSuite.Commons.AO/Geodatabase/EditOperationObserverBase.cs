using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
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

		public abstract override int GetHashCode();

		public abstract bool Equals(IEditOperationObserver other);

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((EditOperationObserverBase) obj);
		}
	}
}
