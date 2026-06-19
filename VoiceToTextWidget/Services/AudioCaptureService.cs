using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VoiceToTextWidget.Services;

public sealed class AudioCaptureService : IDisposable
{
    private readonly WaveInEvent _waveIn;
    private MemoryStream? _buffer;
    private TaskCompletionSource<byte[]>? _tcs;
    private bool _disposed;
    private bool _volumeSet;

    public event Action<byte[], int>? AudioChunkCaptured;
    
    public AudioCaptureService()
    {
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1),
            DeviceNumber = 0,
            BufferMilliseconds = 100
        };
        
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
    }

    public void StartRecording()
    {
        EnsureVolumeMax();
        _buffer = new MemoryStream();
        _tcs = new TaskCompletionSource<byte[]>();
        _waveIn.StartRecording();
    }
    
    private void EnsureVolumeMax()
    {
        if (_volumeSet) return;
        _volumeSet = true;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            
            var volume = device.AudioEndpointVolume;
            volume.MasterVolumeLevelScalar = 1.0f;
            
            Debug.WriteLine($"[AudioCapture] Microphone volume set to max on: {device.FriendlyName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioCapture] Could not set microphone volume: {ex.Message}");
        }
    }
    
    public async Task<byte[]> StopRecordingAsync()
    {
        _waveIn.StopRecording();
        return await _tcs!.Task;
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _buffer?.Write(e.Buffer, 0, e.BytesRecorded);
        AudioChunkCaptured?.Invoke(e.Buffer, e.BytesRecorded);
    }
    
    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_buffer != null)
        {
            var data = _buffer.ToArray();
            _buffer.Dispose();
            _buffer = null;
            _tcs?.TrySetResult(data);
        }
        else
        {
            _tcs?.TrySetResult(Array.Empty<byte>());
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.RecordingStopped -= OnRecordingStopped;
        _waveIn.Dispose();
        _buffer?.Dispose();
    }
}
