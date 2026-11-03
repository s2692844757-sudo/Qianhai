using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Qianhai.ControlPad
{
    public sealed class UdpCommandClient : MonoBehaviour
    {
        [SerializeField] private ControlPadConfig config;
        private UdpClient client;
        private readonly SemaphoreSlim sendGate = new SemaphoreSlim(1, 1);
        private bool shuttingDown;
        private string language = "zh-CN";
        private readonly Dictionary<string, string> currentContentValueByTarget = new Dictionary<string, string>();

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

        public void BroadcastLanguage(string value)
        {
            if (value != "zh-CN" && value != "en-US") return;
            SetLanguage(value);
            if (config == null) return;
            if (config.Targets != null)
                foreach (var target in config.Targets)
                    if (target != null)
                        Send(target.id, "language", value: GetCurrentContentValue(target.id));
            if (config.CoreMovieTarget != null)
                Send(config.CoreMovieTarget.id, "language", value: GetCurrentContentValue(config.CoreMovieTarget.id));
        }

        public string GetCurrentContentValue(string targetId)
        {
            return !string.IsNullOrEmpty(targetId) && currentContentValueByTarget.TryGetValue(targetId, out var value)
                ? value : string.Empty;
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

            if (shuttingDown) return false;
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
                var endpoint = new IPEndPoint(address, port);
                SendPacketAsync(bytes, endpoint, targetId, command);
                if (command == "select-category")
                    currentContentValueByTarget[targetId] = packet.value;
                else if (command == "play")
                    currentContentValueByTarget[targetId] = string.IsNullOrEmpty(packet.assetId) ? packet.value : packet.assetId;
                else if (command == "back" || command == "select-layout")
                    currentContentValueByTarget.Remove(targetId);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("UDP 发送失败：" + exception.Message, this);
                return false;
            }
        }

        private async void SendPacketAsync(byte[] bytes, IPEndPoint endpoint, string targetId, string command)
        {
            bool entered = false;
            try
            {
                await Task.Yield();
                await sendGate.WaitAsync();
                entered = true;
                if (shuttingDown || client == null) return;
                await client.SendAsync(bytes, bytes.Length, endpoint);
                Debug.Log($"UDP → {targetId} {endpoint} {command}", this);
            }
            catch (Exception exception)
            {
                if (!shuttingDown)
                    Debug.LogError("UDP 发送失败：" + exception.Message, this);
            }
            finally
            {
                if (entered) sendGate.Release();
            }
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            client?.Close();
            client = null;
        }
    }
}
