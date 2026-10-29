using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Qianhai.ControlPad
{
    [Serializable]
    public sealed class ThumbnailManifest
    {
        public string targetId;
        public MediaItem[] items = Array.Empty<MediaItem>();

        [Serializable]
        public sealed class MediaItem
        {
            public string id;
            public string title;
            public string thumbnailUrl;
            public string mediaType = "video";
        }

        public static IEnumerator Fetch(string url, Action<ThumbnailManifest, string> completed)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                completed?.Invoke(null, "未配置缩略图清单 URL");
                yield break;
            }

            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed?.Invoke(null, request.error);
                    yield break;
                }

                try
                {
                    var manifest = JsonUtility.FromJson<ThumbnailManifest>(request.downloadHandler.text);
                    if (manifest == null || manifest.items == null)
                        completed?.Invoke(null, "清单格式不正确：需要包含 items 数组");
                    else
                        completed?.Invoke(manifest, null);
                }
                catch (Exception exception)
                {
                    completed?.Invoke(null, "清单解析失败：" + exception.Message);
                }
            }
        }
    }
}
