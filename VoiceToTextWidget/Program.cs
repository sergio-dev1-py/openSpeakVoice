using System;
using System.Diagnostics;
using System.Windows.Forms;
using VoiceToTextWidget.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Services;
using Whisper.net.LibraryLoader;
using Whisper.net.Logger;

namespace VoiceToTextWidget;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        RuntimeOptions.RuntimeLibraryOrder = new List<RuntimeLibrary>
        {
            RuntimeLibrary.Cuda,
            RuntimeLibrary.Cpu,
            RuntimeLibrary.CpuNoAvx
        };

        LogProvider.AddLogger((level, message) =>
        {
            var logLine = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            Debug.WriteLine($"[Whisper.NET native] {logLine}");
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "whisper_debug.log"), logLine + Environment.NewLine); } catch { }
        });

        var settingsService = new SettingsService();
        var apiKeyManager = new ApiKeyManager(settingsService);

        using var audioCapture = new AudioCaptureService();
        using var groqService = new GroqApiSpeechService(settingsService, apiKeyManager);
        using var localService = new WhisperLocalSpeechService(settingsService);
        using var textInjection = new TextInjectionService();

        using var widgetForm = new MainWidgetForm(
            settingsService,
            audioCapture,
            groqService,
            localService,
            textInjection,
            apiKeyManager);

        var hotKeyService = new HotKeyService(settingsService.Settings);

        if (!hotKeyService.IsRegistered)
        {
            MessageBox.Show(
                "No se pudo activar el listener de teclado global.\n" +
                "Intenta ejecutar como administrador.",
                "VoiceToText - Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        widgetForm.SetHotKeyService(hotKeyService);

        Application.Run(widgetForm);
        
        hotKeyService.Dispose();
    }


}
