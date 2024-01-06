using System;
using System.IO;
using Flurl.Http;
using System.Text;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WonderLab.Classes.Enums;
using WonderLab.Classes.Extension;

namespace WonderLab.Services;

/// <summary>
/// 自动更新服务类
/// </summary>
public class UpdateService {
    private DataService _dataManager;
    private readonly string _baseUrl = "http://s2.fxidc.net:2999/api/update/";

    public JsonNode UpdateInfoJsonNode { get; set; }

    public UpdateService(DataService dataService) {
        _dataManager = dataService;
    }

    public async Task InitAsync() {
        string branch = _dataManager.ConfigData.Branch switch {
            BranchType.Lsaac => "lsaac",
            BranchType.Albert => "albert",
            _ => "lsaac"
        };

        string url = $"{_baseUrl}{branch}";
        if (string.IsNullOrEmpty(url)) {
            return;
        }

        try {
            UpdateInfoJsonNode = await JsonNode.ParseAsync(await url
                .GetStreamAsync()) ?? default!;
        }
        catch (Exception) {
            return;
        }
    }

    public async ValueTask<bool> CheckAsync() {
        await InitAsync();
        if (UpdateInfoJsonNode is null) {
            return false;
        }

        int remoteVersion = UpdateInfoJsonNode["version"]!
            .GetValue<string>()
            .Replace(".", "")
            .ToInt();

        int localVersion = _dataManager.Version
            .Replace(".", "")
            .Substring(0, 3)
            .ToInt();

        return remoteVersion > localVersion;
    }

    public void Update() {
        var currentProcess = Process.GetCurrentProcess();
        string name = currentProcess.ProcessName,
            filename = $"{name}.exe";

        var psCommand = new StringBuilder()
            .AppendLine($"Stop-Process -Id {currentProcess.Id} -Force")
            .AppendLine("Wait-Process -Id {currentProcess.Id} -ErrorAction SilentlyContinue")
            .AppendLine("Start-Sleep -Milliseconds 500")
            .AppendLine("Remove-Item updateTemp.zip -Force")
            .AppendLine($"Remove-Item {filename} -Force")
            .AppendLine($"Rename-Item launcher.temp {filename}")
            .AppendLine($"Start-Process {name}.exe -Args updated");

        try {
            using var process = Process.Start(new ProcessStartInfo {
                UseShellExecute = true,
                FileName = "powershell.exe",
                Arguments = psCommand.ToString(),
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Directory.GetCurrentDirectory(),
            });
        }
        catch (Exception) { }
    }
}