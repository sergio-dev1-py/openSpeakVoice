#r "C:\Users\STEINER1TB\.nuget\packages\whisper.net\1.9.0\lib\net8.0\Whisper.net.dll"
#r "C:\Users\STEINER1TB\.nuget\packages\whisper.net.runtime\1.9.0\lib\net8.0\Whisper.net.Runtime.dll"
#r "C:\Users\STEINER1TB\.nuget\packages\whisper.net.runtime.vulkan\1.9.0\lib\net8.0\Whisper.net.Runtime.Vulkan.dll"
#r "C:\Users\STEINER1TB\.nuget\packages\whisper.net.runtime.noavx\1.9.0\lib\net8.0\Whisper.net.Runtime.NoAvx.dll"

using Whisper.net;
using Whisper.net.LibraryLoader;

Console.WriteLine("=== Whisper.net Test ===");

RuntimeOptions.RuntimeLibraryOrder = new List<RuntimeLibrary>
{
    RuntimeLibrary.Vulkan,
    RuntimeLibrary.Cpu
};

Console.WriteLine($"Runtime order: {string.Join(", ", RuntimeOptions.RuntimeLibraryOrder)}");

var modelPath = @"C:\Users\STEINER1TB\AppData\Local\VoiceToTextWidget\Models\ggml-base.bin";
Console.WriteLine($"Model path: {modelPath}");
Console.WriteLine($"Model exists: {File.Exists(modelPath)}");
Console.WriteLine($"Model size: {new FileInfo(modelPath).Length / (1024 * 1024)} MB");

try
{
    Console.WriteLine("Attempting WhisperFactory.FromPath with UseGpu=false...");
    using var factory = WhisperFactory.FromPath(modelPath, new WhisperFactoryOptions { UseGpu = false });
    Console.WriteLine("SUCCESS: Model loaded with UseGpu=false");
}
catch (Exception ex)
{
    Console.WriteLine($"FAILED (UseGpu=false): {ex.GetType().Name}: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    if (ex.InnerException?.InnerException != null)
        Console.WriteLine($"  Inner2: {ex.InnerException.InnerException.GetType().Name}: {ex.InnerException.InnerException.Message}");
}

try
{
    Console.WriteLine("Attempting WhisperFactory.FromPath with UseGpu=true...");
    using var factory = WhisperFactory.FromPath(modelPath, new WhisperFactoryOptions { UseGpu = true });
    Console.WriteLine("SUCCESS: Model loaded with UseGpu=true");
}
catch (Exception ex)
{
    Console.WriteLine($"FAILED (UseGpu=true): {ex.GetType().Name}: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    if (ex.InnerException?.InnerException != null)
        Console.WriteLine($"  Inner2: {ex.InnerException.InnerException.GetType().Name}: {ex.InnerException.InnerException.Message}");
}
