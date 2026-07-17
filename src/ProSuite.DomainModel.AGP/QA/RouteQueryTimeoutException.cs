using System;

namespace ProSuite.DomainModel.AGP.QA;

/// <summary>
/// Thrown when a route data query is cancelled before it completes (query timeout or a
/// denied client-data request). The rows read so far are a truncated, gap-ridden subset, so
/// the caller must treat the build as failed: never cache the partial result nor present it
/// as a complete route.
/// </summary>
public class RouteQueryTimeoutException : Exception
{
	public RouteQueryTimeoutException(string message) : base(message) { }

	public RouteQueryTimeoutException(string message, Exception innerException)
		: base(message, innerException) { }
}
