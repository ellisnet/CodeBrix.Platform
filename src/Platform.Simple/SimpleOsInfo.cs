using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.Simple;

public enum IdentifiedLinuxDistro
{
    //Note: when adding to this list, consider whether the new member(s)
    //  also needs to be added to the DebianStyleDistros array below.

    Unknown = 0,
    Alpine,
    Debian,
    Ubuntu,
    Mint,
    // ReSharper disable once InconsistentNaming
    LMDE,
    Android,
    NotLinux = 999,
}

public class RunUnixShellCommandResult
{
    public RunUnixShellCommandResult(bool isComplete = false)
    {
        IsComplete = isComplete;
    }

    public string Output { get; set; }
    public string Error { get; set; }
    public Exception Exception { get; set; }
    public bool IsComplete { get; private set; }

    public bool IsError => (!string.IsNullOrWhiteSpace(Error))
                           || Exception != null;

    public bool IsEmptyOutput => (string.IsNullOrWhiteSpace(Output));

    public string[] OutputLines =>
        (string.IsNullOrWhiteSpace(Output))
            ? [string.Empty]
            : Output.Split(SimpleOsInfo.UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);

    public void SetComplete() => IsComplete = true;
}

#pragma warning disable CA1822

public class OsVersionInfo
{
    public string VersionNumber { get; set; }
    public int MajorVersion { get; set; } //This is the first version value X.y.y.y
    public int? MinorVersion { get; set; } //This is the second version value y.X.y.y
    public int? BuildVersion { get; set; } //This is the third version value y.y.X.y
    public int? RevisionVersion { get; set; } //This is the fourth version value y.y.y.X
    public bool? IsLongTermSupported { get; set; }
    public string VersionCodename { get; set; }
    public string BasedOnVersion { get; set; }
    public string ProductName { get; set; }
    public string ProductNameDisplay { get; set; }

    public string FullVersion
    {
        get
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(VersionNumber))
            {
                sb.Append(VersionNumber + " ");
            }
            else if (MajorVersion > 0 || MinorVersion.HasValue)
            {
                sb.Append(MajorVersion);
                if (MinorVersion.HasValue)
                {
                    sb.Append($".{MinorVersion.Value}");
                    if (BuildVersion.HasValue)
                    {
                        sb.Append($".{BuildVersion.Value}");
                        if (RevisionVersion.HasValue)
                        {
                            sb.Append($".{RevisionVersion.Value}");
                        }
                    }
                }

                sb.Append(' ');
            }

            if ((IsLongTermSupported is true)
                && (string.IsNullOrWhiteSpace(VersionNumber)
                    || (!VersionNumber.Contains("LTS", StringComparison.InvariantCultureIgnoreCase))))
            {
                sb.Append("LTS ");
            }

            var details = string.Empty;

            if ((!string.IsNullOrWhiteSpace(VersionCodename))
                && (string.IsNullOrWhiteSpace(VersionNumber)
                    || (!VersionNumber.Contains(VersionCodename.Trim(), StringComparison.InvariantCultureIgnoreCase))))
            {
                details += VersionCodename.Trim();
            }

            if (!string.IsNullOrWhiteSpace(BasedOnVersion))
            {
                details += (string.IsNullOrWhiteSpace(details))
                    ? string.Empty
                    : " - ";
                details += $"based on: {BasedOnVersion.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(details))
            {
                sb.Append($"({details.Trim()}) ");
            }

            return sb.ToString().Trim();
        }
    }
}

public class SimpleOsInfo
{
    public static char[] UnixLineSplitters { get; } = ['\r', '\n'];

    private static IdentifiedLinuxDistro[] DebianStyleDistros { get; } =
    [
        IdentifiedLinuxDistro.Debian,
        IdentifiedLinuxDistro.LMDE,
        IdentifiedLinuxDistro.Mint,
        IdentifiedLinuxDistro.Ubuntu
    ];

    //The following list is from here:
    //  https://en.wikipedia.org/wiki/MacOS_version_history
    //  This list was last retrieved and updated: 3/28/2026
    private static (int DarwinVersion, string Codename)[] MacOsCodenames { get; } =
        [
            (6, "Jaguar"),
            (7, "Panther"),
            (8, "Tiger"),
            (9, "Leopard"),
            (10, "Snow Leopard"),
            (11, "Lion"),
            (12, "Mountain Lion"),
            (13, "Mavericks"),
            (14, "Yosemite"),
            (15, "El Capitan"),
            (16, "Sierra"),
            (17, "High Sierra"),
            (18, "Mojave"),
            (19, "Catalina"),
            (20, "Big Sur"),
            (21, "Monterey"),
            (22, "Ventura"),
            (23, "Sonoma"),
            (24, "Sequoia"),
            (25, "Tahoe"),
        ];

