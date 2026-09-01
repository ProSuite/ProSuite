using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	/// <summary>
	/// Tracks, per thread, the tree view whose nodes are currently being removed and
	/// the tree view that is currently changing its selection.
	/// <para>
	/// The native tree view raises its selection notifications (TVN_SELCHANGING and
	/// TVN_SELCHANGED, surfaced as BeforeSelect and AfterSelect) synchronously while
	/// it deletes items, because deleting the selected item forces the selection to
	/// move. Acting on such a notification is not only wrong - the user did not select
	/// anything - but fatal: handling the pending changes may delete the current item
	/// and thus remove another tree node, which re-enters the native control while it
	/// is still traversing its own item list. This corrupts the control and ends the
	/// process with an ExecutionEngineException, which cannot be caught: no error
	/// message, no log entry, no chance to save.
	/// </para>
	/// <para>
	/// The navigation control therefore ignores selection events while nodes are being
	/// removed, which prevents the nested removal in the first place. Node removal
	/// additionally checks that it is not already within such a scope, so that any
	/// other path leading to a nested removal fails with an ordinary exception instead
	/// of killing the process.
	/// </para>
	/// <para>
	/// Since the selection events are ignored, a node that holds the selection moves
	/// it to its parent before removing itself - but only if the selection is not
	/// already being changed, in which case the navigation control is about to select
	/// the node the user picked anyway.
	/// </para>
	/// </summary>
	internal static class TreeViewUpdateScope
	{
		[ThreadStatic] [CanBeNull] private static TreeView _removingNodesFrom;
		[ThreadStatic] [CanBeNull] private static TreeView _changingSelectionOf;

		/// <summary>
		/// Marks the specified tree view as removing nodes, until the returned object
		/// is disposed. A null tree view (a node that is not attached to a tree view)
		/// yields a scope that has no effect: such a removal does not reach the native
		/// control and raises no events.
		/// </summary>
		[NotNull]
		public static IDisposable EnterNodeRemoval([CanBeNull] TreeView treeView)
		{
			return new Scope(treeView, Slot.NodeRemoval);
		}

		/// <summary>
		/// Marks the specified tree view as changing its selection, until the returned
		/// object is disposed.
		/// </summary>
		[NotNull]
		public static IDisposable EnterSelectionChange([CanBeNull] TreeView treeView)
		{
			return new Scope(treeView, Slot.SelectionChange);
		}

		public static bool IsRemovingNodes([CanBeNull] TreeView treeView)
		{
			return treeView != null && ReferenceEquals(treeView, _removingNodesFrom);
		}

		public static bool IsChangingSelection([CanBeNull] TreeView treeView)
		{
			return treeView != null && ReferenceEquals(treeView, _changingSelectionOf);
		}

		/// <summary>
		/// Ensures that tree nodes may be removed from the specified tree view, i.e.
		/// that it is not already removing nodes.
		/// </summary>
		/// <param name="treeView">The tree view the nodes belong to, or null if they
		/// are not attached to a tree view.</param>
		/// <param name="nodeText">The text of the node to be removed, for the error
		/// message.</param>
		public static void EnsureNodeRemovalAllowed([CanBeNull] TreeView treeView,
		                                            [CanBeNull] string nodeText)
		{
			if (! IsRemovingNodes(treeView))
			{
				return;
			}

			throw new InvalidOperationException(
				$"Cannot remove the tree node '{nodeText}' while the navigation tree " +
				"is already removing nodes; this would terminate the process.");
		}

		#region Nested types

		private enum Slot
		{
			NodeRemoval,
			SelectionChange
		}

		private class Scope : IDisposable
		{
			private readonly Slot _slot;
			private readonly bool _entered;
			[CanBeNull] private readonly TreeView _previous;

			public Scope([CanBeNull] TreeView treeView, Slot slot)
			{
				_slot = slot;

				if (treeView == null)
				{
					return;
				}

				_entered = true;
				_previous = Get(slot);

				Set(slot, treeView);
			}

			public void Dispose()
			{
				if (_entered)
				{
					Set(_slot, _previous);
				}
			}

			[CanBeNull]
			private static TreeView Get(Slot slot)
			{
				return slot == Slot.NodeRemoval
					       ? _removingNodesFrom
					       : _changingSelectionOf;
			}

			private static void Set(Slot slot, [CanBeNull] TreeView treeView)
			{
				if (slot == Slot.NodeRemoval)
				{
					_removingNodesFrom = treeView;
				}
				else
				{
					_changingSelectionOf = treeView;
				}
			}
		}

		#endregion
	}
}
