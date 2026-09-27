namespace Windows.Devices.Input
{
#pragma warning disable CA1815 // WinRT struct surface: adding Equals/operators would change the public API (the generated file was exempt as auto-generated)
	// Hand-written (not a generated stub): MouseDevice.MouseMoved reports real deltas through this struct, so it must not
	// carry the NotImplemented marker the API sync generator adds to types that exist only in Generated/.

	/// <summary>
	/// Identifies the change in screen location of the mouse pointer, in mickeys, relative to the last mouse event.
	/// </summary>
	public partial struct MouseDelta
	{
		/// <summary>The x-coordinate change.</summary>
		public int X;

		/// <summary>The y-coordinate change.</summary>
		public int Y;
	}
#pragma warning restore CA1815
}
