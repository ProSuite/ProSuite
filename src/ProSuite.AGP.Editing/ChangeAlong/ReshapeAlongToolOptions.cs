using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ManagedOptions;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ReshapeAlongToolOptions : ChangeAlongToolOptions
	{
		public ReshapeAlongToolOptions([CanBeNull] PartialChangeAlongToolOptions centralOptions,
		                              [CanBeNull] PartialChangeAlongToolOptions localOptions)
			: base(centralOptions, localOptions)
		{
		}

		public ReshapeAlongToolOptions() : this(null, null)
		{
		}
	}
}
