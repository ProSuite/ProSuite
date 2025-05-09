using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.QA.IssuePersistence;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueRepository : IIssueRepository
	{
		[NotNull] private readonly IssueRowWriter _rowWriter;
		[NotNull] private readonly IList<IssueFeatureWriter> _featureWriters;
		[NotNull] private readonly IList<IIssueDataset> _issueDatasets;
		[CanBeNull] private readonly ISpatialReference _spatialReference;

		[NotNull] private readonly Dictionary<esriGeometryType, IssueWriter>
			_issueWritersByGeometryType;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IssueRepository([NotNull] IssueRowWriter rowWriter,
		                       [NotNull] IEnumerable<IssueFeatureWriter> featureWriters,
		                       [NotNull] IIssueTableFields fields,
		                       [NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(rowWriter, nameof(rowWriter));
			Assert.ArgumentNotNull(featureWriters, nameof(featureWriters));
			Assert.ArgumentNotNull(fields, nameof(fields));
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			FeatureWorkspace = featureWorkspace;
			_rowWriter = rowWriter;
			_featureWriters = featureWriters.ToList();

			_issueWritersByGeometryType = GetIssueWritersByGeometryType(rowWriter,
				_featureWriters);

			_issueDatasets = GetIssueDatasets(rowWriter, _featureWriters, fields);

			if (_featureWriters.Count > 0)
			{
				_spatialReference =
					SpatialReferenceUtils.GetUniqueSpatialReference(
						GetSpatialReferences(_featureWriters),
						comparePrecisionAndTolerance: true,
						compareVerticalCoordinateSystems: true);
			}
		}

		#region Implementation of IIssueRepository

		public IIssueGeometryTransformation IssueGeometryTransformation { get; set; }

		public IEnumerable<IIssueDataset> IssueDatasets => _issueDatasets;

		public IFeatureWorkspace FeatureWorkspace { get; }

		public void AddIssue(Issue issue, IGeometry issueGeometry)
		{
			Assert.ArgumentNotNull(issue, nameof(issue));

			try
			{
				if (IssueGeometryTransformation != null)
				{
					issueGeometry = IssueGeometryTransformation.TransformGeometry(issue,
						issueGeometry);
				}

				IGeometry storableIssueGeometry = GetStorableIssueGeometry(issueGeometry);

				IssueWriter issueWriter = GetIssueWriter(storableIssueGeometry);

				issueWriter.Write(issue, storableIssueGeometry);
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error adding issue with geometry {0}",
				                 GeometryUtils.ToString(issueGeometry));
				_msg.ErrorFormat("Error adding issue: {0} ({1})",
				                 FormatIssue(issue), ExceptionUtils.FormatMessage(e));
				throw;
			}
		}

		public void CreateIndexes(ITrackCancel trackCancel, bool ignoreErrors = false)
		{
			foreach (IssueFeatureWriter writer in
			         _featureWriters.Where(writer => writer.WriteCount != 0))
			{
				CreateIndex(writer, trackCancel, ignoreErrors);
			}
		}

		#endregion

		#region Implementation of IDisposable

		public void Dispose()
		{
			_rowWriter.Dispose();

			foreach (IssueFeatureWriter writer in _featureWriters)
			{
				writer.Dispose();
			}

			Marshal.ReleaseComObject(FeatureWorkspace);
		}

		#endregion

		[NotNull]
		private static string FormatIssue([NotNull] Issue issue)
		{
			var props = new List<string>
			            {
				            issue.QualityCondition.Name,
				            issue.Description,
				            issue.IssueCode?.ID,
				            IssueUtils.FormatInvolvedTables(issue.InvolvedTables),
			            };

			return StringUtils.Concatenate(props, "|");
		}

		[NotNull]
		private static IEnumerable<ISpatialReference> GetSpatialReferences(
			[NotNull] IEnumerable<IssueFeatureWriter> writers)
		{
			return from writer in writers
			       where writer != null
			       select writer.SpatialReference;
		}

		[NotNull]
		private static Dictionary<esriGeometryType, IssueWriter>
			GetIssueWritersByGeometryType(
				[NotNull] IssueRowWriter rowWriter,
				[NotNull] IEnumerable<IssueFeatureWriter> featureWriters)
		{
			var result =
				new Dictionary<esriGeometryType, IssueWriter>
				{
					{ esriGeometryType.esriGeometryNull, rowWriter }
				};

			foreach (IssueFeatureWriter featureWriter in featureWriters)
			{
				result.Add(featureWriter.GeometryType, featureWriter);
			}

			return result;
		}

		[NotNull]
		private static IList<IIssueDataset> GetIssueDatasets(
			[NotNull] IssueRowWriter rowWriter,
			[NotNull] IEnumerable<IssueFeatureWriter> featureWriters,
			[NotNull] IIssueTableFields fields)
		{
			var result = new List<IIssueDataset>
			             {
				             new IssueTable(rowWriter, fields)
			             };

			foreach (IssueFeatureWriter featureWriter in featureWriters)
			{
				result.Add(new IssueFeatureClass(featureWriter, fields));
			}

			return result;
		}

		private static void CreateIndex([NotNull] IssueFeatureWriter writer,
		                                [CanBeNull] ITrackCancel trackCancel,
		                                bool ignoreErrors)
		{
			_msg.InfoFormat(writer.WriteCount == 1
				                ? "Creating spatial index for {0} issue feature in '{1}'"
				                : "Creating spatial index for {0} issue features in '{1}'",
			                writer.WriteCount, writer.Name);

			try
			{
				writer.CreateSpatialIndex(trackCancel);
			}
			catch (Exception e)
			{
				if (! ignoreErrors)
				{
					throw;
				}

				_msg.Debug("Error creating spatial index", e);
				_msg.WarnFormat("Error creating spatial index for feature class {0}: {1}",
				                writer.Name, e.Message);
			}
		}

		[CanBeNull]
		private IGeometry GetStorableIssueGeometry([CanBeNull] IGeometry issueGeometry)
		{
			if (_spatialReference == null)
			{
				return null;
			}

			return ErrorRepositoryUtils.GetGeometryToStore(issueGeometry,
			                                               _spatialReference,
			                                               _issueWritersByGeometryType
				                                               .Keys);
		}

		[NotNull]
		private IssueWriter GetIssueWriter([CanBeNull] IGeometry geometry)
		{
			if (geometry == null)
			{
				return _rowWriter;
			}

			IssueWriter issueWriter;
			if (_issueWritersByGeometryType.TryGetValue(geometry.GeometryType,
			                                            out issueWriter))
			{
				return issueWriter;
			}

			throw new ArgumentOutOfRangeException();
		}
	}
}
