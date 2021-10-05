using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA.DependencyGraph.GraphML;

namespace ProSuite.DomainModel.Core.QA.DependencyGraph
{
	public static class GraphMLUtils
	{
		private const string _keyNodeLabel = "nlabel";
		private const string _keyEdgeLabel = "elabel";

		private const string _keyGeometryType = "geomType";
		private const string _keyModelName = "model";

		private const string _keyQualityCondition = "qc";
		private const string _keyFromFilterExpression = "fromFilter";
		private const string _keyToFilterExpression = "toFilter";
		private const string _keyFromDataset = "fromDataset";
		private const string _keyFromModel = "fromModel";
		private const string _keyToDataset = "toDataset";
		private const string _keyToModel = "toModel";
		private const string _keyFromParameter = "fromParam";
		private const string _keyToParameter = "toParam";
		private const string _keyIssueType = "issueType";
		private const string _keyStopCondition = "stop";
		private const string _keyCategory = "cat";
		private const string _keyTest = "test";

		private const string _issueTypeWarning = "warning";
		private const string _issueTypeError = "error";

		[NotNull]
		public static graphmltype GetGraphMLDocument([NotNull] DatasetDependencyGraph graph,
		                                             bool includeModelNodes = true)
		{
			var keys = new List<keytype>
			           {
				           CreateKey(_keyNodeLabel, "Label", keyfortype.node),
				           CreateKey(_keyGeometryType, "GeometryType", keyfortype.node),
				           CreateKey(_keyModelName, "Model", keyfortype.node),
				           CreateKey(_keyEdgeLabel, "Label", keyfortype.edge),
				           CreateKey(_keyQualityCondition, "QualityCondition", keyfortype.edge),
				           CreateKey(_keyFromDataset, "FromDataset", keyfortype.edge),
				           CreateKey(_keyToDataset, "ToDataset", keyfortype.edge),
				           CreateKey(_keyFromFilterExpression, "FromFilterExpression",
				                     keyfortype.edge),
				           CreateKey(_keyToFilterExpression, "ToFilterExpression", keyfortype.edge),
				           CreateKey(_keyFromParameter, "FromParameter", keyfortype.edge),
				           CreateKey(_keyToParameter, "ToParameter", keyfortype.edge),
				           CreateKey(_keyIssueType, "IssueType", keyfortype.edge),
				           CreateKey(_keyStopCondition, "IsStopCondition", keyfortype.edge,
				                     "boolean"),
				           CreateKey(_keyCategory, "Category", keyfortype.edge),
				           CreateKey(_keyTest, "Test", keyfortype.edge)
			           };

			var rootItems = new List<object>();

			rootItems.AddRange(graph.DatasetDependencies.Select(CreateEdge).Cast<object>());

			if (includeModelNodes)
			{
				rootItems.AddRange(graph.ModelNodes.Select(CreateModelNode).Cast<object>());
			}
			else
			{
				rootItems.AddRange(
					graph.DatasetNodes.Select(
						     node => CreateDatasetNode(node, useQualifiedLabel: true))
					     .Cast<object>());
			}

			var mlgraph = new graphtype
			              {
				              edgedefault = graphedgedefaulttype.directed,
				              Items = rootItems.ToArray()
			              };

			var document = new graphmltype
			               {
				               key = keys.ToArray(),
				               Items = new object[] {mlgraph}
			               };

			document.desc = graph.Description;

			return document;
		}

		[NotNull]
		private static keytype CreateKey([NotNull] string id,
		                                 [NotNull] string name,
		                                 keyfortype target,
		                                 [NotNull] string type = "string")
		{
			return new keytype
			       {
				       id = id,
				       Name = name,
				       @for = target,
				       Type = type
			       };
		}

