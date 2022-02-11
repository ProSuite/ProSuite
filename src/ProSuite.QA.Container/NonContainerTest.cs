using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Base class for non-container tests (used for easy identification of concrete non-container tests)
	/// </summary>
	public abstract class NonContainerTest : TestBase
	{
		protected NonContainerTest([NotNull] IEnumerable<IReadOnlyTable> tables) : base(tables) { }
	}
}
