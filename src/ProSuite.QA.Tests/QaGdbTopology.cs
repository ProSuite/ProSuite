using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
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
	public class QaGdbTopology : NonContainerTest
	{
		private readonly Dictionary<int, string> _involvedTables;
		private readonly IList<ITopology> _topologies;

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
			TopologyReference topology)
			: this(new List<ITopology> { topology.Topology },
			       GetFeatureClasses(topology.Topology)) { }

		[Doc(nameof(DocStrings.QaGdbTopology_1))]
		public QaGdbTopology(
			[Doc(nameof(DocStrings.QaGdbTopology_featureClasses))] [NotNull]
			IList<IReadOnlyFeatureClass> featureClasses)
			: this(GetTopologies(featureClasses), featureClasses) { }
		private QaGdbTopology([NotNull] IList<ITopology> topologies,
		                      [NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses)
			: base(featureClasses)
		{
			Assert.ArgumentNotNull(topologies, nameof(topologies));

			_topologies = topologies;

			_involvedTables = new Dictionary<int, string>();

			foreach (ITopology topology in topologies)
			{
				foreach (IReadOnlyFeatureClass featureClass in GetFeatureClasses(topology))
				{
					IFeatureClass aoFeatureClass = ExpectFeatureClass(featureClass);

					// TODO: Add to involved tables of base class? Currently, they are considered
					// as 'Reference only'
					_involvedTables.Add(aoFeatureClass.ObjectClassID, featureClass.Name);
				}
			}
		}

		#region ITest Members

		protected override ISpatialReference GetSpatialReference()
		{
			return _topologies.Select(topology => ((IGeoDataset) topology).SpatialReference)
			                  .FirstOrDefault();
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
		private static List<IReadOnlyFeatureClass> GetFeatureClasses([NotNull] ITopology topology)
		{
			return TopologyUtils.GetFeatureClasses(topology)
			                    .Select(x => (IReadOnlyFeatureClass) ReadOnlyTableFactory.Create(x))
			                    .ToList();
		}

		private static IList<ITopology> GetTopologies(
			[NotNull] IEnumerable<IReadOnlyFeatureClass> featureClasses)
		{
			List<ITopology> topologies = new List<ITopology>();

			foreach (IReadOnlyFeatureClass featureClass in featureClasses)
			{
				IFeatureClass aoFeatureClass = ExpectFeatureClass(featureClass);

				if (! TopologyUtils.ClassBelongsToTopology(aoFeatureClass,
				                                           out ITopology classTopology))
				{
					throw new ArgumentException(
						$"QaGdbTopology: {featureClass.Name} does not participate in a Geodatabase Topology.");
				}

				if (! topologies.Contains(classTopology))
				{
					topologies.Add(classTopology);
				}
			}

			return topologies;
		}
		private static IFeatureClass ExpectFeatureClass(IReadOnlyFeatureClass featureClass)
		{
			ReadOnlyFeatureClass roFeatureClass = featureClass as ReadOnlyFeatureClass;

			if (roFeatureClass == null)
			{
				throw new ArgumentException(
					$"QaGdbTopology: {featureClass.Name} is no Geodatabase FeatureClass. Only Geodatabase FeatureClasses can participate in a Topology.");
			}

			IFeatureClass aoFeatureClass = roFeatureClass.BaseTable as IFeatureClass;

			if (aoFeatureClass == null)
			{
				throw new ArgumentException(
					$"QaGdbTopology: {featureClass.Name} is no Geodatabase FeatureClass. Only Geodatabase FeatureClasses can participate in a Topology.");
			}

			return aoFeatureClass;
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

			var errorCount = ReportDirtyAreasAsErrors(geometry, null);

			errorCount += ReportTopologyErrors(geometry);

			return errorCount;
		}

		private int ReportTopologyErrors([CanBeNull] IGeometry geometry)
		{
			int errorCount = 0;

			foreach (ITopology topology in _topologies)
			{
				IEnvelope box = geometry != null
					                ? geometry.Envelope
					                : GetTopologyExtent(topology);

				if (box.IsEmpty)
				{
					return NoError;
				}

				errorCount += ReportTopologyErrors(topology, box);
			}

			return errorCount;
		}

		private int ReportTopologyErrors(ITopology topology, [NotNull] IEnvelope box)
		{
			if (box.IsEmpty)
			{
				return NoError;
			}

			var container = (IErrorFeatureContainer) topology;
			ISpatialReference sr = ((ITopologyProperties) topology).SpatialReference;

			IEnumRule rules = ((ITopologyRuleContainer) topology).Rules;
			rules.Reset();

			var errorCount = 0;

			ITopologyRule rule;
			while ((rule = (ITopologyRule) rules.Next()) != null)
			{
				errorCount += ReportTopologyErrors(box, topology, rule, container, sr);
			}

			return errorCount;
		}

		private int ReportTopologyErrors([NotNull] IEnvelope box,
		                                 [NotNull] ITopology topology,
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
						string topologyName = ((IDataset) topology).Name;
						string description = $"{GetRuleName(rule)} ({topologyName})";

						errorCount += ReportError(
							description, GetInvolvedRows(errorFeature),
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
			int errorCount = 0;
			foreach (ITopology topology in _topologies)
			{
				if (geometry == null)
				{
					geometry = GetTopologyExtent(topology);
				}

				if (geometry.IsEmpty)
				{
					continue;
				}

				errorCount += ReportDirtyAreasAsErrors(topology, geometry, validationException);
			}

			return errorCount;
		}

		private int ReportDirtyAreasAsErrors([NotNull] ITopology topology,
		                                     [CanBeNull] IGeometry geometry,
		                                     [CanBeNull] Exception validationException)
		{
			if (geometry == null)
			{
				geometry = GetTopologyExtent(topology);
			}

			if (geometry.IsEmpty)
			{
				return NoError;
			}

			IPolygon dirtyArea = topology.DirtyArea[GetAsPolygon(geometry)];

			if (dirtyArea == null || dirtyArea.IsEmpty)
			{
				return NoError;
			}

			string topologyName = ((IDataset) topology).Name;

			IssueCode issueCode;
			string description =
				GetErrorDescription(topologyName, validationException, out issueCode);

			return ReportError(
				description, GetInvolvedDatasets(topology),
				dirtyArea, issueCode, null);
		}

		[NotNull]
		private static string GetErrorDescription([NotNull] string topologyName,
		                                          [CanBeNull] Exception validationException,
		                                          [CanBeNull] out IssueCode issueCode)
		{
			if (validationException != null)
			{
				issueCode = Codes[Code.ValidationFailed];
				return $"Topology {topologyName}: " +
				       $"Dirty area (topology validation failed: {validationException.Message})";
			}

			issueCode = Codes[Code.DirtyArea];
			return $"Topology {topologyName}: Dirty area (validate topology to remove dirty areas)";
		}

		[NotNull]
		private static IEnvelope GetTopologyExtent(ITopology topology)
		{
			return ((IGeoDataset) topology.FeatureDataset).Extent;
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
