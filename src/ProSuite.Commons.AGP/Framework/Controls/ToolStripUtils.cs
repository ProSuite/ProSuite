using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework.Controls
{
	public static class ToolStripUtils
	{
		public static void AddWrappers([NotNull] ToolStrip toolStrip,
		                               params string[] damlIds)
		{
			AddWrappers(toolStrip, ToolStripItemAlignment.Left, damlIds);
		}

		public static void AddWrappers([NotNull] ToolStrip toolStrip,
		                               ToolStripItemAlignment alignment,
		                               [NotNull] params string[] damlIds)
		{
			AddWrappers(toolStrip, alignment, null, damlIds);
		}

		public static void AddWrappers([NotNull] ToolStrip toolStrip,
		                               [CanBeNull] Action onClick,
		                               [NotNull] params string[] damlIds)
		{
			AddWrappers(toolStrip, ToolStripItemAlignment.Left, onClick, damlIds);
		}

		public static void AddWrappers([NotNull] ToolStrip toolStrip,
		                               ToolStripItemAlignment alignment,
		                               [CanBeNull] Action onClick,
		                               [NotNull] params string[] damlIds)
		{
			foreach (string damlId in damlIds)
			{
				AddWrapper(toolStrip, damlId, alignment, onClick);
			}
		}

		[NotNull]
		public static ToolStripCommandWrapperButton AddWrapper(
			[NotNull] ToolStrip toolStrip,
			[NotNull] string damlId,
			ToolStripItemAlignment alignment = ToolStripItemAlignment.Left,
			[CanBeNull] Action onClick = null)
		{
			Assert.ArgumentNotNull(toolStrip, nameof(toolStrip));

			ToolStripCommandWrapperButton wrapper =
				CreateCommandWrapper(damlId, alignment, onClick);

			toolStrip.Items.Add(wrapper);

			return wrapper;
		}

		public static void InsertWrappers([NotNull] ToolStrip toolStrip,
		                                  int index,
		                                  params string[] damlIds)
		{
			InsertWrappers(toolStrip, ToolStripItemAlignment.Left, index, damlIds);
		}

		public static void InsertWrappers([NotNull] ToolStrip toolStrip,
		                                  ToolStripItemAlignment alignment,
		                                  int index,
		                                  params string[] damlIds)
		{
			InsertWrappers(toolStrip, alignment, null, index, damlIds);
		}

		public static void InsertWrappers([NotNull] ToolStrip toolStrip,
		                                  ToolStripItemAlignment alignment,
		                                  [CanBeNull] Action onClick,
		                                  int index,
		                                  params string[] damlIds)
		{
			int nextIndex = index;
			foreach (string damlId in damlIds)
			{
				InsertWrapper(toolStrip, damlId, nextIndex, alignment, onClick);
				nextIndex++;
			}
		}

		public static ToolStripCommandWrapperButton InsertWrapper(
			[NotNull] ToolStrip toolStrip,
			[NotNull] string damlId,
			int index,
			ToolStripItemAlignment alignment = ToolStripItemAlignment.Left)
		{
			return InsertWrapper(toolStrip, damlId, index, alignment, null);
		}

		public static ToolStripCommandWrapperButton InsertWrapper(
			[NotNull] ToolStrip toolStrip,
			[NotNull] string damlId,
			int index,
			ToolStripItemAlignment alignment, [CanBeNull] Action onClick)
		{
			Assert.ArgumentNotNull(toolStrip, nameof(toolStrip));

			ToolStripCommandWrapperButton wrapper =
				CreateCommandWrapper(damlId, alignment, onClick);

			toolStrip.Items.Insert(index, wrapper);

			return wrapper;
		}

		public static void RefreshWrappers([NotNull] ToolStrip toolStrip)
		{
			Assert.ArgumentNotNull(toolStrip, nameof(toolStrip));

			if (toolStrip.IsDisposed)
			{
				return;
			}

			foreach (ToolStripItem item in toolStrip.Items)
			{
				if (item is ICommandWrapper)
				{
					((ICommandWrapper) item).UpdateAppearance();
				}
			}
		}

		public static void AddSeparator(ToolStrip toolStrip,
		                                ToolStripItemAlignment alignment =
			                                ToolStripItemAlignment.Left)
		{
			Assert.ArgumentNotNull(toolStrip, nameof(toolStrip));

			ToolStripItem item = new ToolStripSeparator { Alignment = alignment };

			toolStrip.Items.Add(item);
		}

		public static void InsertSeparator(ToolStrip toolStrip, int index,
		                                   ToolStripItemAlignment alignment =
			                                   ToolStripItemAlignment.Left)
		{
			Assert.ArgumentNotNull(toolStrip, nameof(toolStrip));

			ToolStripItem item = new ToolStripSeparator { Alignment = alignment };

			toolStrip.Items.Insert(index, item);
		}

		[NotNull]
		private static ToolStripCommandWrapperButton CreateCommandWrapper(
			[NotNull] string damlId,
			ToolStripItemAlignment alignment,
			[CanBeNull] Action onClick)
		{
			Assert.ArgumentNotNullOrEmpty(damlId, nameof(damlId));

			var result = new ToolStripCommandWrapperButton(damlId)
			             {
				             Alignment = alignment
			             };

			if (onClick != null)
			{
				result.Click += delegate { onClick(); };
			}
			else
			{
				result.Click += wrapper_Click;
			}

			return result;
		}

		private static void wrapper_Click(object sender, EventArgs e)
		{
			var item = sender as ToolStripCommandWrapperButton;

			ToolStrip toolStrip = item?.Owner;

			if (toolStrip == null)
			{
				return;
			}

			RefreshWrappers(toolStrip);
		}
	}
}
