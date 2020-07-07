namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class NormalScreenState : IScreenState
	{
		private readonly ControlSet _all;
		private readonly ControlSet _internalSet = new ControlSet();

		public NormalScreenState(ControlSet all)
		{
			_all = all;
			_internalSet = new ControlSet();
		}

		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			if (IsPartOfThisState(control))
			{
				return true;
			}

			return ! _all.Contains(control);
		}

		#endregion

		private bool IsPartOfThisState(object control)
		{
			return _internalSet.Contains(control);
		}

		public void Enable(params object[] controls)
		{
			_internalSet.AddRange(controls);
		}
	}
}