    //The following list is from here:
    //  https://en.wikipedia.org/wiki/Android_version_history
    //  This list was last retrieved and updated: 3/28/2026
    private static Dictionary<int, (Version Version, string Codename)> AndroidCodenames { get; } = new()
    {
        { 1, new ValueTuple<Version, string>(new Version(1, 0), "Initial") },
        { 2, new ValueTuple<Version, string>(new Version(1, 1), "Petit Four") },
        { 3, new ValueTuple<Version, string>(new Version(1, 5), "Cupcake") },
        { 4, new ValueTuple<Version, string>(new Version(1, 6), "Donut") },
        { 5, new ValueTuple<Version, string>(new Version(2, 0), "Eclair") },
        { 6, new ValueTuple<Version, string>(new Version(2,0,1), "Eclair") },
        { 7, new ValueTuple<Version, string>(new Version(2, 1), "Eclair") },
        { 8, new ValueTuple<Version, string>(new Version(2, 2), "Froyo") },
        { 9, new ValueTuple<Version, string>(new Version(2, 3), "Gingerbread") },
        { 10, new ValueTuple<Version, string>(new Version(2, 3, 3), "Gingerbread") },
        { 11, new ValueTuple<Version, string>(new Version(3, 0), "Honeycomb") },
        { 12, new ValueTuple<Version, string>(new Version(3, 1), "Honeycomb") },
        { 13, new ValueTuple<Version, string>(new Version(3, 2), "Honeycomb") },
        { 14, new ValueTuple<Version, string>(new Version(4, 0), "Ice Cream Sandwich") },
        { 15, new ValueTuple<Version, string>(new Version(4, 0, 3), "Ice Cream Sandwich") },
        { 16, new ValueTuple<Version, string>(new Version(4, 1), "Jelly Bean") },
        { 17, new ValueTuple<Version, string>(new Version(4, 2), "Jelly Bean") },
        { 18, new ValueTuple<Version, string>(new Version(4, 3), "Jelly Bean") },
        { 19, new ValueTuple<Version, string>(new Version(4, 4), "KitKat") },
        { 20, new ValueTuple<Version, string>(new Version(4, 5), "KitKat") },
        { 21, new ValueTuple<Version, string>(new Version(5, 0), "Lollipop") },
        { 22, new ValueTuple<Version, string>(new Version(5, 1), "Lollipop") },
        { 23, new ValueTuple<Version, string>(new Version(6, 0), "Marshmallow") },
        { 24, new ValueTuple<Version, string>(new Version(7, 0), "Nougat") },
        { 25, new ValueTuple<Version, string>(new Version(7, 1), "Nougat") },
        { 26, new ValueTuple<Version, string>(new Version(8, 0), "Oreo") },
        { 27, new ValueTuple<Version, string>(new Version(8, 1), "Oreo") },
        { 28, new ValueTuple<Version, string>(new Version(9, 0), "Pie") },
        { 29, new ValueTuple<Version, string>(new Version(10, 0), "Quince Tart") },
        { 30, new ValueTuple<Version, string>(new Version(11, 0), "Red Velvet Cake") },
        { 31, new ValueTuple<Version, string>(new Version(12, 0), "Snow Cone") },
        { 32, new ValueTuple<Version, string>(new Version(12, 1), "Snow Cone V2") },
        { 33, new ValueTuple<Version, string>(new Version(13, 0), "Tiramisu") },
        { 34, new ValueTuple<Version, string>(new Version(14, 0), "Upside Down Cake") },
        { 35, new ValueTuple<Version, string>(new Version(15, 0), "Vanilla Ice Cream") },
        { 36, new ValueTuple<Version, string>(new Version(16, 0), "Baklava") },
        { 37, new ValueTuple<Version, string>(new Version(17, 0), "Cinnamon Bun") },
    };

    private const string UnixRootUsername = "root";
    private const string DebianDistroIdentifier = "debian";
    private const string LmdeDistroIdentifier = "lmde";
    private const string MintDistroIdentifier = "mint";
    private const string UbuntuDistroIdentifier = "ubuntu";
    private const string AlpineDistroIdentifier = "alpine linux";

