namespace Windows.Media.SpeechRecognition
{
	public enum SpeechRecognitionResultStatus
	{
		Success,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		TopicLanguageNotSupported,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		GrammarLanguageMismatch,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		GrammarCompilationFailure,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		AudioQualityFailure,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		UserCanceled,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		Unknown,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		TimeoutExceeded,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		PauseLimitExceeded,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		NetworkFailure,

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		MicrophoneUnavailable
	}
}
