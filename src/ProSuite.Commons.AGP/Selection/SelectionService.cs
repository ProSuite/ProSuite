using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Selection
{
	public class SelectionService
	{
		private readonly Dictionary<MapMember, List<long>> _newSelection = null;
		private readonly MapView _mapView;

		public SelectionService(MapView mapView, SelectionCombinationMethod selectionMethod)
		{
			_mapView = mapView;
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				_newSelection = new Dictionary<MapMember, List<long>>();
			}
			else
			{
				_newSelection = _mapView.Map.GetSelection().ToDictionary();
			}
		}

		public void Select()
		{
			_newSelection.RemoveAll(t => t.Value.Count < 1);

			SelectionSet regular = SelectionSet.FromDictionary(_newSelection);
			_mapView?.Map.SetSelection(regular);
		}

		public void Add(BasicFeatureLayer layer, SelectionCombinationMethod selectionMethod,
		                List<long> oids)
		{
			if (selectionMethod == SelectionCombinationMethod.New)
			{
				_newSelection[layer] = oids;
			}
			else if (selectionMethod == SelectionCombinationMethod.XOR)
			{
				ArcGIS.Core.Data.Selection selection = layer.GetSelection();
				List<long> currentlySelectedIDs = selection.GetObjectIDs().ToList();

				_newSelection[layer] = GetXorList(currentlySelectedIDs, oids);
			}
			else if (selectionMethod == SelectionCombinationMethod.Add)
			{
				ArcGIS.Core.Data.Selection selection = layer.GetSelection();
				List<long> currentlySelectedIDs = selection.GetObjectIDs().ToList();

				_newSelection[layer] = GetCombinedList(currentlySelectedIDs, oids);
			}
			else if (selectionMethod == SelectionCombinationMethod.And)
			{
				ArcGIS.Core.Data.Selection selection = layer.GetSelection();
				List<long> currentlySelectedIDs = selection.GetObjectIDs().ToList();

				_newSelection[layer] = GetAndList(currentlySelectedIDs, oids);
			}
		}

		private List<long> GetXorList(List<long> currentlySelectedIDs, List<long> newOids)
		{
			List<long> xorList = new List<long>();

			if (currentlySelectedIDs.Count > 0)
			{
				xorList = currentlySelectedIDs.Except(newOids).ToList();

				foreach (long oid in newOids)
				{
					if (! currentlySelectedIDs.Contains(oid))
					{
						xorList.Add(oid);
					}
				}
			}
			else
			{
				xorList = newOids;
			}

			return xorList;
		}

		private List<long> GetCombinedList(List<long> currentlySelectedIDs, List<long> newOids)
		{
			//IEnumerable<long> concat = currentlySelectedIDs.Concat(oids);
			//List<long> distinct2 = new HashSet<long>(concat).ToList();

			var toAdd = newOids.Except(currentlySelectedIDs).Distinct().ToArray();
			List<long> distinct = currentlySelectedIDs.Concat(toAdd).ToList();

			return distinct;
		}

		private List<long> GetAndList(List<long> currentlySelectedIDs, List<long> newOids)
		{
			List<long> andList = new List<long>();
			if (currentlySelectedIDs.Count > 0 && newOids.Count > 0)
			{
				andList = currentlySelectedIDs.Intersect(newOids).ToList();
			}

			return andList;
		}
	}

	public static class DictionaryExtensions
	{
		public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict,
		                                           Func<KeyValuePair<TKey, TValue>, bool> removeIf)
		{
			foreach (var item in dict.Where(removeIf).ToList())
			{
				dict.Remove(item.Key);
			}
		}
	}
}
