using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.System
{
	/// <summary>
	/// Signature for logging message objects. Can be used to pass e.g. a log4net logging method
	/// to a component that has no dependency on log4net.
	/// </summary>
	/// <param name="message">The message.</param>
	public delegate void LogMethod([NotNull] object message);
}
