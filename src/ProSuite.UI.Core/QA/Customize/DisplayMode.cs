using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.Core.QA.Customize
{
	public enum DisplayMode
	{
		QualityConditionList = 0,

		[UsedImplicitly] [Obsolete("for backward compatibilty")]
		List = 0,
		Plain,
		Layer,
		Hierarchic,
		DatasetList,
		Category
	}
}