    public const string LinuxListTextFileCommand = "cat";
    public const string LinuxIdentifyDistroArgs = "/etc/issue";
    public const string DebianVersionArgs = "/etc/debian_version";
    public const string LinuxOsReleaseDetailsArgs = "/etc/os-release";

    public const string MacOsInfoCommand = "system_profiler";
    public const string MacOsInfoCommandArgs = "SPSoftwareDataType";

    public const string UnixUsernameCommand = "whoami";

    public static Task<SimpleOsInfo> GatherInfo(bool withConsoleOutput = false) =>
        (new SimpleOsInfo()).Gather(withConsoleOutput);

    private static (int MajorVersion, int? MinorVersion, int? BuildVersion, int? RevisionVersion) GetVersionParts(string versionText)
    {
        var majorVersion = 0;
        int? minorVersion, buildVersion;
        var revisionVersion = buildVersion = minorVersion = null;

        if (!string.IsNullOrWhiteSpace(versionText))
        {
            var versionParts = versionText.Trim().Split('.');

            if (versionParts.Length > 0 && int.TryParse(versionParts[0], out var major))
            {
                majorVersion = major;

                if (versionParts.Length > 1 && int.TryParse(versionParts[1], out var minor))
                {
                    minorVersion = minor;

                    if (versionParts.Length > 2 && int.TryParse(versionParts[2], out var build))
                    {
                        buildVersion = build;

                        if (versionParts.Length > 3 && int.TryParse(versionParts[3], out var revision))
                        {
                            revisionVersion = revision;
                        }
                    }
                }
            }
        }

        return (majorVersion, minorVersion, buildVersion, revisionVersion);
    }

    private static Dictionary<string, string> GetInfoDictionary(IList<string> infoLines, char assignmentChar)
    {
        var result = new Dictionary<string, string>();

        if (infoLines != null)
        {
            foreach (var line in infoLines
                         .Where(w => (!string.IsNullOrWhiteSpace(w)) && w.Contains(assignmentChar))
                         .Select(s => s.Trim()))
            {
                var lineParts = line.Split(assignmentChar);
                if (lineParts.Length > 1)
                {
                    var key = lineParts[0];
                    var value = string.Join(assignmentChar, lineParts[1..]);

                    if (value.StartsWith('\"') && value.EndsWith('\"'))
                    {
                        value = value.TrimStart('\"').TrimEnd('\"').Trim();
                    }

                    _ = result.TryAdd(key, value);
                }
            }
        }

        return result;
    }

