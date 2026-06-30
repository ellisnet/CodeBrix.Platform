using CodeBrix.Platform.Simple;
using JustBetweenUs.Encryption.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace JustBetweenUs.ViewModels;

public interface ICopyToClipboard
{
    Action<string> CopyTextToClipboard { get; set; }
}

#if HAS_CODEBRIX
[Microsoft.UI.Xaml.Data.Bindable]
#endif
public class MainViewModel : SimpleViewModel, ICopyToClipboard
{
    private IEncryptionService _encryptSvc;
    private bool _copyMessageShown;
    private SimpleOsInfo _osInfo;

    public MainViewModel()
    {
        if (!IsDesignMode(true))
        {
            Debug.WriteLine("Main view model startup.");

            _encryptSvc = GetService<IEncryptionService>();

            EncryptionModes.Clear();
            foreach (var kvp in _encryptionModeDictionary)
            {
                EncryptionModes.Add(kvp.Value.Description);
            }
            var selection = _encryptionModeDictionary.First();
            _selectedEncryptionMode = selection.Key;
            _selectedEncryptionModeText = selection.Value.Description;
            base.NotifyPropertyChanged(nameof(EncryptionModes));
            base.NotifyPropertyChanged(nameof(SelectedEncryptionModeText));

            // ReSharper disable once AsyncVoidLambda
            new Task(async () => //intentionally a fire-and-forget task
            {
                var defaultKey = await _encryptSvc.GetDefaultKey();
                //We can't set a value to EncryptionKey except on the main (UI) thread, because this causes problems on Linux and macOS
                InvokeOnMainThread(() => EncryptionKey = defaultKey);
                await Task.Delay(2000);
                await ShowInfo("This application is adapted from a sample provided by Paul Ainsworth.");
            }).Start();
        }
    }

    #region | Bindable properties |

    #region Select encryption mode

    private readonly Dictionary<EncryptionMode.CryptAlgorithm, EncryptionMode> _encryptionModeDictionary =
        EncryptionMode.GetDictionary();

    public List<string> EncryptionModes { get; } = new();

    private EncryptionMode.CryptAlgorithm _selectedEncryptionMode;

    private string _selectedEncryptionModeText = string.Empty;

    public string SelectedEncryptionModeText
    {
        get => _selectedEncryptionModeText;
        set
        {
            SetProperty(ref _selectedEncryptionModeText, value);
            _selectedEncryptionMode = _encryptionModeDictionary
                .Single(s => s.Value.Description == value)
                .Key;
        }
    }

    #endregion

    private string _encryptionKey = string.Empty;
    [AffectsCommands(nameof(EncryptCommand), nameof(DecryptCommand))]
    public string EncryptionKey
    {
        get => _encryptionKey;
        set => SetProperty(ref _encryptionKey, value ?? string.Empty);
    }

    private string _enteredText = string.Empty;
    [AffectsCommands(nameof(EncryptCommand), nameof(DecryptCommand))]
    public string EnteredText
    {
        get => _enteredText;
        set => SetProperty(ref _enteredText, value ?? string.Empty);
    }

    private string _processedText = string.Empty;
    [AffectsCommands(nameof(CopyToClipboardCommand))]
    public string ProcessedText
    {
        get => _processedText;
        set => SetProperty(ref _processedText, value ?? string.Empty);
    }

    #endregion

    #region | Commands and their implementations |

    #region EncryptCommand

    private SimpleCommand _encryptCommand;
    public SimpleCommand EncryptCommand => 
        (_encryptCommand ??= new SimpleCommand(CanEncrypt, DoEncrypt));

    private bool CanEncrypt() => 
        (!string.IsNullOrWhiteSpace(EncryptionKey)) && (!string.IsNullOrWhiteSpace(EnteredText));

    private async Task DoEncrypt()
    {
        if (CanEncrypt())
        {
            try
            {
                switch (_selectedEncryptionMode)
                {
                    case EncryptionMode.CryptAlgorithm.Aes:
                        ProcessedText = await _encryptSvc.AES_EncryptToBase64(EncryptionKey.Trim(), EnteredText);
                        break;

                    case EncryptionMode.CryptAlgorithm.TripleDes:
                        ProcessedText = await _encryptSvc.OriginalTripleDES_EncryptToBase64(EncryptionKey.Trim(), EnteredText);
                        break;
                    
                    case EncryptionMode.CryptAlgorithm.Twofish:
                        ProcessedText = await _encryptSvc.Twofish_EncryptToBase64(EncryptionKey.Trim(), EnteredText);
                        break;
                }
            }
            catch (Exception e)
            {
                await ShowError($"Error while encrypting: {e.Message}");
            }
        }
    }

    #endregion

