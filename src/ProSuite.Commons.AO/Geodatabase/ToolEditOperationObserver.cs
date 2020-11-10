using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Provides the possibility to observe higher-level edits that can be reported by
	/// specific edit tools.
	/// </summary>
	[CLSCompliant(false)]
	public abstract class ToolEditOperationObserver : EditOperationObserverBase
	{
		public virtual void Splitting([CanBeNull] IFeature original,
		                              [NotNull] IEnumerable<IFeature> inserts) { }

		public virtual void Split([CanBeNull] IFeature storedOriginal,
		                          [NotNull] IEnumerable<IFeature> storedInserts) { }

		public virtual void Merging([NotNull] IList<IFeature> featuresToDelete,
		                            [NotNull] IFeature featureToUpdate) { }

		public virtual void Merged([NotNull] IFeature updatedFeature) { }

		public virtual void Updated([NotNull] IObject storedObject) { }
	}
}
