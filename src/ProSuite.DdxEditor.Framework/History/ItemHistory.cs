using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.History
{
	public class ItemHistory
	{
		[NotNull] private readonly List<Item> _nodes = new List<Item>();
		private int _currentNodeIndex = -1;

		public void Add([NotNull] Item item)
		{
			if (_nodes.Count == 0)
			{
				// the history list is currently empty
				AddNode(item);
				_currentNodeIndex = 0;
			}
			else
			{
				// there is at least one existing node
				Item currentNode = _currentNodeIndex < 0
					                   ? null
					                   : _nodes[_currentNodeIndex];

				if (Equals(currentNode, item))
				{
					// ignore equal node
				}
				else
				{
					// if the new node is inserted before the last node, remove
					// existing nodes after the insertion index
					if (_currentNodeIndex < _nodes.Count - 1)
					{
						RemoveNodes(_currentNodeIndex + 1,
						            _nodes.Count - _currentNodeIndex - 1);
					}

					// add the new node
					AddNode(item);
					_currentNodeIndex = _nodes.Count - 1;
				}
			}
		}

		public bool CanGoBack => _currentNodeIndex > 0;

		public bool CanGoForward => _currentNodeIndex < _nodes.Count - 1;

		[NotNull]
		public Item GoBack()
		{
			_currentNodeIndex--;

			return _nodes[_currentNodeIndex];
		}

		[NotNull]
		public Item GoForward()
		{
			_currentNodeIndex++;

			return _nodes[_currentNodeIndex];
		}

		#region Non-public members

		private void AddNode([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			item.Deleted += item_Deleted;
			_nodes.Add(item);
		}

		private void RemoveNodes(int startIndex, int count)
		{
			foreach (Item itemToDelete in _nodes.GetRange(startIndex, count))
			{
				itemToDelete.Deleted -= item_Deleted;
			}

			_nodes.RemoveRange(startIndex, count);
		}

		private void RemoveNode([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			item.Deleted -= item_Deleted;
			_nodes.Remove(item);
		}

		private void item_Deleted(object sender, EventArgs e)
		{
			var item = sender as Item;
			if (item == null)
			{
				return;
			}

			int index = _nodes.IndexOf(item);
			if (index < 0)
			{
				return;
			}

			if (index <= _currentNodeIndex)
			{
				_currentNodeIndex--;
			}

			RemoveNode(item);
		}

		#endregion
	}
}
