using UnityEditor;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Menu Tools/Reconnect Profiler: chạy adb forward và adb reverse để Profiler kết nối lại
    /// với app Android đang chạy (cổng 34999 trên máy, 34998 trên thiết bị). Chỉ hoạt động trên Windows (cmd.exe).
    /// </summary>
    public static class ReconnectToProfiler
    {
        [MenuItem("Tools/" + "Reconnect Profiler")]
        public static void ReconnectProfiler()
        {
            Debug.Log($"bunder ver :{Application.identifier}");
            runCMD($"/C adb forward tcp:34999 localabstract:Unity-{Application.identifier}");
            runCMD($"/C adb reverse tcp:34998 tcp:34999");

            static void runCMD(string cmd)
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = cmd;
                process.StartInfo = startInfo;
                process.Start();
            }
        }
    }
}
