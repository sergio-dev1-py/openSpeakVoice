using System;
using System.Windows.Forms;
using VoiceToTextWidget.Forms;
using VoiceToTextWidget.Services;

namespace VoiceToTextWidget;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var settingsService = new SettingsService();

        using var audioCapture = new AudioCaptureService();
        using var speechRecognition = new SpeechRecognitionService(settingsService);
        using var textInjection = new TextInjectionService();

        using var widgetForm = new MainWidgetForm(
            settingsService,
            audioCapture,
            speechRecognition,
            textInjection);

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