    #region DecryptCommand

    private SimpleCommand _decryptCommand;
    public SimpleCommand DecryptCommand =>
        (_decryptCommand ??= new SimpleCommand(CanDecrypt, DoDecrypt));

    private bool CanDecrypt() =>
        (!string.IsNullOrWhiteSpace(EncryptionKey)) 
        && (!string.IsNullOrWhiteSpace(EnteredText));
        //&& IsBase64Text(EnteredText); //Too distracting to have the button flash on and off

    private async Task DoDecrypt()
    {
        if (CanDecrypt())
        {
            if (!_encryptSvc.IsBase64Text(EnteredText))
            {
                await ShowInfo("The specified text does not look like it is encrypted.");
            }
            else
            {
                try
                {
                    switch (_selectedEncryptionMode)
                    {
                        case EncryptionMode.CryptAlgorithm.Aes:
                            ProcessedText = await _encryptSvc.AES_DecryptFromBase64(EncryptionKey.Trim(), EnteredText);
                            break;

                        case EncryptionMode.CryptAlgorithm.TripleDes:
                            ProcessedText = await _encryptSvc.OriginalTripleDES_DecryptFromBase64(EncryptionKey.Trim(), EnteredText);
                            break;

                        case EncryptionMode.CryptAlgorithm.Twofish:
                            ProcessedText = await _encryptSvc.Twofish_DecryptFromBase64(EncryptionKey.Trim(), EnteredText);
                            break;
                    }
                }
                catch (Exception e)
                {
                    await ShowError($"Error while decrypting: {e.Message}");
                }
            }
        }
    }

    #endregion

    #region CopyToClipboardCommand

    private SimpleCommand _copyToClipboardCommand;
    public SimpleCommand CopyToClipboardCommand =>
        (_copyToClipboardCommand ??= new SimpleCommand(CanCopyToClipboard, DoCopyToClipboard));

    private bool CanCopyToClipboard() => (!string.IsNullOrWhiteSpace(ProcessedText));

    private async Task DoCopyToClipboard()
    {
        if (CanCopyToClipboard())
        {
            if (CopyTextToClipboard != null)
            {
                InvokeOnMainThread(() => CopyTextToClipboard(ProcessedText));
                if (!_copyMessageShown)
                {
                    _copyMessageShown = true;
                    await ShowInfo("The processed text has been copied to the system clipboard.");
                }
            }
            else
            {
                await ShowError(
                    "This platform implementation does not have the Copy-to-clipboard functionality enabled.");
            }
        }
    }

    #endregion

    #region ShowOsInfoCommand
    
    private SimpleCommand _showOsInfoCommand;
    public SimpleCommand ShowOsInfoCommand =>
        (_showOsInfoCommand ??= new SimpleCommand(CanShowOsInfo, DoShowOsInfo));
    
    private bool CanShowOsInfo() => true;

    private async Task DoShowOsInfo()
    {
        if (CanShowOsInfo())
        {
            _osInfo ??= await SimpleOsInfo.GatherInfo(withConsoleOutput: false);
            var sb = new StringBuilder();
            sb.AppendLine($"Currently running on: {_osInfo.PlatformOsName}");
            sb.AppendLine($"Operating system description: {_osInfo.OsDescription}");
            sb.AppendLine($"Operating system version: {_osInfo.OsVersion}");
            sb.AppendLine($"Product name: {_osInfo.ProductName}");
            sb.AppendLine($"Product name (for display): {_osInfo.ProductNameDisplay}");

            //Note that when running via CodeBrix.Platform on Android, the following lines will not be displayed - since
            //  the SimpleDialog text is truncated on this platform - it must have a maximum number of lines.
            //  Further note: CodeBrix.Platform is not supported on Android
            sb.AppendLine($"Running as user: {_osInfo.RunningAsUser}{((_osInfo.IsAdminUser is true) ? " (local admin)" : "")}");
            sb.AppendLine($"DotNet version: {_osInfo.DotNetVersion}");
            sb.AppendLine($"Platform architecture: {_osInfo.PlatformArchitecture}");

            await ShowInfo(sb.ToString());
        }
    }
    
    #endregion
    
    #endregion

    #region | ICopyToClipboard implementation |

    public Action<string> CopyTextToClipboard { get; set; }

    #endregion

    #region | IDisposable implementation |

    public override void Dispose()
    {
        _encryptSvc = null;
        _encryptCommand?.Dispose();
        _encryptCommand = null;
        _decryptCommand?.Dispose();
        _decryptCommand = null;
        _copyToClipboardCommand?.Dispose();
        _copyToClipboardCommand = null;
        CopyTextToClipboard = null;
        base.Dispose();
    }

    #endregion
}
