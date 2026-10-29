using System;
using UnityEngine;

namespace Qianhai.ControlPad
{
    [CreateAssetMenu(menuName = "Qianhai/Control Pad Config", fileName = "ControlPadConfig")]
    public sealed class ControlPadConfig : ScriptableObject
    {
        [Serializable]
        public sealed class UdpEndpoint
        {
            public string id = "core-movie";
            public string displayName = "核心影片电脑";
            public string host = "192.168.1.107";
            [Min(1)] public int port = 5005;
        }

        [Serializable]
        public sealed class PlaybackTarget
        {
            public string id = "layout-3";
            public string displayName = "布局3";
            public string host = "192.168.1.101";
            [Min(1)] public int port = 5005;
            [Tooltip("建议填写局域网 HTTP JSON 清单地址；不要填 SMB 目录地址。")]
            public string thumbnailManifestUrl = "http://192.168.1.101:8080/manifest.json";
        }

        [Header("六台播放电脑")]
        [SerializeField] private PlaybackTarget[] targets = Array.Empty<PlaybackTarget>();

        [Header("核心影片播放电脑（独立目标，无缩略图清单）")]
        [SerializeField] private UdpEndpoint coreMovieTarget = new UdpEndpoint();

        public PlaybackTarget[] Targets => targets;
        public UdpEndpoint CoreMovieTarget => coreMovieTarget;

        public PlaybackTarget GetTarget(string id)
        {
            if (targets == null) return null;
            foreach (var target in targets)
                if (target != null && string.Equals(target.id, id, StringComparison.OrdinalIgnoreCase))
                    return target;
            return null;
        }

        public bool TryGetUdpEndpoint(string id, out string host, out int port)
        {
            if (coreMovieTarget != null && string.Equals(coreMovieTarget.id, id, StringComparison.OrdinalIgnoreCase))
            {
                host = coreMovieTarget.host;
                port = coreMovieTarget.port;
                return true;
            }

            var target = GetTarget(id);
            if (target != null)
            {
                host = target.host;
                port = target.port;
                return true;
            }

            host = string.Empty;
            port = 0;
            return false;
        }
    }
}
