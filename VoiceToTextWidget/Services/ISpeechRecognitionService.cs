namespace VoiceToTextWidget.Services;

public interface ISpeechRecognitionService : IDisposable
{
    Task PreloadAsync();
    Task StartSessionAsync();
    void FeedAudio(byte[] buffer, int bytesRecorded);
    Task<string> FinishSessionAsync(byte[] audioData);
    Task<string> FinishSessionAsync();
}
