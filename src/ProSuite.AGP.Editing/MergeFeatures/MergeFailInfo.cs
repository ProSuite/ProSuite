using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public struct MergeFailInfo
	{
		private Row _lineObject1;
		private Row _lineObject2;
		private Row _nodeObject;

		private string _reason;

		public string FirstLineDesc => GdbObjectUtils.GetDisplayValue(_lineObject1);

		public string SecondLineDesc => GdbObjectUtils.GetDisplayValue(_lineObject2);

		public string Reason
		{
			get => _reason;
			set => _reason = value;
		}

		public Row FirstLineObject
		{
			get => _lineObject1;
			set
			{
				Assert.NotNull(value);
				_lineObject1 = value;
			}
		}

		public Row SecondeLineObject
		{
			get => _lineObject2;
			set
			{
				Assert.NotNull(value);
				_lineObject2 = value;
			}
		}

		public Row NodeObject
		{
			get => _nodeObject;
			set => _nodeObject = value;
		}

		public bool HasNodeObject => _nodeObject != null;

		public bool HasFailings => HasNodeObject || _lineObject1 != null || _lineObject2 != null;

		public bool RemoveNodeLink(int objectId)
		{
			if (_nodeObject != null && _nodeObject.GetObjectID() == objectId)
			{
				_nodeObject = null;
				return true;
			}

			return false;
		}
	}
}
