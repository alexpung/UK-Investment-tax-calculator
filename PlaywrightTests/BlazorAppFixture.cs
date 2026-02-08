using System.Diagnostics;
using System.Net;
using System.Net.Http;
using NUnit.Framework;

namespace PlaywrightTests;

/// <summary>
/// Assembly-level fixture that starts the Blazor app before any tests run
/// and stops it after all tests complete.
/// </summary>
[SetUpFixture]
public class BlazorAppFixture
{
    private static Process? _appProcess;
    private static readonly string AppUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";

    [OneTimeSetUp]
    public async Task StartBlazorApp()
    {
        // Skip if BASE_URL is set (CI environment where app is started separately)
        if (Environment.GetEnvironmentVariable("BASE_URL") != null)
        {
            Console.WriteLine($"Using externally hosted app at {AppUrl}");
            await WaitForAppToBeReady();
            return;
        }

        Console.WriteLine("Starting Blazor app...");

        var projectPath = FindProjectPath();
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls \"{AppUrl}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        _appProcess = Process.Start(startInfo);

        if (_appProcess == null)
        {
            throw new Exception("Failed to start Blazor app process");
        }

        // Wait for app to be ready
        await WaitForAppToBeReady();
        
        Console.WriteLine("Blazor app started successfully");
    }

    [OneTimeTearDown]
    public void StopBlazorApp()
    {
        if (_appProcess == null) return;
        
        try
        {
            if (!_appProcess.HasExited)
            {
                Console.WriteLine("Stopping Blazor app...");
                _appProcess.Kill(entireProcessTree: true);
            }
            _appProcess.Dispose();
        }
        catch (InvalidOperationException ex)
        {
            // Process already exited between HasExited check and Kill - this is fine
            Console.WriteLine($"Process cleanup race condition (expected): {ex.Message}");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Process access error - log and continue
            Console.WriteLine($"Process cleanup error: {ex.Message}");
        }
        finally
        {
            _appProcess = null;
        }
    }

    private static string FindProjectPath()
    {
        // Find the solution directory by walking up from the test output directory
        var currentDir = AppContext.BaseDirectory;
        
        while (currentDir != null)
        {
            var solutionFile = Path.Combine(currentDir, "CapitalGainCalculator.sln");
            if (File.Exists(solutionFile))
            {
                return Path.Combine(currentDir, "BlazorApp-Investment Tax Calculator", "InvestmentTaxCalculator.csproj");
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        throw new Exception("Could not find solution directory. Make sure tests are run from within the solution.");
    }

    private static async Task WaitForAppToBeReady()
    {
        using var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var httpClient = new HttpClient(httpClientHandler);
        
        var maxWaitTime = TimeSpan.FromSeconds(60);
        var checkInterval = TimeSpan.FromMilliseconds(500);
        var perRequestTimeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                using var cts = new CancellationTokenSource(perRequestTimeout);
                var response = await httpClient.GetAsync(AppUrl, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // App not ready yet - connection refused or similar
            }
            catch (TaskCanceledException)
            {
                // Request timed out - app not ready yet
            }

            await Task.Delay(checkInterval);
        }

        throw new Exception($"Blazor app did not start within {maxWaitTime.TotalSeconds} seconds");
    }
}
