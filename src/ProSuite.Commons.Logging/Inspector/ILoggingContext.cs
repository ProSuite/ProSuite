using System.Collections.Generic;

namespace ProSuite.Commons.Logging.Inspector
{
	/// <summary>
	/// The logging context, which is any number of objects on a stack.
	/// The stack is enumerated in outermost-to-innermost order.
	/// The text representation of the innermost object is available
	/// through the <see cref="TopMessage"/> property.
	/// </summary>
	/// <remarks>
	/// Inspired by log4net's <see cref="T:log4net.Util.ThreadContextStack" />.
	/// </remarks>
	public interface ILoggingContext : IEnumerable<object>
	{
		string TopMessage { get; }
	}
}