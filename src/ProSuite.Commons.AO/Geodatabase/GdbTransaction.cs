using System;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
	public class GdbTransaction : GdbTransactionBase
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbTransaction"/> class.
		/// </summary>
		public GdbTransaction() : this(false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbTransaction"/> class.
		/// </summary>
		/// <param name="reconcileRedefinedVersion">if set to <c>true</c> and the edited 
		/// SDE version was re-defined when saving, a reconcile will be performed. If no 
		/// conflicts are found the edit session can still be saved.</param>
		public GdbTransaction(bool reconcileRedefinedVersion)
			: base(reconcileRedefinedVersion) { }

		#endregion

		#region Overrides of GdbTransactionBase

		protected override bool CanWriteInContext(IWorkspace workspace)
		{
			// no further restrictions
			return true;
		}

		#endregion
	}
}
