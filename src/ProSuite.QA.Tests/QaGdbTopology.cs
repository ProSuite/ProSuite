using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[TopologyTest]
	public class QaGdbTopology : NonContainerTest, IEditing
	{
		private readonly Dictionary<int, string> _involvedTables;
		private readonly ITopology _topology;

		private bool _allowEditing;
		private bool _isEditing;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string RuleNotFulfilled = "RuleNotFulfilled";
			public const string DirtyArea = "DirtyArea";
			public const string ValidationFailed = "ValidationFailed";

			public Code() : base("GdbTopology") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaGdbTopology_0))]
		public QaGdbTopology(
			[Doc(nameof(DocStrings.QaGdbTopology_topology))] [NotNull]
			ITopology topology)
			: this(topology, GetFeatureClasses(topology)) { }

		private QaGdbTopology([NotNull] ITopology topology,
		                      [NotNull] ICollection<IReadOnlyTable> tables)
			: base(tables)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			_topology = topology;

			_involvedTables = new Dictionary<int, string>(tables.Count);
			foreach (IReadOnlyTable table in tables)
			{
				if (! (table is ReadOnlyTable readOnlyTable))
				{
					throw new ArgumentException("Unsupported input table type");
				}

				var featureClass = (IFeatureClass) readOnlyTable.BaseTable;

				_involvedTables.Add(featureClass.ObjectClassID, ((IDataset) featureClass).Name);
			}
		}

		#region IEditing Members

		public bool AllowEditing
		{
			get { return _allowEditing; }
			set { _allowEditing = value; }
		}

		public bool IsEditing => _isEditing;

		#endregion

		#region ITest Members

		protected override ISpatialReference GetSpatialReference()
		{
			var geoDataset = _topology as IGeoDataset;
			return geoDataset?.SpatialReference;
		}

		protected override void SetConstraintCore(IReadOnlyTable table, int tableIndex,
		                                          string constraint)
		{
			throw new ArgumentException("Cannot set a constraint to a table");
		}

		public override int Execute()
		{
			return ExecuteGeometry(null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return ExecuteGeometry(boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return ExecuteGeometry(area);
		}

		public override int Execute(IEnumerable<IReadOnlyRow> selection)
		{
			return NoError;
		}

		public override int Execute(IReadOnlyRow row)
		{
			return NoError;
		}

		#endregion

		[NotNull]
		private static List<IReadOnlyTable> GetFeatureClasses([NotNull] ITopology topology)
		{
			return TopologyUtils.GetFeatureClasses(topology)
			                    .Select(x => (IReadOnlyTable) ReadOnlyTableFactory.Create(x))
			                    .ToList();
		}

		private int ExecuteGeometry([CanBeNull] IGeometry geometry)
		{
			if (AreaOfInterest != null)
			{
				geometry = geometry == null
					           ? AreaOfInterest
					           : ((ITopologicalOperator) AreaOfInterest)
					           .Intersect(geometry, esriGeometryDimension.esriGeometry2Dimension);
			}

			if (geometry != null && geometry.IsEmpty)
			{
				return NoError;
			}

			IEnvelope box = geometry != null
				                ? geometry.Envelope
				                : GetTopologyExtent();

			var errorCount = 0;

			if (_allowEditing)
			{
				try
				{
					_isEditing = true;

					try
					{
						box = TopologyUtils.ValidateTopology(_topology, box);
					}
					catch (Exception e)
					{
						errorCount += ReportDirtyAreasAsErrors(geometry, e);
					}
				}
				finally
				{
					_isEditing = false;
				}
			}
			else
			{
				errorCount += ReportDirtyAreasAsErrors(geometry, null);
			}

			errorCount += ReportTopologyErrors(box);

			return errorCount;
		}

		private int ReportTopologyErrors([NotNull] IEnvelope box)
		{
			if (box.IsEmpty)
			{
				return NoError;
			}

			var container = (IErrorFeatureContainer) _topology;
			ISpatialReference sr = ((ITopologyProperties) _topology).SpatialReference;

			IEnumRule rules = ((ITopologyRuleContainer) _topology).Rules;
			rules.Reset();

			var errorCount = 0;

			ITopologyRule rule;
			while ((rule = (ITopologyRule) rules.Next()) != null)
			{
				errorCount += ReportTopologyErrors(box, rule, container, sr);
			}

			return errorCount;
		}

		private int ReportTopologyErrors([NotNull] IEnvelope box,
		                                 [NotNull] ITopologyRule rule,
		                                 [NotNull] IErrorFeatureContainer container,
		                                 [NotNull] ISpatialReference spatialReference)
		{
			var errorCount = 0;

			IEnumTopologyErrorFeature errorFeatures = null;

			try
			{
				const bool errors = true;
				const bool exceptions = false;
				errorFeatures =
					container.ErrorFeatures[spatialReference, rule, box, errors, exceptions];

				ITopologyErrorFeature errorFeature = errorFeatures.Next();
				while (errorFeature != null)
				{
					if (! errorFeature.IsException && ! errorFeature.IsDeleted)
					{
						errorCount += ReportError(
							GetRuleName(rule), GetInvolvedRows(errorFeature),
							GetErrorGeometry(errorFeature),
							Codes[Code.RuleNotFulfilled], null);
					}

					errorFeature = errorFeatures.Next();
				}
			}
			catch (Exception e)
			{
				string msg = string.Format(
					"Error getting error features for rule type {0} and\n" +
					"orig: class {1}, subtype {2}; dest: class {3}, subtype {4}",
					rule.TopologyRuleType, rule.OriginClassID, rule.OriginSubtype,
					rule.DestinationClassID, rule.DestinationSubtype);

				throw new InvalidOperationException(msg, e);
			}
			finally
			{
				if (errorFeatures != null)
				{
					Marshal.ReleaseComObject(errorFeatures);
				}
			}

			return errorCount;
		}

		[CanBeNull]
		private static IGeometry GetErrorGeometry(
			[NotNull] ITopologyErrorFeature errorFeature)
		{
			Assert.ArgumentNotNull(errorFeature, nameof(errorFeature));

			IGeometry result = ((IFeature) errorFeature).ShapeCopy;

			if (result != null && ! result.IsEmpty)
			{
				const bool allowReorder = true;
				GeometryUtils.Simplify(result, allowReorder);
			}

			return result;
		}

		[NotNull]
		private InvolvedRows GetInvolvedRows(
			[NotNull] ITopologyErrorFeature errorFeature)
		{
			Assert.ArgumentNotNull(errorFeature, nameof(errorFeature));

			var rowOrig = new InvolvedRow(_involvedTables[errorFeature.OriginClassID],
			                              errorFeature.OriginOID);
			int destClass = errorFeature.DestinationClassID;

			InvolvedRows involved = new InvolvedRows();
			involved.Add(rowOrig);
			if (destClass <= 0)
			{
				return involved;
			}

			var rowDest = new InvolvedRow(_involvedTables[destClass],
			                              errorFeature.DestinationOID);
			involved.Add(rowDest);
			return involved;
		}

		[NotNull]
		private static string GetRuleName([NotNull] ITopologyRule rule)
		{
			string ruleName = rule.Name;

			if (! string.IsNullOrEmpty(ruleName))
			{
				return ruleName;
			}

			ruleName = rule.TopologyRuleType.ToString().Substring(7);
			var i = 1;

			while (i < ruleName.Length)
			{
				char c = ruleName[i];
				if (c >= 'A' && c <= 'Z')
				{
					ruleName = ruleName.Insert(i, " ");
					i++;
				}

				i++;
			}

			return ruleName;
		}

		private int ReportDirtyAreasAsErrors([CanBeNull] IGeometry geometry,
		                                     [CanBeNull] Exception validationException)
		{
			if (geometry == null)
			{
				geometry = GetTopologyExtent();
			}

			if (geometry.IsEmpty)
			{
				return NoError;
			}

			IPolygon dirtyArea = _topology.DirtyArea[GetAsPolygon(geometry)];

			if (dirtyArea == null || dirtyArea.IsEmpty)
			{
				return NoError;
			}

			IssueCode issueCode;
			string description = GetErrorDescription(validationException, out issueCode);

			return ReportError(
				description, GetInvolvedDatasets(_topology),
				dirtyArea, issueCode, null);
		}

		[NotNull]
		private static string GetErrorDescription([CanBeNull] Exception validationException,
		                                          [CanBeNull] out IssueCode issueCode)
		{
			if (validationException != null)
			{
				issueCode = Codes[Code.ValidationFailed];
				return string.Format("Dirty area (topology validation failed: {0})",
				                     validationException.Message);
			}

			issueCode = Codes[Code.DirtyArea];
			return "Dirty area (run test in an edit session to validate dirty areas)";
		}

		[NotNull]
		private IEnvelope GetTopologyExtent()
		{
			return ((IGeoDataset) _topology.FeatureDataset).Extent;
		}

		[NotNull]
		private static InvolvedRows GetInvolvedDatasets(
			[NotNull] ITopology topology)
		{
			Assert.ArgumentNotNull(topology, nameof(topology));

			InvolvedRows involved = new InvolvedRows();
			involved.AddRange(
				TopologyUtils.GetFeatureClasses(topology).Select(
					featureClass => new InvolvedRow(((IDataset) featureClass).Name)).ToList());
			return involved;
		}

		[CanBeNull]
		private static IPolygon GetAsPolygon([CanBeNull] IGeometry geometry)
		{
			if (geometry == null)
			{
				return null;
			}

			if (geometry is IPolygon)
			{
				return (IPolygon) geometry;
			}

			var envelope = geometry as IEnvelope;

			if (envelope == null)
			{
				throw new ArgumentException("Cannot convert " + geometry.GetType() +
				                            " to polygon");
			}

			return GeometryFactory.CreatePolygon(envelope);
		}
	}
}