		[NotNull]
		private static edgetype CreateEdge([NotNull] DatasetDependency dependency)
		{
			var data = new List<datatype>
			           {
				           CreateData(_keyEdgeLabel, dependency.QualityCondition.Name),
				           CreateData(_keyQualityCondition, dependency.QualityCondition.Name),
				           CreateData(_keyFromDataset, dependency.FromDatasetNode.Dataset.Name),
				           CreateData(_keyFromModel, dependency.FromDatasetNode.Dataset.Model.Name),
				           CreateData(_keyFromParameter, dependency.FromParameterName),
				           CreateData(_keyToDataset, dependency.ToDatasetNode.Dataset.Name),
				           CreateData(_keyToModel, dependency.ToDatasetNode.Dataset.Model.Name),
				           CreateData(_keyToParameter, dependency.ToParameterName),
				           CreateData(_keyIssueType, dependency.Element.AllowErrors
					                                     ? _issueTypeWarning
					                                     : _issueTypeError),
				           CreateData(_keyStopCondition, dependency.Element.StopOnError
					                                         ? "true"
					                                         : "false"),
				           CreateData(_keyTest, dependency.QualityCondition.TestDescriptor.Name)
			           };

			string categoryName = GetCategoryName(dependency);
			if (categoryName != null)
			{
				data.Add(CreateData(_keyCategory, categoryName));
			}

			if (StringUtils.IsNotEmpty(dependency.FromFilterExpression))
			{
				data.Add(CreateData(_keyFromFilterExpression, dependency.FromFilterExpression));
			}

			if (StringUtils.IsNotEmpty(dependency.ToFilterExpression))
			{
				data.Add(CreateData(_keyToFilterExpression, dependency.ToFilterExpression));
			}

			var result = new edgetype
			             {
				             data = data.ToArray(),
				             source = dependency.FromDatasetNode.NodeId,
				             target = dependency.ToDatasetNode.NodeId
			             };

			if (! dependency.Directed)
			{
				result.directed = false;
				result.directedSpecified = true;
			}

			return result;
		}

		[CanBeNull]
		private static string GetCategoryName([NotNull] DatasetDependency dependency)
		{
			DataQualityCategory category = dependency.QualityCondition.Category;

			return category?.GetQualifiedName();
		}

		[NotNull]
		private static nodetype CreateModelNode([NotNull] ModelNode modelNode)
		{
			DdxModel model = modelNode.Model;

			var data = new List<datatype>
			           {
				           CreateData(_keyNodeLabel, model.Name),
				           CreateData(_keyModelName, model.Name)
			           };

			var mlgraph =
				new graphtype
				{
					edgedefault = graphedgedefaulttype.directed,
					Items = modelNode.DatasetNodes.Select(node => CreateDatasetNode(node,
					                                                                useQualifiedLabel
					                                                                : false))
					                 .Cast<object>()
					                 .ToArray()
				};

			var items = new List<object>();
			items.AddRange(data.Cast<object>());
			items.Add(mlgraph);
			return new nodetype
			       {
				       id = modelNode.NodeId,
				       Items = items.ToArray()
			       };
		}

		[NotNull]
		private static nodetype CreateDatasetNode([NotNull] DatasetNode datasetNode,
		                                          bool useQualifiedLabel)
		{
			Dataset dataset = datasetNode.Dataset; // shorthand

			var node = new nodetype
			           {
				           id = datasetNode.NodeId
			           };

			string label = useQualifiedLabel
				               ? string.Format("{0}: {1}", dataset.Model.Name, dataset.Name)
				               : dataset.Name;
			string geometryTypeName = dataset.GeometryType == null
				                          ? string.Empty
				                          : dataset.GeometryType.Name;

			var data = new List<datatype>
			           {
				           CreateData(_keyNodeLabel, label),
				           CreateData(_keyGeometryType, geometryTypeName),
				           CreateData(_keyModelName, dataset.Model.Name)
			           };

			node.Items = data.Cast<object>().ToArray();

			return node;
		}

		[NotNull]
		private static datatype CreateData([NotNull] string key, [NotNull] string value)
		{
			return new datatype
			       {
				       key = key,
				       Text = new[] {value}
			       };
		}
	}
}
