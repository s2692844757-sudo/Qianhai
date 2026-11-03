using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Qianhai.ControlPad
{
    public sealed class UdpCommandClient : MonoBehaviour
    {
        [SerializeField] private ControlPadConfig config;
        private UdpClient client;
        private string language = "zh-CN";

        [Serializable]
        private sealed class CommandPacket
        {
            public string command;
            public string targetId;
            public string assetId;
            public string value;
            public string language;
            public long sentAtUnixMs;
        }

        private void Awake()
        {
            try { client = new UdpClient(); }
            catch (SocketException exception) { Debug.LogError("UDP 初始化失败：" + exception.Message, this); }
        }

        public void Configure(ControlPadConfig value) => config = value;

        public void SetLanguage(string value)
        {
            if (value == "zh-CN" || value == "en-US") language = value;
        }

        public bool Send(string targetId, string command, string assetId = "", string value = "")
        {
            if (config == null || !config.TryGetUdpEndpoint(targetId, out var host, out var port))
            {
                Debug.LogWarning("没有配置 UDP 目标：" + targetId, this);
                return false;
            }

            if (!IPAddress.TryParse(host, out var address))
            {
                Debug.LogWarning("目标 IP 格式不正确：" + host, this);
                return false;
            }

            if (client == null) client = new UdpClient();
            var packet = new CommandPacket
            {
                command = command,
                targetId = targetId,
                assetId = assetId ?? string.Empty,
                value = value ?? string.Empty,
                language = language,
                sentAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
            try
            {
                client.Send(bytes, bytes.Length, new IPEndPoint(address, port));
                Debug.Log($"UDP → {targetId} {address}:{port} {command}", this);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("UDP 发送失败：" + exception.Message, this);
                return false;
            }
        }

        private void OnDestroy()
        {
            client?.Close();
            client = null;
        }
    }
}
