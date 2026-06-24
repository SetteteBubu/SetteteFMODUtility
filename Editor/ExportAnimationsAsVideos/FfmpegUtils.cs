using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class FfmpegUtils
{
    private static string FfmpegPath
    {
        get
        {
            //Git package flow
            var packageInfo = UnityEditor.PackageManager.PackageInfo
                .FindForAssembly(typeof(FfmpegUtils).Assembly);

            string ffmpegExe = Application.platform == RuntimePlatform.WindowsEditor
                ? "ffmpeg.exe"
                : "ffmpeg";

            if (packageInfo != null)
                return Path.Combine(packageInfo.resolvedPath, "Plugins", "FFMPEG", "bin", ffmpegExe);

            //Development project flow
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            return Path.Combine(
                            projectRoot,
                            "Assets",
                            "Plugins",
                            "com.settete.settete-fmod-utility",
                            "Plugins",
                            "FFMPEG",
                            "bin",
                            "ffmpeg.exe");
        }
    }

    public static void StitchToMp4(string frameFolder, string outputMp4, int fps)
    {
        string inputPattern = Path.Combine(frameFolder, "frame_%05d.png");

        // H.264, yuv420p for max compatibility (Premiere, DaVinci, QuickTime all happy)
        string args = $"-y -framerate {fps} -i \"{inputPattern}\" " +
                      $"-c:v libx264 -pix_fmt yuv420p -crf 18 " +
                      $"-movflags +faststart \"{outputMp4}\"";

        var psi = new ProcessStartInfo(FfmpegPath, args)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        string err = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
            UnityEngine.Debug.LogError($"FFmpeg failed for {outputMp4}:\n{err}");

        AssetDatabase.DeleteAsset(frameFolder);
    }
}