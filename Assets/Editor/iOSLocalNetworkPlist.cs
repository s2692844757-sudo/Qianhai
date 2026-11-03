#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Qianhai.EditorTools
{
    public static class iOSLocalNetworkPlist
    {
        [PostProcessBuild(1000)]
        public static void AddLocalNetworkPermissions(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath)) return;

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetString("NSLocalNetworkUsageDescription", "用于读取展厅局域网内的素材缩略图，并向播放电脑发送控制指令。");

            PlistElementDict transportSecurity;
            if (root.values.TryGetValue("NSAppTransportSecurity", out var existing))
                transportSecurity = existing.AsDict();
            else
                transportSecurity = root.CreateDict("NSAppTransportSecurity");
            transportSecurity.SetBoolean("NSAllowsLocalNetworking", true);

            plist.WriteToFile(plistPath);
        }
    }
}
#endif
