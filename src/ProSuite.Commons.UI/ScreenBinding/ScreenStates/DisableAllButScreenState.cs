namespace ProSuite.Commons.UI.ScreenBinding.ScreenStates
{
	public class DisableAllButScreenState : IScreenState
	{
		private readonly ControlSet _internalSet = new ControlSet();

		#region IScreenState Members

		public bool IsControlEnabled(object control)
		{
			return _internalSet.Contains(control);
		}

		#endregion

		public void Enable(params object[] controls)
		{
			_internalSet.AddRange(controls);
		}
	}
}
