namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizerUIOptions
	{
		public bool ShowConfirmation { get; set; }

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public bool IsReadBackEnabled { get; set; }

		public string ExampleText { get; set; }

		public string AudiblePrompt { get; set; }
	}
}
