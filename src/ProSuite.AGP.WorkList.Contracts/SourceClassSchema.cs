using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

// TODO: (DARO) move to ProSuite.AGP.WorkList. But that will cause a lot of moving types!
public class SourceClassSchema
{
	public SourceClassSchema([NotNull] Dictionary<string, int> subFields)
	{
		SubFields = subFields;
	}

	[NotNull]
	public Dictionary<string, int> SubFields { get; }
}
