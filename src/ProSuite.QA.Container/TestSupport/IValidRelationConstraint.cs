using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public interface IValidRelationConstraint
	{
		bool HasConstraint { get; }

		bool IsFulfilled([NotNull] IRow row1, int tableIndex1,
		                 [NotNull] IRow row2, int tableIndex2,
		                 [NotNull] out string conditionMessage);
	}
}
