using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	partial class DistributedTestRunner
	{
		private class WorkspaceInfo
		{
			public string FactoryUid { get; set; }
			public string PropertySet { get; set; }
			public List<string> TableNames { get; set; }
		}

		[NotNull]
		private IList<ReadOnlyFeatureClass> GetParallelBaseFeatureClasses(
			[NotNull] IList<QualityConditionGroup> qcGroups)
		{
			QualityConditionGroup parallelGroup =
				qcGroups.FirstOrDefault(
					x => x.ExecType == QualityConditionExecType.TileParallel);

			if (parallelGroup == null)
			{
				return new List<ReadOnlyFeatureClass>();
			}

			HashSet<IReadOnlyTable> usedTables = new HashSet<IReadOnlyTable>();
			foreach (var tests in parallelGroup.QualityConditions.Values)
			{
				foreach (ITest test in tests)
				{
					foreach (IReadOnlyTable table in test.InvolvedTables)
					{
						AddRecursive(table, usedTables);
					}
				}
			}

			IList<ReadOnlyFeatureClass> baseFcs = new List<ReadOnlyFeatureClass>();
			foreach (IReadOnlyTable table in usedTables)
			{
				if (table is ITransformedTable)
				{
					continue;
				}

				if (table is ReadOnlyFeatureClass baseFc)
				{
					baseFcs.Add(baseFc);
				}
			}

			return baseFcs;
		}

		private void CountData(IEnumerable<SubVerification> verifications,
		                       IList<WorkspaceInfo> workspaceInfos)
		{
			List<IReadOnlyFeatureClass> baseFcs = new List<IReadOnlyFeatureClass>();

			foreach (WorkspaceInfo workspaceInfo in workspaceInfos)
			{
				Guid factoryGuid = new Guid(workspaceInfo.FactoryUid);
				Type factoryClass = Type.GetTypeFromCLSID(factoryGuid);
				var factory =
					Assert.NotNull(
						(IWorkspaceFactory) Activator.CreateInstance(Assert.NotNull(factoryClass)));
				IPropertySet connectionProps =
					PropertySetUtils.FromXmlString(workspaceInfo.PropertySet);
				IWorkspace workspace = factory.Open(connectionProps, 0);

				foreach (string tableName in workspaceInfo.TableNames)
				{
					IFeatureClass fc =
						((IFeatureWorkspace) workspace).OpenFeatureClass(tableName);
					baseFcs.Add(ReadOnlyTableFactory.Create(fc));
				}
			}

			foreach (SubVerification verification in verifications)
			{
				if (verification.QualityConditionGroup.ExecType !=
				    QualityConditionExecType.TileParallel)
				{
					continue;
				}

				IGeometry envelope =
					ProtobufGeometryUtils.FromShapeMsg(
						verification.SubRequest.Parameters.Perimeter);
				if (envelope == null)
				{
					continue;
				}

				long baseRowsCount = 0;
				IFeatureClassFilter filter = new AoFeatureClassFilter(
					envelope, esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects);
				foreach (var baseFc in baseFcs)
				{
					baseRowsCount += baseFc.RowCount(filter);
				}

				verification.InvolvedBaseRowsCount = baseRowsCount;

				IEnvelope e = envelope.Envelope;
				_msg.Debug(
					$"RowCount: {baseRowsCount} [{e.XMin:N0}, {e.YMin:N0}, {e.XMax:N0}, {e.YMax:N0}]");
			}
		}

		private IList<WorkspaceInfo> GetWorkspaceInfos(
			IList<ReadOnlyFeatureClass> roFeatureClasses)
		{
			Dictionary<IWorkspace, HashSet<string>> wsDict =
				new Dictionary<IWorkspace, HashSet<string>>();
			foreach (var roFc in roFeatureClasses)
			{
				IFeatureClass fc = (IFeatureClass) roFc.BaseTable;
				IDataset ds = (IDataset) fc;
				IWorkspace ws = ds.Workspace;
				IList<string> fcNames = new List<string>();
				if (ds.FullName is IQueryName2 qn)
				{
					string tables = qn.QueryDef.Tables;
					foreach (var expression in tables.Split(','))
					{
						foreach (string tableName in expression.Split())
						{
							if (string.IsNullOrWhiteSpace(tableName))
							{
								continue;
							}

							if (((IWorkspace2) ws).get_NameExists(
								    esriDatasetType.esriDTFeatureClass, tableName))
							{
								fcNames.Add(tableName);
							}
						}
					}
				}
				else
				{
					fcNames.Add(ds.Name);
				}

				if (! wsDict.TryGetValue(ws, out HashSet<string> wsInfo))
				{
					wsInfo = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
					wsDict.Add(ws, wsInfo);
				}

				foreach (var fcName in fcNames)
				{
					wsInfo.Add(fcName);
				}
			}

			List<WorkspaceInfo> wsInfos = new List<WorkspaceInfo>();
			foreach (var pair in wsDict)
			{
				IWorkspace ws = pair.Key;
				if (pair.Value.Count == 0)
				{
					continue;
				}

				IWorkspaceFactory factory = ws.WorkspaceFactory;
				WorkspaceInfo wsInfo = new WorkspaceInfo
				                       {
					                       FactoryUid = $"{factory.GetClassID().Value}",
					                       PropertySet =
						                       PropertySetUtils.ToXmlString(
							                       ws.ConnectionProperties),
					                       TableNames = new List<string>()
				                       };
				foreach (string fcName in pair.Value)
				{
					wsInfo.TableNames.Add(fcName);
				}

				wsInfos.Add(wsInfo);
			}

			return wsInfos;
		}

		private void ReportSubverifcationsCreated(
			[NotNull] IEnumerable<SubVerification> subVerifications)
		{
			if (SubverificationObserver == null)
			{
				return;
			}

			foreach (SubVerification subVerification in subVerifications)
			{
				List<string> qcNames = new List<string>();

				var sr = subVerification.SubRequest.Specification;
				HashSet<int> excludes = new HashSet<int>();
				foreach (int exclude in sr.ExcludedConditionIds)
				{
					excludes.Add(exclude);
				}

				foreach (QualitySpecificationElementMsg msg in sr.ConditionListSpecification
				                                                 .Elements)
				{
					if (! excludes.Contains(msg.Condition.ConditionId))
					{
						qcNames.Add(msg.Condition.Name);
					}
				}

				SubverificationObserver?.CreatedSubverification(
					subVerification.Id,
					subVerification.QualityConditionGroup.ExecType,
					qcNames,
					subVerification.TileEnvelope);
			}
		}
	}
}
