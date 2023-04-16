using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObject : IEquatable<ExceptionObject>
	{
		[NotNull] private readonly IList<InvolvedTable> _involvedTables;
		[CanBeNull] private readonly ManagedProperties _managedProperties;

		public ExceptionObject(long id,
		                       Guid qualityConditionUuid,
		                       Guid qualityConditionVersionUuid,
		                       [CanBeNull] IBox shapeEnvelope,
		                       double? xyTolerance,
		                       esriGeometryType? shapeType,
		                       ShapeMatchCriterion shapeMatchCriterion,
		                       [CanBeNull] string issueCode,
		                       [CanBeNull] string affectedComponent,
		                       [NotNull] IList<InvolvedTable> involvedTables,
		                       [CanBeNull] string involvedTablesText = null,
		                       ExceptionObjectStatus status = ExceptionObjectStatus.Active,
		                       double? doubleValue1 = null,
		                       double? doubleValue2 = null,
		                       [CanBeNull] string textValue = null,
		                       [CanBeNull] string exceptionCategory = null,
		                       bool intersectsAreaOfInterest = true,
		                       [CanBeNull] IBox aoiShapeEnvelope = null,
		                       string managedOrigin = null,
		                       Guid? managedLineageUuid = null,
		                       DateTime? managedVersionBeginDate = null,
		                       DateTime? managedVersionEndDate = null,
		                       Guid? managedVersionUuid = null,
		                       string managedVersionOrigin = null)
		{
			Assert.ArgumentNotNull(involvedTables, nameof(involvedTables));

			Id = id;
			QualityConditionUuid = qualityConditionUuid;
			QualityConditionVersionUuid = qualityConditionVersionUuid;
			ShapeEnvelope = shapeEnvelope;
			AreaOfInterestShapeEnvelope = aoiShapeEnvelope;
			XYTolerance = xyTolerance;
			ShapeType = shapeType;
			ShapeMatchCriterion = shapeMatchCriterion;
			IssueCode = issueCode;
			AffectedComponent = affectedComponent;
			_involvedTables = involvedTables;
			InvolvedTablesText = involvedTablesText;
			Status = status;
			DoubleValue1 = doubleValue1;
			DoubleValue2 = doubleValue2;
			TextValue = textValue;
			ExceptionCategory = exceptionCategory;
			IntersectsAreaOfInterest = intersectsAreaOfInterest;

			if (managedOrigin != null || managedLineageUuid != null ||
			    managedVersionBeginDate != null || managedVersionEndDate != null ||
			    managedVersionUuid != null || managedVersionOrigin != null)
			{
				_managedProperties = new ManagedProperties(managedOrigin, managedLineageUuid,
				                                           managedVersionBeginDate,
				                                           managedVersionEndDate,
				                                           managedVersionUuid,
				                                           managedVersionOrigin);
			}
		}

		public long Id { get; }

		public Guid QualityConditionUuid { get; }

		public Guid QualityConditionVersionUuid { get; }

		[CanBeNull]
		public IBox ShapeEnvelope { get; }

		[CanBeNull]
		public IBox AreaOfInterestShapeEnvelope { get; }

		public esriGeometryType? ShapeType { get; }

		public double? XYTolerance { get; }

		public ShapeMatchCriterion ShapeMatchCriterion { get; }

		[CanBeNull]
		public string IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		[NotNull]
		public IEnumerable<InvolvedTable> InvolvedTables => _involvedTables;

		[CanBeNull]
		public string InvolvedTablesText { get; }

		public ExceptionObjectStatus Status { get; }

		public double? DoubleValue1 { get; }

		public double? DoubleValue2 { get; }

		[CanBeNull]
		public string TextValue { get; }

		[CanBeNull]
		public string ExceptionCategory { get; }

		public bool IntersectsAreaOfInterest { get; }

		public string ManagedOrigin => _managedProperties?.Origin;

		public Guid? ManagedLineageUuid => _managedProperties?.LineageUuid;

		public DateTime? ManagedVersionBeginDate => _managedProperties?.VersionBeginDate;

		public DateTime? ManagedVersionEndDate => _managedProperties?.VersionEndDate;

		public Guid? ManagedVersionUuid => _managedProperties?.VersionUuid;

		[CanBeNull]
		public string ManagedVersionOrigin => _managedProperties?.VersionOrigin;

		public override string ToString()
		{
			return string.Format("Id: {0}, Geometry: {1}, Quality Condition: {2}",
			                     Id,
			                     ExceptionObjectUtils.GetShapeTypeText(ShapeType),
			                     QualityConditionUuid);
		}

		public bool Equals(ExceptionObject other)
		{
			// NOTE: assumes that exceptions are read from at most one table per shape type
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Id == other.Id && ShapeType == other.ShapeType;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((ExceptionObject) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Id.GetHashCode() * 397) ^ ShapeType?.GetHashCode() ?? 0;
			}
		}

		private class ManagedProperties
		{
			public ManagedProperties([CanBeNull] string origin,
			                         Guid? lineageUuid,
			                         DateTime? versionBeginDate,
			                         DateTime? versionEndDate,
			                         Guid? versionUuid,
			                         string versionOrigin)
			{
				Origin = origin;
				LineageUuid = lineageUuid;
				VersionBeginDate = versionBeginDate;
				VersionEndDate = versionEndDate;
				VersionUuid = versionUuid;
				VersionOrigin = versionOrigin;
			}

			[CanBeNull]
			public string Origin { get; }

			public Guid? LineageUuid { get; }

			public DateTime? VersionBeginDate { get; }

			public DateTime? VersionEndDate { get; }

			public Guid? VersionUuid { get; }

			[CanBeNull]
			public string VersionOrigin { get; }
		}
	}
}
