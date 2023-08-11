namespace ProSuite.DomainServices.AO.QA.Issues
{
	public enum IssueAttribute
	{
		IssueDescription,

		IssueCode,

		IssueCodeDescription,

		InvolvedObjects,

		QualityConditionName,

		TestName,

		TestDescription,

		TestType,

		IssueSeverity,

		IsStopCondition,

		Category,

		AffectedComponent,

		Url,

		DoubleValue1,

		DoubleValue2,

		TextValue,

		QualityConditionUuid,

		QualityConditionVersionUuid,

		IssueAssignment,

		ExceptionStatus,

		/// <summary>
		/// Identification of the origin of an exception (entity that defined the exception). 
		/// Can be freely used by the organisation that defines the exception.
		/// </summary>
		ExceptionOrigin,

		ExceptionNotes,

		ExceptionCategory,

		ExceptionDefinitionDate,

		ExceptionLastRevisionDate,

		/// <summary>
		/// Retirement date for exception
		/// </summary>
		ExceptionRetirementDate,

		/// <summary>
		/// Exception match criterion for geometries
		/// </summary>
		ExceptionShapeMatchCriterion,

		ManagedExceptionOrigin,

		ManagedExceptionLineageUuid,

		ManagedExceptionVersionBeginDate,

		ManagedExceptionVersionEndDate,

		ManagedExceptionVersionUuid,

		ManagedExceptionVersionOrigin,

		ManagedExceptionVersionImportStatus,

		ExportedExceptionUsageCount,

		ExportedExceptionObjectId,

		CorrectionStatus
	}
}
