using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ProxmoxSpiceHelper;

var logLevel = new LoggingLevelSwitch
{
    MinimumLevel = LogEventLevel.Debug
};

var configArgument = new Argument<string>("config");
var verbosityOption = new Option<string>(["--verbosity", "-v", "-q"]).FromAmong("Q", "Quiet", "M", "Minimal", "N",
    "Normal", "D", "Detailed", "Diag", "Diagnostic");
var viewerOption = new Option<string>("--viewer");
var rootCommand =
    new RootCommand(
        "A simple command line app to open a SPICE console for a Proxmox-hosted VM without using the web interface.")
    {
        configArgument,
        verbosityOption,
        viewerOption
    };
rootCommand.Name = "ProxmoxSpiceHelper";

rootCommand.SetHandler(Main);
return await rootCommand.InvokeAsync(args);

async Task Main(InvocationContext invocationContext)
{
    Configuration configuration;
    var spiceCommand = string.Empty;
    var spiceFile = string.Empty;

    var configurationPath = invocationContext.ParseResult.GetValueForArgument(configArgument);
    var verbosity = invocationContext.ParseResult.GetValueForOption(verbosityOption);
    var viewer = invocationContext.ParseResult.GetValueForOption(viewerOption);
    switch (verbosity)
    {
        case "Q":
        case "Quiet":
            break;
        case "M":
        case "Minimal":
            logLevel.MinimumLevel = LogEventLevel.Error;
            break;
        case "N":
        case "Normal":
            logLevel.MinimumLevel = LogEventLevel.Warning;
            break;
        case "D":
        case "Detailed":
            logLevel.MinimumLevel = LogEventLevel.Information;
            break;
        case "Diag":
        case "Diagnostic":
            logLevel.MinimumLevel = LogEventLevel.Debug;
            break;
    }

    await using var log = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.ControlledBy(logLevel)
        .CreateLogger();

    log.Debug("Set log level to {Level}", logLevel.MinimumLevel);
    log.Debug(
        "Parameters: config: '{Config}', viewer: '{Viewer}'",
        configurationPath, viewer);


    try
    {
        configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configurationPath))!;
    }
    catch (Exception e)
    {
        log.Fatal(e, "Failed to parse configuration file");
        Environment.Exit(1);
        return;
    }

    if (string.IsNullOrWhiteSpace(configuration.Node))
    {
        configuration.Node = configuration.Host.Split('.')[0];
        log.Information("No node specified, assuming hostname of host ('{Host}' -> '{Hostname}')", configuration.Host,
            configuration.Node);
    }

    var api = new Proxmox(configuration);
    log.Debug("Created Proxmox API connector");

    try
    {
        spiceCommand = await api.GetSpiceCommand();
    }
    catch (Exception e)
    {
        log.Fatal(e, "Failed to get SPICE command");
        Environment.Exit(1);
    }

    try
    {
        spiceFile = Path.GetTempFileName();
        File.WriteAllText(spiceFile, spiceCommand);
    }
    catch (Exception e)
    {
        log.Fatal(e, "Failed to write temp file");
        Environment.Exit(1);
    }

    try
    {
        Process.Start(
            string.IsNullOrWhiteSpace(viewer)
                ? Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? FileExtensionInfo.GetInformation(FileExtensionInfo.AssocStr.Executable, ".vv")
                    : "virt-viewer" // Just assume this goes somewhere.
                : viewer, spiceFile);
    }
    catch (Exception e)
    {
        log.Fatal(e, "Failed to start virt-viewer");
        Environment.Exit(1);
    }
}