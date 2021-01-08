using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	/// <summary>
	/// Base class for non-container tests (used for easy identification of concrete non-container tests)
	/// </summary>
	[CLSCompliant(false)]
	public abstract class NonContainerTest : TestBase
	{
		protected NonContainerTest([NotNull] IEnumerable<ITable> tables) : base(tables) { }
	}
}
