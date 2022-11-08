using System.Collections.Generic;

namespace ProSuite.Processing.Domain
{
	public interface ITagged
	{
		ICollection<string> Tags { get; }
	}
}
