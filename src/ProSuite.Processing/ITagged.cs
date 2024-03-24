using System.Collections.Generic;

namespace ProSuite.Processing
{
	public interface ITagged
	{
		ICollection<string> Tags { get; }
	}
}
