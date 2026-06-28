namespace CodeBrix.Platform.ApplicationModel.Core;

internal interface ICoreApplicationExtension
{
	bool CanExit { get; }

	void Exit();
}