    private async Task<OsVersionInfo> GetDebianVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsLinux && distro == IdentifiedLinuxDistro.Debian)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the best possible version info |

                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var version = shellResult.OutputLines[0].Trim().Split(' ').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(version)) { break; }

                result.VersionNumber = version;

                var (majorVersion, minorVersion, buildVersion, revisionVersion) = GetVersionParts(version);
                result.MajorVersion = majorVersion;
                result.MinorVersion = minorVersion;
                result.BuildVersion = buildVersion;
                result.RevisionVersion = revisionVersion;

                #endregion

                #region | Get the os version codename and product name |

                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');

                if (infoDictionary is not { Count: > 0 }) { break; }

                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion

            } while (false);
        }

        return result;
    }

    private async Task GetDebianUserInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        if (IsLinux && DebianStyleDistros.Contains(distro))
        {
            var shellResult = await RunUnixShellCommand(UnixUsernameCommand,
                showOutput: withConsoleOutput);
            if (shellResult.IsComplete && (!shellResult.IsError) && (!shellResult.IsEmptyOutput))
            {
                RunningAsUser = shellResult.OutputLines[0].Trim();
                IsAdminUser = IsUnixRootUser(RunningAsUser);
            }
        }
    }

    private async Task<OsVersionInfo> GetUbuntuVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsLinux && distro == IdentifiedLinuxDistro.Ubuntu)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the os version, codename and product name |

                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');

                if (infoDictionary is not { Count: > 0 }) { break; }

                if (infoDictionary.TryGetValue("VERSION", out var version)
                    && (!string.IsNullOrWhiteSpace(version)))
                {
                    result.VersionNumber = version.Trim();

                    var versionTextParts = version.Trim()
                        .Split(" ")
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    result.IsLongTermSupported =
                        versionTextParts.Any(a => a.Equals("LTS", StringComparison.InvariantCultureIgnoreCase));

                    var (majorVersion, minorVersion, buildVersion, revisionVersion) = GetVersionParts(versionTextParts[0]);
                    result.MajorVersion = majorVersion;
                    result.MinorVersion = minorVersion;
                    result.BuildVersion = buildVersion;
                    result.RevisionVersion = revisionVersion;
                }

                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion

                #region | Get the BasedOnVersion info |

                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                result.BasedOnVersion = $"Debian {shellResult.OutputLines[0].Trim()}";

                #endregion
            } while (false);
        }

        return result;
    }

    private async Task<OsVersionInfo> GetMintVersionInfo(IdentifiedLinuxDistro distro,
        bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsLinux && distro is IdentifiedLinuxDistro.Mint or IdentifiedLinuxDistro.LMDE)
        {
            result = new OsVersionInfo();

            do
            {
                #region | Get the os version, codename and product name |

                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxOsReleaseDetailsArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, '=');

                if (infoDictionary is not { Count: > 0 }) { break; }

                if (infoDictionary.TryGetValue("VERSION", out var version)
                    && (!string.IsNullOrWhiteSpace(version)))
                {
                    result.VersionNumber = version.Trim();

                    var versionTextParts = version.Trim()
                        .Split(" ")
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    result.IsLongTermSupported =
                        versionTextParts.Any(a => a.Equals("LTS", StringComparison.InvariantCultureIgnoreCase));

                    var (majorVersion, minorVersion, buildVersion, revisionVersion) = GetVersionParts(versionTextParts[0]);
                    result.MajorVersion = majorVersion;
                    result.MinorVersion = minorVersion;
                    result.BuildVersion = buildVersion;
                    result.RevisionVersion = revisionVersion;
                }

                if (infoDictionary.TryGetValue("VERSION_CODENAME", out var codename)
                    && (!string.IsNullOrWhiteSpace(codename)))
                {
                    result.VersionCodename = codename.Trim();
                }

                if (infoDictionary.TryGetValue("NAME", out var name)
                    && (!string.IsNullOrWhiteSpace(name)))
                {
                    result.ProductName = name.Trim();
                }

                if (infoDictionary.TryGetValue("PRETTY_NAME", out var displayName)
                    && (!string.IsNullOrWhiteSpace(displayName)))
                {
                    result.ProductNameDisplay = displayName.Trim();
                }

                #endregion

                #region | Get the BasedOnVersion info |

                shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, DebianVersionArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                result.BasedOnVersion = $"Debian {shellResult.OutputLines[0].Trim()}";

                #endregion
            } while (false);
        }

        return result;
    }

    private async Task<OsVersionInfo> GetWindowsVersionInfo(bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsWindows)
        {
            result = new OsVersionInfo();

            var version = Environment.OSVersion.Version;

            if (withConsoleOutput)
            {
                Console.WriteLine("Windows version info -");
                Console.WriteLine($"Major: {version.Major}");
                Console.WriteLine($"Minor: {version.Minor}");
                Console.WriteLine($"Build: {version.Build}");
                Console.WriteLine($"Revision: {version.Revision}");
            }

            result.VersionNumber = version.ToString();
            result.MajorVersion = version.Major;
            result.MinorVersion = version.Minor;
            result.BuildVersion = version.Build;
            result.RevisionVersion = version.Revision;

            var winVersion = result.MajorVersion;

            //refer to: https://stackoverflow.com/questions/69038560/detect-windows-11-with-net-framework-or-windows-api
            if (winVersion == 10 && result.MinorVersion >= 0 && result.BuildVersion >= 22000)
            {
                winVersion = 11;
            }

            result.ProductName = $"Microsoft Windows {winVersion}";

            //Satisfy the compiler that something async is happening
            var details = await Task.FromResult(string.Empty);

            if (!string.IsNullOrWhiteSpace(Environment.OSVersion.ServicePack))
            {
                details += $"Service Pack {Environment.OSVersion.ServicePack}";
            }

            details += $"{((!string.IsNullOrWhiteSpace(details)) ? " - " : "")}{Environment.OSVersion.Platform}";

            result.ProductNameDisplay = $"{result.ProductName} ({details.Trim()})";
        }

        return result;
    }

    private async Task GetWindowsUserInfo(bool withConsoleOutput = false)
    {
        if (IsWindows)
        {
#pragma warning disable CA1416
            using var identity = WindowsIdentity.GetCurrent();

            if (identity != null)
            {
                if (withConsoleOutput)
                {
                    Console.WriteLine("Windows identity found -");
                    Console.WriteLine($"Name: {identity.Name}");
                    var claims = identity.Claims?.ToArray();
                    if (claims is { Length: > 0 })
                    {
                        Console.WriteLine("User claims:");
                        foreach (var claim in claims)
                        {
                            Console.WriteLine($" - {claim}");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(identity.Name))
                {
                    RunningAsUser = identity.Name.Trim();
                }

                IsAdminUser = (new WindowsPrincipal(identity)).IsInRole(WindowsBuiltInRole.Administrator);
            }
#pragma warning restore CA1416

            if (string.IsNullOrWhiteSpace(RunningAsUser))
            {
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("USERNAME")))
                {
                    RunningAsUser = Environment.GetEnvironmentVariable("USERNAME")!.Trim();
                }
                else
                {
                    //Satisfy the compiler that something async is happening
                    RunningAsUser = await Task.FromResult(string.Empty);
                }
            }
        }
    }

    private async Task<OsVersionInfo> GetMacOsVersionInfo(bool withConsoleOutput = false)
    {
        OsVersionInfo result = null;

        if (IsMacOs)
        {
            result = new OsVersionInfo();

            do
            {
                var shellResult = await RunUnixShellCommand(MacOsInfoCommand, MacOsInfoCommandArgs,
                    showOutput: withConsoleOutput);
                if (shellResult.IsError || (!shellResult.IsComplete) || shellResult.IsEmptyOutput) { break; }

                var infoDictionary = GetInfoDictionary(shellResult.OutputLines, ':');

                if (infoDictionary.TryGetValue("System Version", out var sysVersion)
                    && (!string.IsNullOrWhiteSpace(sysVersion)))
                {
                    var sysVersionParts = sysVersion.Split(' ')
                        .Where(w => !string.IsNullOrWhiteSpace(w))
                        .Select(s => s.Trim())
                        .ToArray();

                    foreach (var part in sysVersionParts)
                    {
                        var versionParts = GetVersionParts(part);
                        if (versionParts is { MajorVersion: > 0, MinorVersion: not null })
                        {
                            result.MajorVersion = versionParts.MajorVersion;
                            result.MinorVersion = versionParts.MinorVersion;
                            result.BuildVersion = versionParts.BuildVersion;
                            result.RevisionVersion = versionParts.RevisionVersion;

                            result.VersionNumber = $"macOS {part.Trim()}";

                            break;
                        }
                    }
                }

                if (infoDictionary.TryGetValue("Kernel Version", out var basedOn)
                    && (!string.IsNullOrWhiteSpace(basedOn)))
                {
                    result.BasedOnVersion = basedOn.Trim();

                    var (majorVersion, _, _, _) =
                        GetVersionParts(basedOn.Replace("darwin", "", StringComparison.InvariantCultureIgnoreCase));
                    if (majorVersion > 0)
                    {
                        if (MacOsCodenames.Any(a => a.DarwinVersion == majorVersion))
                        {
                            result.VersionCodename = MacOsCodenames
                                .First(f => f.DarwinVersion == majorVersion)
                                .Codename;
                        }
                        else
                        {
                            var (darwinMajor, codename) = MacOsCodenames.MaxBy(o => o.DarwinVersion);
                            if (majorVersion > darwinMajor)
                            {
                                result.VersionCodename = $"newer than {codename}";
                            }
                        }
                    }
                }
            } while (false);
        }

        return result;
    }

    private async Task GetMacOsUserInfo(bool withConsoleOutput = false)
    {
        if (IsMacOs)
        {
            var shellResult = await RunUnixShellCommand(UnixUsernameCommand,
                showOutput: withConsoleOutput);
            if (shellResult.IsComplete && (!shellResult.IsError) && (!shellResult.IsEmptyOutput))
            {
                RunningAsUser = shellResult.OutputLines[0].Trim();
                IsAdminUser = IsUnixRootUser(RunningAsUser);
            }
        }
    }

    private static OsVersionInfo GetAndroidVersionInfo()
    {
        var result = new OsVersionInfo
        {
            VersionNumber = null,
            MajorVersion = 0,
            MinorVersion = null,
            BuildVersion = null,
            RevisionVersion = null,
            IsLongTermSupported = null,
            VersionCodename = null,
            BasedOnVersion = null,
            ProductName = null,
            ProductNameDisplay = null
        };

#if (MAUI || HAS_CODEBRIX)
        var version = string.Empty;
        var major = 0;
        var minor = 0;
        var build = 0;

        //Model and Manufacturer info will only be present with MAUI - not CodeBrix.Platform - I haven't found a method
        //  equivalent to DeviceInfo.Current for getting this information on Android running with CodeBrix.Platform.
        //  Further note: CodeBrix.Platform is not supported on Android.

        var model = string.Empty;
        var manufacturer = string.Empty;

#if MAUI
        var info = DeviceInfo.Current.Version;

        if (info.Major > 0)
        {
            result.MajorVersion = major = info.Major;
            version += $"{major}";

            if (info.Minor > -1)
            {
                result.MinorVersion = minor = info.Minor;
                version += $".{minor}";

                if (info.Build > -1)
                {
                    result.BuildVersion = build = info.Build;
                    version += $".{build}";

                    if (info.Revision > -1)
                    {
                        result.RevisionVersion = info.Revision;
                        version += $".{info.Revision}";
                    }
                }
            }
        }
        model = DeviceInfo.Current.Model?.Trim() ?? string.Empty;
        manufacturer = DeviceInfo.Current.Manufacturer?.Trim() ?? string.Empty;

#elif HAS_CODEBRIX
        try
        {
#pragma warning disable Uno0001  //This code section is only active on Android
            var encoded = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
#pragma warning restore Uno0001
            var v = ulong.Parse(encoded);
            major = (int)(ushort)((v & 0xFFFF000000000000L) >> 48);
            minor = (int)(ushort)((v & 0x0000FFFF00000000L) >> 32);
            build = (int)(ushort)((v & 0x00000000FFFF0000L) >> 16);
            var revision = (int)(ushort)((v & 0x000000000000FFFFL));

            if (major > 0)
            {
                result.MajorVersion = major;
                result.MinorVersion = minor;
                version += $"{major}.{minor}";

                if (build > 0 || revision > 0)
                {
                    result.BuildVersion = build;
                    version += $".{build}";

                    if (revision > 0)
                    {
                        result.RevisionVersion = revision;
                        version += $".{revision}";
                    }
                }
            }
        }
        catch (Exception)
        {
            //Version info could not be decoded
        }
#endif

        if (major > 0)
        {
            foreach (var kvp in AndroidCodenames.OrderByDescending(o => o.Key))
            {
                if (major >= kvp.Value.Version.Major
                    && minor >= kvp.Value.Version.Minor
                    && build >= kvp.Value.Version.Build)
                {
                    result.VersionCodename = kvp.Value.Codename;
                    result.ProductName = $"Android {version} ({kvp.Value.Codename}) API level {kvp.Key}";
                    break;
                }
            }

            result.VersionNumber = version;
            if (string.IsNullOrWhiteSpace(result.ProductName))
            {
                result.ProductName = $"Android {version}";
            }

            //See note above - model and manufacturer only available on MAUI, not CodeBrix.Platform
            if (string.IsNullOrWhiteSpace(model))
            {
                result.ProductNameDisplay = result.ProductName;
            }
            else
            {
                result.ProductNameDisplay = $"{result.ProductName} - {model}"
                                            + $"{(string.IsNullOrWhiteSpace(manufacturer) ? string.Empty : $" ({manufacturer})")}";
            }
        }

#endif

        return result;
    }

    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#if MAUI
    public bool IsAndroid => DeviceInfo.Current.Platform == DevicePlatform.Android;
