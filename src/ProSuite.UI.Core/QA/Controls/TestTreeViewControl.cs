using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.Controls
{
	public partial class TestTreeViewControl : TreeView
	{
		private const string _none = "none";

		private const string _allowErrors = "allowErrors";
		private const string _continueOnErrors = "continueOnErrors";
		private const string _stopOnErrors = "stopOnErrors";

		private const string _imageKeyNoIssues = "noIssues";
		private const string _imageKeyWarning = "warning";
		private const string _imageKeyError = "error";

		private const string _fullSel = "fullSel";
		private const string _halfSel = "halfSel";
		private const string _emptySel = "emptySel";

		private const string _datasetCategory = "datasetCategory";
		private const string _datasetCategorySelected = "datasetCategorySelected";

		[CanBeNull] private ICollection<QualitySpecificationElement>
			_selectedSpecificationElements;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestTreeViewControl"/> class.
		/// </summary>
		public TestTreeViewControl()
		{
			InitializeComponent();

			ImageList imageList = DatasetTypeImageLookup.CreateImageList();

			ImageList.ImageCollection images = imageList.Images;

			images.Add(_none, new Bitmap(16, 16));

			images.Add(_allowErrors, TestTypeImages.TestTypeWarning);
			images.Add(_continueOnErrors, TestTypeImages.TestTypeError);
			images.Add(_stopOnErrors, TestTypeImages.TestTypeStop);

			images.Add(_imageKeyNoIssues, VerificationResultImages.OK);
			images.Add(_imageKeyWarning, VerificationResultImages.Warning);
			images.Add(_imageKeyError, VerificationResultImages.Error);

			images.Add(_fullSel, Resources.Full);
			images.Add(_halfSel, Resources.Half);
			images.Add(_emptySel, Resources.Empty);

			images.Add(_datasetCategory, Resources.DatasetCategory);
			images.Add(_datasetCategorySelected, Resources.DatasetCategorySelected);

			ImageList = imageList;

			DrawMode = TreeViewDrawMode.OwnerDrawText;
			DrawNode += TestTreeViewControl_DrawNode;
		}

		private void TestTreeViewControl_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			var element = e.Node.Tag as QualitySpecificationElement;
			QualityCondition qualityCondition = element?.QualityCondition;

			bool updated = qualityCondition?.Updated ?? false;
			e.Node.NodeFont = updated
				                  ? new Font(((TreeView) sender).Font, FontStyle.Italic)
				                  : ((TreeView) sender).Font;

			if (element != null &&
			    _selectedSpecificationElements?.Contains(element) == true &&
			    ! e.Node.IsSelected)
			{
				DrawNodeSelection(e);
			}
			else
			{
				e.DrawDefault = true;
			}
		}

		private static void DrawNodeSelection([NotNull] DrawTreeNodeEventArgs e)
		{
			e.DrawDefault = false;

			e.Graphics.FillRectangle(Brushes.LightSteelBlue, e.Bounds);

			e.Graphics.DrawRectangle(SystemPens.Control, e.Bounds);

			TextRenderer.DrawText(e.Graphics,
			                      e.Node.Text,
			                      e.Node.NodeFont,
			                      e.Node.Bounds,
			                      e.Node.ForeColor);
		}

		#endregion

		public void ShowPlain(
			[NotNull] IEnumerable<SpecificationDataset> specificationDatasets)
		{
			BeginUpdate();

			try
			{
				Nodes.Clear();
				foreach (SpecificationDataset specificationDataset in specificationDatasets)
				{
					AddNode(Nodes, specificationDataset);
				}

				if (Nodes.Count > 0)
				{
					SelectedNode = Nodes[0];
				}
			}
			finally
			{
				EndUpdate();
			}
		}

		public void SetSelectedElements(
			[NotNull] ICollection<QualitySpecificationElement> selected)
		{
			_selectedSpecificationElements = selected;
		}

		public void ShowLayer([NotNull] IList<SpecificationDataset> specificationDatasets)
		{
			ShowLayer(specificationDatasets,
			          new DatasetCategoryItemComparer(),
			          new DatasetComparer(),
			          new SpecificationDatasetComparer());
		}

		public void ShowLayer([NotNull] IList<SpecificationDataset> specificationDatasets,
		                      [NotNull] IComparer<DatasetCategoryItem> categorySorter,
		                      [NotNull] IComparer<Dataset> datasetSorter,
		                      [NotNull] IComparer<SpecificationDataset> specSorter)
		{
			BeginUpdate();

			try
			{
				Nodes.Clear();

				IDictionary categoryList = GetLayersPerCategory(specificationDatasets,
				                                                categorySorter,
				                                                datasetSorter, specSorter);

				BuildTree(Nodes, categoryList);
			}
			finally
			{
				EndUpdate();
			}
		}

		public void SetDatasetIcons(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node.Tag is DataQualityCategoryItem)
				{
					// TODO
					node.ImageKey = _datasetCategory;
					node.SelectedImageKey = _datasetCategorySelected;
				}
				else if (node.Tag is DatasetCategoryItem)
				{
					node.ImageKey = _datasetCategory;
					node.SelectedImageKey = _datasetCategorySelected;
				}
				else
				{
					var dataset = node.Tag as Dataset;
					if (dataset != null)
					{
						string key = DatasetTypeImageLookup.GetImageKey(dataset);

						node.ImageKey = key;
						node.SelectedImageKey = key;
					}
				}

				SetDatasetIcons(node.Nodes);
			}
		}

		public void ShowHierarchic(
			[NotNull] IList<SpecificationDataset> testInfoList,
			[NotNull] IComparer<DatasetCategoryItem> datasetCategoryComparer,
			[NotNull] IComparer<Dataset> datasetComparer,
			[NotNull] IComparer<SpecificationDataset> specificationDatasetComparer)
		{
			BeginUpdate();

			try
			{
				Nodes.Clear();

				IDictionary categoryList = GetHierarchic(testInfoList, datasetCategoryComparer,
				                                         datasetComparer,
				                                         specificationDatasetComparer);
				BuildTree(Nodes, categoryList);
			}
			finally
			{
				EndUpdate();
			}
		}

		public void ShowQualityConditionsByCategory(
			[NotNull] IEnumerable<SpecificationDataset> qualityConditionVerifications)
		{
			Assert.ArgumentNotNull(qualityConditionVerifications,
			                       nameof(qualityConditionVerifications));

			BeginUpdate();

			try
			{
				Nodes.Clear();

				IEnumerable<DataQualityCategoryItem> rootItems =
					GetDataQualityCategoryHierarchy(qualityConditionVerifications);

				BuildTree(Nodes, rootItems, new DataQualityCategoryComparer());
			}
			finally
			{
				EndUpdate();
			}
		}

		private static void BuildTree(
			[NotNull] TreeNodeCollection nodes,
			[NotNull] IEnumerable<DataQualityCategoryItem> categoryItems,
			[NotNull] IComparer<DataQualityCategory> comparer)
		{
			foreach (
				DataQualityCategoryItem categoryItem in
				categoryItems.OrderBy(c => c.Category, comparer))
			{
				TreeNode node = nodes.Add(categoryItem.Name);
				node.Tag = categoryItem;

				BuildTree(node.Nodes, categoryItem.SubCategories, comparer);

				foreach (SpecificationDataset specificationDataset in
				         categoryItem.SpecificationDatasets
				                     .OrderBy(v => v.QualityCondition?.Name))
				{
					AddNode(node.Nodes, specificationDataset);
				}
			}
		}

		[NotNull]
		private static IEnumerable<DataQualityCategoryItem> GetDataQualityCategoryHierarchy(
			[NotNull] IEnumerable<SpecificationDataset> qualityConditionVerifications)
		{
			var itemsByCategory =
				new Dictionary<DataQualityCategory, DataQualityCategoryItem>();
			var noCategoryItem = new DataQualityCategoryItem(null);

			foreach (
				SpecificationDataset specificationDataset in
				qualityConditionVerifications)
			{
				QualityCondition qualityCondition = specificationDataset.QualityCondition;
				if (qualityCondition?.Category == null)
				{
					noCategoryItem.SpecificationDatasets.Add(specificationDataset);
				}
				else
				{
					DataQualityCategoryItem item = GetDataQualityCategoryItem(itemsByCategory,
						qualityCondition
							.Category);

					item.SpecificationDatasets.Add(specificationDataset);
				}
			}

			var rootCategoryItems = new HashSet<DataQualityCategoryItem>();

			foreach (DataQualityCategoryItem item in itemsByCategory.Values)
			{
				if (item.IsRootCategory)
				{
					rootCategoryItems.Add(item);
				}
			}

			if (noCategoryItem.SpecificationDatasets.Count > 0)
			{
				rootCategoryItems.Add(noCategoryItem);
			}

			return rootCategoryItems;
		}

		[NotNull]
		private static DataQualityCategoryItem GetDataQualityCategoryItem(
			[NotNull] Dictionary<DataQualityCategory, DataQualityCategoryItem> itemsByCategory,
			[NotNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(itemsByCategory, nameof(itemsByCategory));
			Assert.ArgumentNotNull(category, nameof(category));

			DataQualityCategoryItem item;
			if (itemsByCategory.TryGetValue(category, out item))
			{
				return item;
			}

			item = new DataQualityCategoryItem(category);
			itemsByCategory.Add(category, item);

			if (category.ParentCategory != null)
			{
				DataQualityCategoryItem parentItem = GetDataQualityCategoryItem(itemsByCategory,
					category
						.ParentCategory);
				parentItem.SubCategories.Add(item);
			}

			return item;
		}

		[NotNull]
		private static IDictionary GetHierarchic(
			[NotNull] ICollection<SpecificationDataset> allSpecList,
			[NotNull] IComparer<DatasetCategoryItem> categoryComparer,
			[NotNull] IComparer<Dataset> datasetComparer,
			[NotNull] IComparer<SpecificationDataset> specificationDatasetComparer)
		{
			IDictionary categoryList = GetDatasetCategories(allSpecList, categoryComparer,
			                                                datasetComparer);

			foreach (SpecificationDataset specificationDataset in allSpecList)
			{
				QualityCondition condition = specificationDataset.QualityCondition;
				if (condition == null)
				{
					continue;
				}

				foreach (Dataset dataset in condition.GetDatasetParameterValues(true))
				{
					var datasetList =
						(IDictionary) categoryList[new DatasetCategoryItem(
							                           dataset.DatasetCategory)];

					if (datasetList.Contains(dataset))
					{
						continue;
					}

					var datasetDict = new Dictionary<object, object>();
					var parentDatasets = new SimpleSet<Dataset> {dataset};

					ICollection<SpecificationDataset> specificationDatasets =
						GetSpecifiations(allSpecList, parentDatasets);

					GetHierarchic(datasetDict, specificationDatasets,
					              parentDatasets, datasetComparer,
					              specificationDatasetComparer, 0);

					datasetList.Add(dataset, datasetDict);
				}
			}

			SortSpecifications(categoryList, specificationDatasetComparer);

			return categoryList;
		}

		private static void GetHierarchic(
			[NotNull] IDictionary parent,
			[NotNull] ICollection<SpecificationDataset> specificationDatasets,
			[NotNull] ICollection<Dataset> parentDatasets,
			[NotNull] IComparer<Dataset> datasetcoComparer,
			[NotNull] IComparer<SpecificationDataset> specComparer,
			int depth)
		{
			const int maxDepth = 2;
			if (depth > maxDepth)
			{
				return;
			}

			bool endDepth = depth == maxDepth;

			var siblingDatasets = new List<Dataset>();

			foreach (SpecificationDataset specificationDataset in specificationDatasets)
			{
				if (endDepth)
				{
					continue;
				}

				QualityCondition condition = specificationDataset.QualityCondition;
				if (condition == null)
				{
					continue;
				}

				foreach (Dataset dataset in condition.GetDatasetParameterValues(true))
				{
					if (! parentDatasets.Contains(dataset) &&
					    ! siblingDatasets.Contains(dataset))
					{
						siblingDatasets.Add(dataset);
					}
				}
			}

			siblingDatasets.Sort(datasetcoComparer);

			var showList = new List<SpecificationDataset>(specificationDatasets);

			foreach (Dataset dataset in siblingDatasets)
			{
				var subDict = new Dictionary<object, object>();
				parent.Add(dataset, subDict);

				var subDatasets = new SimpleSet<Dataset>(parentDatasets) {dataset};

				ICollection<SpecificationDataset> subSpecList =
					GetSpecifiations(specificationDatasets, subDatasets);

				foreach (SpecificationDataset subSpec in subSpecList)
				{
					showList.Remove(subSpec);
				}

				GetHierarchic(subDict, subSpecList, subDatasets, datasetcoComparer,
				              specComparer, depth + 1);
			}

			showList.Sort(specComparer);

			foreach (SpecificationDataset spec in showList)
			{
				parent.Add(spec, spec);
			}
		}

		[NotNull]
		private static ICollection<SpecificationDataset> GetSpecifiations(
			[NotNull] IEnumerable<SpecificationDataset> specList,
			[NotNull] ICollection<Dataset> parameterDatasets)
		{
			var result = new List<SpecificationDataset>();

			foreach (SpecificationDataset spec in specList)
			{
				QualityCondition condition = spec.QualityCondition;
				if (condition == null)
				{
					continue;
				}

				var mustDs = new HashSet<Dataset>(parameterDatasets);

				foreach (Dataset dataset in condition.GetDatasetParameterValues(true))
				{
					mustDs.Remove(dataset);
				}

				if (mustDs.Count == 0)
				{
					result.Add(spec);
				}
			}

			return result;
		}

		private static void SortSpecifications(
			[NotNull] IDictionary parents,
			[NotNull] IComparer<SpecificationDataset> specSorter)
		{
			foreach (DictionaryEntry pair in parents)
			{
				object child = pair.Value;
				if (child is IDictionary childDict)
				{
					SortSpecifications(childDict, specSorter);
				}
				else if (child is List<SpecificationDataset> specList)
				{
					specList.Sort(specSorter);
				}
			}
		}

		[NotNull]
		private static IDictionary GetLayersPerCategory(
			[NotNull] ICollection<SpecificationDataset> allSpecList,
			[NotNull] IComparer<DatasetCategoryItem> categorySorter,
			[NotNull] IComparer<Dataset> datasetSorter,
			[NotNull] IComparer<SpecificationDataset> specSorter)
		{
			IDictionary result = GetDatasetCategories(allSpecList, categorySorter,
			                                          datasetSorter);

			foreach (SpecificationDataset specificationDataset in allSpecList)
			{
				QualityCondition condition = specificationDataset.QualityCondition;
				if (condition == null)
				{
					continue;
				}

				// List specification only once per dataset ==>
				var uniqueDatasets = new Dictionary<string, Dataset>();

				foreach (Dataset dataset in condition.GetDatasetParameterValues(true))
				{
					if (uniqueDatasets.ContainsKey(dataset.Name))
					{
						continue;
					}

					uniqueDatasets.Add(dataset.Name, dataset);

					var datasetCategoryItem = new DatasetCategoryItem(dataset.DatasetCategory);
					var datasetList = (IDictionary) result[datasetCategoryItem];

					List<SpecificationDataset> specList;
					if (datasetList.Contains(dataset))
					{
						specList = (List<SpecificationDataset>) datasetList[dataset];
					}
					else
					{
						specList = new List<SpecificationDataset>();
						datasetList.Add(dataset, specList);
					}

					specList.Add(specificationDataset);
				}
			}

			SortSpecifications(result, specSorter);

			return result;
		}

		[NotNull]
		private static IDictionary GetDatasetCategories(
			[NotNull] IEnumerable<SpecificationDataset> specificationDatasets,
			[NotNull] IComparer<DatasetCategoryItem> categoryComparer,
			[NotNull] IComparer<Dataset> datasetComparer)
		{
			IDictionary result = new SortedDictionary<DatasetCategoryItem, object>(
				categoryComparer);

			foreach (SpecificationDataset specificationDataset in specificationDatasets)
			{
				QualityCondition condition = specificationDataset.QualityCondition;
				if (condition == null)
				{
					continue;
				}

				foreach (Dataset dataset in condition.GetDatasetParameterValues(true))
				{
					var datasetCategoryItem = new DatasetCategoryItem(dataset.DatasetCategory);

					if (result.Contains(datasetCategoryItem))
					{
						continue;
					}

					IDictionary datasetDict =
						new SortedDictionary<Dataset, object>(datasetComparer);
					result.Add(datasetCategoryItem, datasetDict);
				}
			}

			return result;
		}

		private static void BuildTree([NotNull] TreeNodeCollection nodes,
		                              [NotNull] IDictionary parents)
		{
			foreach (DictionaryEntry pair in parents)
			{
				object parent = pair.Key;
				TreeNodeCollection childNodes;

				if (parent is DatasetCategoryItem datasetCategoryItem)
				{
					if (parents.Count == 1 && datasetCategoryItem.IsNull)
					{
						childNodes = nodes;
					}
					else
					{
						TreeNode parentNode = nodes.Add(datasetCategoryItem.Name);
						parentNode.Tag = datasetCategoryItem;
						childNodes = parentNode.Nodes;
					}
				}
				else if (parent is Dataset dataset)
				{
					TreeNode parentNode = nodes.Add(dataset.AliasName);
					parentNode.Tag = dataset;
					childNodes = parentNode.Nodes;
				}
				else if (parent is SpecificationDataset specificationDataset)
				{
					TreeNode parentNode = AddNode(nodes, specificationDataset);
					childNodes = parentNode.Nodes;
				}
				else
				{
					TreeNode parentNode = nodes.Add(parent.ToString());
					childNodes = parentNode.Nodes;
				}

				object child = pair.Value;
				if (child is IDictionary childDict)
				{
					BuildTree(childNodes, childDict);
				}
				else if (child is IList subList)
				{
					foreach (object sub in subList)
					{
						if (sub is IDictionary dictionary)
						{
							BuildTree(childNodes, dictionary);
						}
						else if (sub is SpecificationDataset specDataset)
						{
							AddNode(childNodes, specDataset);
						}
						else
						{
							throw new NotImplementedException(
								"Unhandled child type " + sub.GetType());
						}
					}
				}
				else if (pair.Key == pair.Value) { }
				else
				{
					throw new NotImplementedException("Unhandled type " + child.GetType());
				}
			}
		}

		[NotNull]
		private static TreeNode AddNode([NotNull] TreeNodeCollection nodes,
		                                [NotNull] SpecificationDataset specificationDataset)
		{
			Assert.NotNull(specificationDataset.QualityCondition);

			TreeNode node = nodes.Add(specificationDataset.QualityCondition.Name);

			QualitySpecificationElement element =
				specificationDataset.QualitySpecificationElement;

			if (element != null)
			{
				node.Tag = element;
				node.Checked = element.Enabled;

				if (element.AllowErrors)
				{
					node.ImageKey = _allowErrors;
					node.SelectedImageKey = _allowErrors;
				}
				else if (element.StopOnError == false)
				{
					node.ImageKey = _continueOnErrors;
					node.SelectedImageKey = _continueOnErrors;
				}
				else
				{
					node.ImageKey = _stopOnErrors;
					node.SelectedImageKey = _stopOnErrors;
				}
			}

			QualityConditionVerification verification =
				specificationDataset.QualityConditionVerification;

			if (verification != null)
			{
				ConfigureVerificationNode(node, verification);
			}

			return node;
		}

		private static void ConfigureVerificationNode(
			[NotNull] TreeNode node,
			[NotNull] QualityConditionVerification verification)
		{
			node.Tag = verification;

			if (verification.ErrorCount == 0)
			{
				node.ImageKey = _imageKeyNoIssues;
				node.SelectedImageKey = _imageKeyNoIssues;
			}
			else if (verification.AllowErrors)
			{
				node.ImageKey = _imageKeyWarning;
				node.SelectedImageKey = _imageKeyWarning;
			}
			else
			{
				node.ImageKey = _imageKeyError;
				node.SelectedImageKey = _imageKeyError;
			}
		}

		internal void UpdateChecked()
		{
			foreach (TreeNode node in Nodes)
			{
				UpdateChecked(node);
			}
		}

		private static void UpdateChecked([NotNull] TreeNode parent)
		{
			Assert.ArgumentNotNull(parent, nameof(parent));

			var elem = parent.Tag as QualitySpecificationElement;
			if (elem != null && elem.Enabled != parent.Checked)
			{
				parent.Checked = elem.Enabled;
			}

			foreach (TreeNode node in parent.Nodes)
			{
				UpdateChecked(node);
			}
		}

		public void SetState([NotNull] TreeNode node, CheckState state)
		{
			switch (state)
			{
				case CheckState.Checked:
					node.Checked = true;
					node.ImageKey = _fullSel;
					node.SelectedImageKey = _fullSel;
					break;

				case CheckState.Indeterminate:
					node.Checked = true;
					node.ImageKey = _halfSel;
					node.SelectedImageKey = _halfSel;
					break;

				case CheckState.Unchecked:
					node.Checked = false;
					node.ImageKey = _emptySel;
					node.SelectedImageKey = _emptySel;
					break;
			}
		}
	}
}
