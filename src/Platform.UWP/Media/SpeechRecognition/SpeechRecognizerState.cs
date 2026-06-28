namespace Windows.Media.SpeechRecognition
{
	public enum SpeechRecognizerState
	{
		Idle,

		Capturing,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		Processing,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		SoundStarted,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		SoundEnded,

		SpeechDetected,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		Paused
	}
}
