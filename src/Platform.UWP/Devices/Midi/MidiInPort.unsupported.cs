#if IS_UNIT_TESTS || __CROSSRUNTIME__
namespace Windows.Devices.Midi
{
	public partial class MidiInPort
	{
		/// <summary>
		/// Remove public parameterless constructor,
		/// needed for NET Standard reference check.
		/// </summary>
		private MidiInPort()
		{
		}
	}
}
#endif
