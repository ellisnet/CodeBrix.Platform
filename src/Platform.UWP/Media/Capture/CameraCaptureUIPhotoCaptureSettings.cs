#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	public partial class CameraCaptureUIPhotoCaptureSettings
	{
#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public CameraCaptureUIMaxPhotoResolution MaxResolution
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "CameraCaptureUIMaxPhotoResolution CameraCaptureUIPhotoCaptureSettings.MaxResolution");
				return CameraCaptureUIMaxPhotoResolution.HighestAvailable;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "CameraCaptureUIMaxPhotoResolution CameraCaptureUIPhotoCaptureSettings.MaxResolution");
			}
		}

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public CameraCaptureUIPhotoFormat Format { get; set; } = CameraCaptureUIPhotoFormat.Jpeg;

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public global::Windows.Foundation.Size CroppedSizeInPixels
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedSizeInPixels");
				return global::Windows.Foundation.Size.Empty;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedSizeInPixels");
			}
		}

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public global::Windows.Foundation.Size CroppedAspectRatio
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedAspectRatio");
				return global::Windows.Foundation.Size.Empty;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings", "Size CameraCaptureUIPhotoCaptureSettings.CroppedAspectRatio");
			}
		}

#if IS_UNIT_TESTS
		[global::CodeBrix.Platform.NotImplemented]
#endif
		public bool AllowCropping { get; set; } = true;
	}
}
