using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaNonEmptyGeometry : NonContainerTest
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyFeatureClass _featureClass;
		private readonly bool _dontFilterPolycurvesByZeroLength;
		private readonly string _shapeFieldName;
		private readonly ISpatialReference _spatialReference;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string GeometryNull = "GeometryNull";
			public const string GeometryEmpty = "GeometryEmpty";

			public Code() : base("EmptyGeometry") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometry(
				[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
				IReadOnlyFeatureClass featureClass)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, false) { }

		[Doc(nameof(DocStrings.QaNonEmptyGeometry_0))]
		public QaNonEmptyGeometry(
			[NotNull] [Doc(nameof(DocStrings.QaNonEmptyGeometry_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaNonEmptyGeometry_dontFilterPolycurvesByZeroLength))]
			bool
				dontFilterPolycurvesByZeroLength)
			: base(new[] { (IReadOnlyTable) featureClass })
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			_featureClass = featureClass;

			// NOTE: querying on shape_length was observed to hang for some feature classes (BUG-000095040)
			//       This could be reproduced with "Select By Attributes" also.
			//       Allow disabling the use of this filter globally, by means of environment variable.
			_dontFilterPolycurvesByZeroLength =
				dontFilterPolycurvesByZeroLength ||
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					"PROSUITE_QA_NONEMPTYGEOMETRY_DONTFILTERBYSHAPELENGTH");

			_shapeFieldName = featureClass.ShapeFieldName;
			_spatialReference = featureClass.SpatialReference;
		}

		[InternallyUsedTest]
		public QaNonEmptyGeometry([NotNull] QaNonEmptyGeometryDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.DontFilterPolycurvesByZeroLength) { }

		#region ITest Members

		public override int Execute()
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IPolygon area)
		{
			return TestFeatures(_featureClass);
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			var errorCount = 0;

			foreach (IReadOnlyRow row in selectedRows)
			{
				if (CancelTestingRow(row))
				{
					continue;
				}

				errorCount += Execute(row);
			}

			return errorCount;
		}

		public override int Execute(IReadOnlyRow row)
		{
			var feature = row as IReadOnlyFeature;

			// if row is not a feature: no error
			return feature == null
				       ? NoError
				       : TestFeature(feature);
		}

		protected override ISpatialReference GetSpatialReference()
		{
			return _spatialReference;
		}

		#endregion

		private int TestFeatures([NotNull] IReadOnlyFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			var errorCount = 0;

			const bool recycling = true;
			ITableFilter filter = CreateFilter(featureClass, GetConstraint(0));

			long previousOid = -1;
			bool tryFallbackImplementation = false;

			// New with 12.5 (probably 10.8.1 x64 too?) for multipatch features:
			// COMException (errorCode -2147220959) with various messages, such as
			// - Insufficient permissions [ORA-00942: table or view does not exist]
			// - The operation was attempted on an empty geometry (when in SDE schema/user)
			// when an empty geometry is encountered!

			HashSet<long> processedOids = new HashSet<long>();
			try
			{
				foreach (IReadOnlyRow feature in
				         featureClass.EnumRows(filter, recycling))
				{
					errorCount += TestFeature((IReadOnlyFeature) feature);
					previousOid = feature.OID;
					processedOids.Add(feature.OID);
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error getting feature from {featureClass.Name}. " +
				           $"Previous successful object id: {previousOid}", e);

				Exception inner = e;
				while (inner != null && ! (inner is COMException))
				{
					inner = inner.InnerException;
				}

				if (inner is COMException com)
				{
					string msg =
						com.ErrorCode == -2147220959 || com.ErrorCode == -2147188959
							? "Error getting feature with presumably empty geometry. Using fall-back implementation (slow) to identify object id."
							: "Error getting feature. Using fall-back implementation (slow) to identify object id.";

					_msg.Debug(msg, e);
					tryFallbackImplementation = true;
				}

				if (! tryFallbackImplementation)
				{
					throw;
				}
			}

			if (! tryFallbackImplementation)
			{
				return errorCount;
			}

			// Read all features without geometry, get geometry separately for each feature:
			filter.SubFields = featureClass.OIDFieldName;

			Stack<long> unhandledOids = new Stack<long>();
			foreach (IReadOnlyRow feature in
			         featureClass.EnumRows(filter, recycling))
			{
				if (processedOids.Add(feature.OID))
				{
					unhandledOids.Push(feature.OID);
				}
			}

			errorCount += GetExceptionErrors(featureClass, unhandledOids);
			return errorCount;
		}

		private class ExMsg
		{
			public string Msg { get; set; }
			public long Oid { get; set; }
		}

		private class ProcessOids
		{
			private static readonly object _lockObj = new object();

			private readonly Stack<long> _oids;
			private readonly TaskWorkspace _tws;
			private readonly string _fcName;

			public ProcessOids(Stack<long> oids, TaskWorkspace tws, string fcName)
			{
				_oids = oids;
				_tws = tws;
				_fcName = fcName;
			}

			public List<ExMsg> GetExceptionMsgs()
			{
				List<ExMsg> errors = new List<ExMsg>();

				IFeatureWorkspace fws = (IFeatureWorkspace) _tws.GetWorkspace();
				IFeatureClass fc = fws.OpenFeatureClass(_fcName);

				IQueryFilter filter = new QueryFilterClass();
				filter.AddField(fc.OIDFieldName);
				filter.AddField(fc.ShapeFieldName);
				while (true)
				{
					HashSet<long> oids = new HashSet<long>();
					lock (_lockObj)
					{
						while (oids.Count < 1000)
						{
							if (_oids.Count <= 0)
							{
								break;
							}

							oids.Add(_oids.Pop());
						}
					}

					if (oids.Count <= 0)
					{
						return errors;
					}

					try
					{
						foreach (var row in GdbQueryUtils.GetRowsInList(
							         (ITable) fc, fc.OIDFieldName, oids, recycle: false, filter))
						{
							oids.Remove(row.OID);
						}
					}
					catch
					{
						ReadOnlyFeatureClass ro = ReadOnlyTableFactory.Create(fc);
						foreach (long oid in oids)
						{
							try
							{
								if (ro.GetRow(oid) is ReadOnlyFeature featureWithGeometry)
								{
									Marshal.ReleaseComObject(featureWithGeometry.BaseRow);
								}
							}
							catch (Exception e)
							{
								errors.Add(new ExMsg { Msg = e.Message, Oid = oid });
							}
						}
					}
				}
			}
		}

		private int GetExceptionErrors(
			[NotNull] IReadOnlyFeatureClass gdbFeatureClass,
			[NotNull] Stack<long> oids,
			int taskCount = -1)
		{
			TaskWorkspace tws = new TaskWorkspace(gdbFeatureClass.Workspace);

			List<ExMsg>[] exceptionMsgsArray;
			if (taskCount > 1)
			{
				List<Task<List<ExMsg>>> tasks = new List<Task<List<ExMsg>>>();
				for (int iTask = 0; iTask < taskCount; iTask++)
				{
					ProcessOids proc = new ProcessOids(oids, tws, gdbFeatureClass.Name);
					tasks.Add(Task.Run(() => proc.GetExceptionMsgs()));
				}

				exceptionMsgsArray = Task.WhenAll(tasks).Result;
			}
			else
			{
				ProcessOids proc = new ProcessOids(oids, tws, gdbFeatureClass.Name);
				exceptionMsgsArray = new[] { proc.GetExceptionMsgs() };
			}

			int errorCount = 0;
			foreach (var errorMsgs in exceptionMsgsArray)
			{
				foreach (ExMsg exMsg in errorMsgs)
				{
					errorCount += ReportError(
						$"Feature geometry cannot be loaded ({exMsg.Msg})",
						new InvolvedRows { new InvolvedRow(gdbFeatureClass.Name, exMsg.Oid) },
						null, Codes[Code.GeometryEmpty], _shapeFieldName);
				}
			}

			return errorCount;
		}

		private int TestFeature([NotNull] IReadOnlyFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeometry geometry = feature.Shape;

			if (geometry == null)
			{
				return ReportError(
					"Feature has no geometry", InvolvedRowUtils.GetInvolvedRows(feature),
					null, Codes[Code.GeometryNull], _shapeFieldName);
			}

			return geometry.IsEmpty
				       ? ReportError(
					       "Feature has empty geometry", InvolvedRowUtils.GetInvolvedRows(feature),
					       null, Codes[Code.GeometryEmpty], _shapeFieldName)
				       : NoError;
		}

		[NotNull]
		private ITableFilter CreateFilter([NotNull] IReadOnlyFeatureClass featureClass,
		                                  [CanBeNull] string filterExpression)
		{
			ITableFilter filter =
				new AoTableFilter
				{
					SubFields = _shapeFieldName,
					WhereClause =
						GetWhereClause(featureClass, filterExpression,
						               _dontFilterPolycurvesByZeroLength)
				};

			var subfields = new List<string> { _shapeFieldName };
			if (featureClass.HasOID)
			{
				subfields.Add(featureClass.OIDFieldName);
			}

			TableFilterUtils.SetSubFields(filter, subfields);

			return filter;
		}

		[NotNull]
		private static string GetWhereClause([NotNull] IReadOnlyFeatureClass featureClass,
		                                     [CanBeNull] string filterExpression,
		                                     bool dontFilterPolycurvesByZeroLength)
		{
			string emptyGeometryWhereClause = dontFilterPolycurvesByZeroLength
				                                  ? null
				                                  : GetEmptyGeometryWhereClause(featureClass);

			if (StringUtils.IsNullOrEmptyOrBlank(filterExpression))
			{
				return emptyGeometryWhereClause ?? string.Empty;
			}

			return emptyGeometryWhereClause == null
				       ? filterExpression
				       : string.Format("({0}) AND ({1})", emptyGeometryWhereClause,
				                       filterExpression);
		}

		[CanBeNull]
		private static string GetEmptyGeometryWhereClause(
			[NotNull] IReadOnlyFeatureClass featureClass)
		{
			esriGeometryType shapeType = featureClass.ShapeType;

			if (shapeType == esriGeometryType.esriGeometryPolygon ||
			    shapeType == esriGeometryType.esriGeometryPolyline)
			{
				IField lengthField = featureClass.LengthField;

				if (lengthField != null)
				{
					return string.Format("{0} IS NULL OR {0} <= 0", lengthField.Name);
				}
			}

			return null;
		}
	}
}
