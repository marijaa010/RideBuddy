using System.Diagnostics;

namespace RideBuddy.E2E.Tests.Smoke;

public class RideCommunicationSmokeTests
{
    [Fact]
    public async Task Ride_Communication_Smoke_Test_Should_Pass()
    {
        await RunSmokeScriptAsync();
    }

    [Fact]
    public async Task Ride_Communication_Cancellation_Compensation_Smoke_Test_Should_Pass()
    {
        await RunSmokeScriptAsync("-IncludeCancellationCheck");
    }

    [Fact]
    public async Task Ride_Rest_Endpoints_Smoke_Test_Should_Pass()
    {
        await RunSmokeScriptAsync("-IncludeRideEndpointChecks");
    }

    [Fact]
    public async Task Ride_Role_Authorization_Smoke_Test_Should_Pass()
    {
        await RunSmokeScriptAsync("-IncludeRoleAuthorizationChecks");
    }

    private static async Task RunSmokeScriptAsync(string additionalArguments = "")
    {
        if (!ShouldRunSmokeTests())
        {
            return;
        }

        var repoRoot = FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "scripts", "smoke-test-ride-communication.ps1");

        File.Exists(scriptPath)
            .Should()
            .BeTrue($"Smoke test script was not found: {scriptPath}");

        var shell = ResolvePowerShellExecutable();
        var scriptArguments = string.IsNullOrWhiteSpace(additionalArguments)
            ? ""
            : " " + additionalArguments;

        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ComposeProjectRoot \"{repoRoot}\"{scriptArguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = repoRoot
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var completed = await Task.Run(() => process.WaitForExit(15 * 60 * 1000));
        if (!completed)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Smoke test timed out after 15 minutes.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        process.ExitCode.Should().Be(0,
            $"Smoke test script failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }

    private static bool ShouldRunSmokeTests()
    {
        var enabled = Environment.GetEnvironmentVariable("RUN_RIDEBUDDY_SMOKE_TESTS");
        return string.Equals(enabled, "1", StringComparison.Ordinal);
    }

    private static string ResolvePowerShellExecutable()
    {
        var candidates = new[] { "pwsh", "powershell" };

        foreach (var candidate in candidates)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-NoProfile -Command \"$PSVersionTable.PSVersion\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    continue;
                }

                process.WaitForExit(2000);
                if (process.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch
            {
                // Try next shell executable.
            }
        }

        throw new InvalidOperationException("Neither 'pwsh' nor 'powershell' is available on PATH.");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var composePath = Path.Combine(current.FullName, "docker-compose.yml");
            if (File.Exists(composePath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root with docker-compose.yml.");
    }
}
