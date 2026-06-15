using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public partial class VerifiedConditionsHierarchyControl : UserControl
	{
		private const int _leftMargin = 8;
		private const double _minimumTestExecutionTimeSeconds = 0.1;
		private const int _rightMargin = 8;

		private List<DrawInfo> _drawInfo;
		private bool _rendering;

		private QualityVerification _verification;

		// TODO pass via properties
		private readonly Color _loadColor = Color.Blue;

		private readonly Color _testColor = Color.Red;

		public event EventHandler SelectionChanged;

		public VerifiedConditionsHierarchyControl()
		{
			InitializeComponent();
		}

		public int SplitterDistance
		{
			get { return _splitContainerConditions.SplitterDistance; }
			set { _splitContainerConditions.SplitterDistance = value; }
		}

		[CanBeNull]
		public TreeNode SelectedNode => _treeViewConditions.SelectedNode;

		public void RenderLayerView(
			[NotNull] QualityVerification qualityVerifiation,
			[NotNull] Predicate<QualityConditionVerification> includeConditionVerification)
		{
			bool oldRendering = _rendering;

			_rendering = true;

			try
			{
				_verification = qualityVerifiation;

				_treeViewConditions.ShowLayer(GetSpecificationDatasets(
					                              qualityVerifiation, includeConditionVerification),
				                              new DatasetCategoryItemComparer(),
				                              new DatasetComparer(),
				                              new SpecificationDatasetComparer());
				_treeViewConditions.SetDatasetIcons(_treeViewConditions.Nodes);
				_panelExecuteInfo.Invalidate();
			}
			finally
			{
				_rendering = oldRendering;
			}
		}

		public void RenderHierarchicView(
			[NotNull] QualityVerification qualityVerifiation,
			[NotNull] Predicate<QualityConditionVerification> includeConditionVerification)
		{
			bool oldRendering = _rendering;

			_rendering = true;

			try
			{
				_verification = qualityVerifiation;

				_treeViewConditions.ShowHierarchic(GetSpecificationDatasets(
					                                   qualityVerifiation,
					                                   includeConditionVerification),
				                                   new DatasetCategoryItemComparer(),
				                                   new DatasetComparer(),
				                                   new SpecificationDatasetComparer());
				_treeViewConditions.SetDatasetIcons(_treeViewConditions.Nodes);
				_panelExecuteInfo.Invalidate();
			}
			finally
			{
				_rendering = oldRendering;
			}
		}

		public void RenderConditionsByCategoryView(
			[NotNull] QualityVerification verification,
			[NotNull] Func<QualityConditionVerification, bool> includeConditionVerification)
		{
			bool oldRendering = _rendering;

			_rendering = true;

			try
			{
				_verification = verification;

				_treeViewConditions.ShowQualityConditionsByCategory(
					verification.ConditionVerifications.Where(includeConditionVerification)
					            .Select(x => new SpecificationDataset(x)));

				_treeViewConditions.SetDatasetIcons(_treeViewConditions.Nodes);
				_panelExecuteInfo.Invalidate();
			}
			finally
			{
				_rendering = oldRendering;
			}
		}

		[NotNull]
		private static IList<SpecificationDataset> GetSpecificationDatasets(
			[NotNull] QualityVerification qualityVerifiation,
			[NotNull] Predicate<QualityConditionVerification> includeConditionVerification)
		{
			var result = new List<SpecificationDataset>(
				qualityVerifiation.ConditionVerifications.Count);

			foreach (QualityConditionVerification conditionVerification in
			         qualityVerifiation.ConditionVerifications)
			{
				if (includeConditionVerification(conditionVerification))
				{
					result.Add(new SpecificationDataset(conditionVerification)
					           {
						           UseAliasDatasetName = true
					           });
				}
			}

			return result;
		}

		private void _panelExecuteInfo_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.Clear(_panelExecuteInfo.BackColor);

			if (_verification == null)
			{
				return;
			}

			_drawInfo = new List<DrawInfo>();
			PaintInfo(_treeViewConditions.Nodes, e, _drawInfo, _verification);
		}

		private void _treeViewConditions_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (_rendering)
			{
				return;
			}

			OnSelectionChanged();
		}

		protected virtual void OnSelectionChanged()
		{
			EventHandler handler = SelectionChanged;
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		private void _treeViewConditions_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			e.DrawDefault = true;

			if (_drawInfo == null)
			{
				return;
			}

			if (_verification == null)
			{
				return;
			}

			var compareInfo = new List<DrawInfo>();
			PaintInfo(_treeViewConditions.Nodes, null, compareInfo, _verification);
			if (! Equals(_drawInfo, compareInfo))
			{
				_panelExecuteInfo.Invalidate();
				_panelExecuteInfo.Refresh();
			}
		}

		private void PaintInfo([NotNull] TreeNodeCollection treeNodes,
		                       [CanBeNull] PaintEventArgs e,
		                       [NotNull] ICollection<DrawInfo> drawInfo,
		                       [NotNull] QualityVerification verification)
		{
			TimeSpan timeSpan = verification.EndDate - verification.StartDate;

			// factor was reaching infinity on verification with exactly 0 duration... 
			//      ... this is caused, by QaGraphicConflict test (https://issuetracker02.eggits.net/browse/GEN-575 submitted)
			double factor;
			if (Math.Abs(timeSpan.TotalSeconds) < double.Epsilon)
			{
				factor = (_panelExecuteInfo.Width - _leftMargin - _rightMargin) /
				         _minimumTestExecutionTimeSeconds;
			}
			else
			{
				factor = (_panelExecuteInfo.Width - _leftMargin - _rightMargin) /
				         timeSpan.TotalSeconds;
			}

			foreach (TreeNode node in treeNodes)
			{
				if (node.IsExpanded)
				{
					PaintInfo(node.Nodes, e, drawInfo, verification);
				}

				if (node.IsVisible == false)
				{
					continue;
				}

				drawInfo.Add(new DrawInfo(node.Tag,
				                          new Point(node.Bounds.X,
				                                    node.Bounds.Y)));

				if (e == null)
				{
					continue;
				}

				int top = node.Bounds.Top + 1;
				int height = node.Bounds.Height - 2;

				if (node.Tag is QualityConditionVerification)
				{
					var conditionVerification = (QualityConditionVerification) node.Tag;

					double startTime = 0;

					// load times
					startTime = Draw(top, height, _leftMargin,
					                 startTime, conditionVerification.LoadTime(verification),
					                 e.Graphics, _loadColor, factor);

					// execute times
					startTime = Draw(top, height, _leftMargin,
					                 startTime, conditionVerification.ExecuteTime,
					                 e.Graphics, _testColor, factor);

					startTime = Draw(top, height, _leftMargin,
					                 startTime, conditionVerification.RowExecuteTime,
					                 e.Graphics, _testColor, factor);

					Draw(top, height, _leftMargin,
					     startTime, conditionVerification.TileExecuteTime,
					     e.Graphics, _testColor, factor);
				}
				else if (node.Tag is Dataset)
				{
					var dataset = (Dataset) node.Tag;
					QualityVerificationDataset verificationDataset =
						verification.GetVerificationDataset(dataset);

					if (verificationDataset != null)
					{
						double startTime = 0;
						startTime = Draw(top, height, _leftMargin,
						                 startTime, verificationDataset.LoadTime,
						                 e.Graphics, _loadColor, factor);

						// load times
						startTime = Draw(top, height, _leftMargin,
						                 startTime, GetLoadTime(node, verification),
						                 e.Graphics, _loadColor, factor);

						// show child times
						Draw(top, height, _leftMargin,
						     startTime, GetExecutionTime(node),
						     e.Graphics, _testColor, factor);
					}
				}
			}
		}

		private static double Draw(int top, int height, int leftMargin,
		                           double startTime, double dTime,
		                           [NotNull] Graphics graphics,
		                           Color color, double factor)
		{
			int left = leftMargin + (int) (startTime * factor);
			int width = leftMargin + (int) ((startTime + dTime) * factor) - left;

			var rect = new Rectangle(left, top + 1, width, height - 2);

			using (Brush brush = new SolidBrush(color))
			{
				graphics.FillRectangle(brush, rect);
			}

			return startTime + dTime;
		}

		private double GetLoadTime([NotNull] TreeNode node,
		                           [NotNull] QualityVerification verification)
		{
			return GetLoadTime(node, verification, new HashSet<Dataset>());
		}

		private static double GetLoadTime([NotNull] TreeNode node,
		                                  [NotNull] QualityVerification verification,
		                                  [NotNull] ICollection<Dataset> included)
		{
			double loadTime = 0;

			foreach (TreeNode child in node.Nodes)
			{
				var dataset = node.Tag as Dataset;

				if (dataset != null && ! included.Contains(dataset))
				{
					QualityVerificationDataset verificationDataset =
						verification.GetVerificationDataset(dataset);

					if (verificationDataset != null)
					{
						loadTime += verificationDataset.LoadTime;
					}

					included.Add(dataset);
				}

				loadTime += GetLoadTime(child, verification, included);
			}

			return loadTime;
		}

		private static double GetExecutionTime([NotNull] TreeNode node)
		{
			double executionTime = 0;
			foreach (TreeNode child in node.Nodes)
			{
				var conditionVerification = child.Tag as QualityConditionVerification;
				if (conditionVerification != null)
				{
					executionTime += conditionVerification.TotalExecuteTime;
				}

				executionTime += GetExecutionTime(child);
			}

			return executionTime;
		}

		#region nested classes

		private class DrawInfo
		{
			private readonly object _tag;
			private Point _location;

			public DrawInfo(object tag, Point location)
			{
				_tag = tag;
				_location = location;
			}

			public override bool Equals(object other)
			{
				var o = other as DrawInfo;
				if (o == null)
				{
					return false;
				}

				return
					_tag == o._tag &&
					_location.X == o._location.X &&
					_location.Y == o._location.Y;
			}

			public override int GetHashCode()
			{
				return _tag.GetHashCode();
			}
		}

		#endregion

		private static bool Equals(IList<DrawInfo> x, IList<DrawInfo> y)
		{
			if (x == null || y == null)
			{
				return false;
			}

			int n = x.Count;
			if (n != y.Count)
			{
				return false;
			}

			for (var i = 0; i < n; i++)
			{
				if (x[i].Equals(y[i]) == false)
				{
					return false;
				}
			}

			return true;
		}

		private void VerifiedConditionsHierarchyControl_Load(object sender, EventArgs e)
		{
			_splitContainerConditions.Panel2MinSize = 232;
		}
	}
}
