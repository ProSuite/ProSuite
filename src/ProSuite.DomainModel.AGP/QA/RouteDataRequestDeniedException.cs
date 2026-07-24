using System;

namespace ProSuite.DomainModel.AGP.QA;

/// <summary>
/// Thrown when a route data query is cancelled because a client-data request was denied - typically
/// the single MCT worker was busy (an edit operation, or a concurrent route layer read), so the
/// microservice could not be answered without risking a dead-lock. Unlike a genuine
/// <see cref="RouteQueryTimeoutException"/> this is transient and self-healing: the caller may skip
/// the current draw and let the next refresh re-query. Subclasses
/// <see cref="RouteQueryTimeoutException"/> so existing "treat as failed / do not cache" handlers
/// keep working; callers that want the softer retry behaviour catch this type specifically.
/// </summary>
public class RouteDataRequestDeniedException : RouteQueryTimeoutException
{
	public RouteDataRequestDeniedException(string message) : base(message) { }

	public RouteDataRequestDeniedException(string message, Exception innerException)
		: base(message, innerException) { }
}