#elif (WIN_UI || HAS_CODEBRIX)
    public bool IsAndroid => (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily?.Trim() ?? string.Empty)
        .StartsWith("Android.", StringComparison.InvariantCultureIgnoreCase);
#else
    public bool IsAndroid => false;
#endif

    public bool IsX64 => RuntimeInformation.OSArchitecture == Architecture.X64;
    public bool IsArm64 => RuntimeInformation.OSArchitecture == Architecture.Arm64;

    public string OsDescription => RuntimeInformation.OSDescription;

    public IdentifiedLinuxDistro LinuxDistro { get; private set; } = IdentifiedLinuxDistro.Unknown;
    public OsVersionInfo OsVersionInfo { get; private set; }

    public string RunningAsUser { get; private set; }
    public bool? IsAdminUser { get; private set; }

    public string OsVersion => (string.IsNullOrWhiteSpace(OsVersionInfo?.FullVersion))
        ? "(information not available)"
        : OsVersionInfo.FullVersion;

    public string ProductName => (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductName))
        ? OsVersionInfo.ProductName.Trim()
        : (!string.IsNullOrWhiteSpace(OsDescription))
            ? OsDescription
            : string.Empty;

    public string ProductNameDisplay => (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductNameDisplay))
        ? OsVersionInfo.ProductNameDisplay.Trim()
        : (!string.IsNullOrWhiteSpace(OsDescription))
            ? OsDescription.Trim()
            : (!string.IsNullOrWhiteSpace(OsVersionInfo?.ProductName))
                ? OsVersionInfo.ProductName.Trim()
                : string.Empty;

    public string LinuxDistroName => (IsLinux)
        ? LinuxDistro.ToString()
        : string.Empty;

    public string PlatformOsName => (IsWindows)
        ? "Microsoft Windows"
        : (IsMacOs)
            ? "Apple macOS"
            : (IsAndroid)
                ? "Android"
                : (IsLinux)
                    ? $"Linux ({((LinuxDistro == IdentifiedLinuxDistro.Unknown) ? "unidentified distro" : LinuxDistro.ToString())})"
                    : "Unknown";

    public string DotNetVersion => RuntimeEnvironment.GetSystemVersion();

    public string PlatformArchitecture => RuntimeInformation.OSArchitecture.ToString();

    public async Task<SimpleOsInfo> Gather(bool withConsoleOutput = false)
    {
        if (IsMacOs)
        {
            LinuxDistro = IdentifiedLinuxDistro.NotLinux;
            OsVersionInfo = await GetMacOsVersionInfo(withConsoleOutput);
            await GetMacOsUserInfo(withConsoleOutput);
        }
        else if (IsWindows)
        {
            LinuxDistro = IdentifiedLinuxDistro.NotLinux;
            OsVersionInfo = await GetWindowsVersionInfo(withConsoleOutput);
            await GetWindowsUserInfo(withConsoleOutput);
        }
        else if (IsAndroid)
        {
            LinuxDistro = IdentifiedLinuxDistro.Android;
            OsVersionInfo = GetAndroidVersionInfo();
            RunningAsUser = "mobile";
            IsAdminUser = false;
        }
        else if (IsLinux)
        {
            do
            {
                //Step 1 - try to identify the distro
                var shellResult = await RunUnixShellCommand(LinuxListTextFileCommand, LinuxIdentifyDistroArgs,
                    showOutput: withConsoleOutput);
                if (!shellResult.IsError)
                {
                    LinuxDistro = IdentifyLinuxDistro(shellResult.OutputLines[0]);
                }

                if (withConsoleOutput)
                {
                    Console.WriteLine((shellResult.IsError)
                        ? $"Unable to get Linux distro information, because of error: {shellResult.Error}"
                        : $"Linux distro information: {shellResult.OutputLines[0]}"
                          + $" - Identified distro: {LinuxDistro}");
                }

                if (shellResult.IsError || LinuxDistro == IdentifiedLinuxDistro.Unknown)
                {
                    break;
                }

                //Step 2 - try to get the version info (including exact version #) plus product and user
                switch (LinuxDistro)
                {
                    case IdentifiedLinuxDistro.Alpine:
                        //TODO: Add Alpine support
                        break;

                    case IdentifiedLinuxDistro.Debian:
                        OsVersionInfo = await GetDebianVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;

                    case IdentifiedLinuxDistro.Ubuntu:
                        OsVersionInfo = await GetUbuntuVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;

                    case IdentifiedLinuxDistro.Mint:
                    case IdentifiedLinuxDistro.LMDE:
                        OsVersionInfo = await GetMintVersionInfo(LinuxDistro, withConsoleOutput);
                        await GetDebianUserInfo(LinuxDistro, withConsoleOutput);
                        break;
                }

            } while (false);
        }

        return this;
    }

    public bool IsUnixRootUser(string username) =>
        (!string.IsNullOrWhiteSpace(username))
        && username.Trim().Equals(UnixRootUsername, StringComparison.InvariantCultureIgnoreCase);

    public IdentifiedLinuxDistro IdentifyLinuxDistro(string distroInfo)
    {
        var result = IdentifiedLinuxDistro.Unknown;

        if (IsLinux && (!string.IsNullOrWhiteSpace(distroInfo)))
        {
            var distro = distroInfo.Trim().ToLowerInvariant();
            if (distro.StartsWith(DebianDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Debian;
            }
            else if (distro.StartsWith(LmdeDistroIdentifier)
                     || distro.StartsWith($"linux {MintDistroIdentifier} {DebianDistroIdentifier}"))
            {
                result = IdentifiedLinuxDistro.LMDE;
            }
            else if (distro.StartsWith(MintDistroIdentifier)
                     || distro.StartsWith($"linux {MintDistroIdentifier}"))
            {
                result = IdentifiedLinuxDistro.Mint;
            }
            else if (distro.StartsWith(UbuntuDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Ubuntu;
            }
            else if (distro.Contains(AlpineDistroIdentifier))
            {
                result = IdentifiedLinuxDistro.Alpine;
            }
        }

        return result;
    }

    public static async Task<RunUnixShellCommandResult> RunUnixShellCommand(
        string command,
        string args = null,
        bool ignoreWarnings = true,
        bool showOutput = true,
        int? postRunWaitSeconds = null
    )
    {
        var result = new RunUnixShellCommandResult(isComplete: false);

        if (string.IsNullOrWhiteSpace(command))
        {
            result.Exception = new ArgumentException("Value cannot be null or blank.", nameof(command));
            result.Error = $"Invalid command - {result.Exception.Message}";
        }
        else
        {
            var tcs = new TaskCompletionSource<RunUnixShellCommandResult>();

#pragma warning disable IDE0039

            var shellTask = async () =>
            {
                var taskResult = new RunUnixShellCommandResult(isComplete: false);

                try
                {
                    if (showOutput)
                    {
                        Console.WriteLine($"\nAttempting to run the shell command: {command}"
                            + $"{((string.IsNullOrWhiteSpace(args)) ? "" : $" {args}")}");
                    }

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = command.Trim(),
                            Arguments = (args ?? string.Empty).Trim(),
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start(); //TODO: Need to block access to this line on iOS

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var doDone = false;

                    if (showOutput && (!string.IsNullOrWhiteSpace(output)))
                    {
                        var lines = output.Split(UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine("====OUTPUT====");
                        foreach (var line in lines)
                        {
                            Console.WriteLine(line);
                        }
                        doDone = true;
                    }

                    if (showOutput && (!string.IsNullOrWhiteSpace(error)))
                    {
                        var lines = error.Split(UnixLineSplitters, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine("====ERROR/WARNING INFO====");
                        var removeWarnings = new List<string>();
                        foreach (var line in lines)
                        {
                            Console.WriteLine(line);
                            if (ignoreWarnings
                                && (!line.Trim().StartsWith("warning", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                removeWarnings.Add(line);
                            }
                        }
                        doDone = true;

                        if (ignoreWarnings)
                        {
                            error = (removeWarnings.Count > 0)
                                ? string.Join('\n', removeWarnings)
                                : string.Empty;
                        }
                    }

                    if (doDone)
                    {
                        Console.WriteLine("====DONE====");
                    }

                    // ReSharper disable once ConstantNullCoalescingCondition
                    taskResult.Output = output ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(error))
                    {
                        if (postRunWaitSeconds is > 0)
                        {
                            if (showOutput)
                            {
                                Console.WriteLine($"Waiting {postRunWaitSeconds.Value} seconds for operation cleanup...");
                            }

                            await Task.Delay(TimeSpan.FromSeconds(postRunWaitSeconds.Value));
                        }
                        taskResult.SetComplete();
                    }
                    else
                    {
                        taskResult.Output = error;
                    }
                }
                catch (Exception e)
                {
                    taskResult.Exception = e;
                    taskResult.Error = (!string.IsNullOrWhiteSpace(e.Message))
                        ? e.Message.Trim()
                        : "An unexpected error occurred.";

                    if (showOutput)
                    {
                        Console.WriteLine("====SHELL PROCESS EXCEPTION INFO====");
                        Console.WriteLine(e.ToString());
                        Console.WriteLine("====DONE====");
                    }
                }

                tcs?.TrySetResult(taskResult);
            };

            _ = shellTask.Invoke();

#pragma warning restore IDE0039

            result = await tcs.Task;
        }

        return result;
    }
}

#pragma warning disable CA1822
