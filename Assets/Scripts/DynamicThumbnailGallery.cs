using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Qianhai.ControlPad;
using Qianhai.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Qianhai.ControlPad
{
    public sealed class DynamicThumbnailGallery : MonoBehaviour
    {
        [SerializeField] private ControlPadConfig config;
        [SerializeField] private UdpCommandClient udpClient;
        [SerializeField] private RectTransform content;
        [SerializeField] private ThumbnailCardView cardTemplate;
        [SerializeField] private RectTransform imageContent;
        [SerializeField] private ThumbnailCardView imageCardTemplate;
        [SerializeField] private Image selectedPreview;
        [SerializeField] private Image selectedThumbnailOverlay;
        [SerializeField] private Text statusLabel;
        [SerializeField] private string targetId = "layout-3";
        [SerializeField] private string mediaTypeFilter = "image";
        [SerializeField] private Sprite imageTabSelected;
        [SerializeField] private Sprite imageTabUnselected;
        [SerializeField] private Sprite videoTabSelected;
        [SerializeField] private Sprite videoTabUnselected;
        [SerializeField] private Sprite headerChineseSelected;
        [SerializeField] private Sprite headerChineseUnselected;
        [SerializeField] private Sprite headerEnglishSelected;
        [SerializeField] private Sprite headerEnglishUnselected;
        [SerializeField] private Sprite muteActionSprite;
        [SerializeField] private Sprite amplifierActionSprite;
        private readonly List<GameObject> cards = new List<GameObject>();
        private ThumbnailManifest.MediaItem selectedItem;
        private GameObject imageModeRoot;
        private GameObject videoModeRoot;
        private Text selectedTitleLabel;
        private Button imageTabButton;
        private Button videoTabButton;
        private Image headerChineseArtwork;
        private Image headerEnglishArtwork;
        private string language = "zh-CN";
        private float volume = 1f;
        private bool muted;
        private int previewVersion;
        private Sprite ownedPreviewSprite;

        private void Awake()
        {
            imageModeRoot = transform.Find("ImageModeRoot")?.gameObject;
            videoModeRoot = transform.Find("VideoModeRoot")?.gameObject;
            selectedTitleLabel = transform.Find("VideoModeRoot/SelectedTitleLabel")?.GetComponent<Text>();
            imageTabButton = transform.Find("ImagesTabButton")?.GetComponent<Button>();
            videoTabButton = transform.Find("VideosTabButton")?.GetComponent<Button>();
            headerChineseArtwork = transform.Find("HeaderLanguageRoot/ChineseButton/Artwork")?.GetComponent<Image>();
            headerEnglishArtwork = transform.Find("HeaderLanguageRoot/EnglishButton/Artwork")?.GetComponent<Image>();

            WireControl("PlayCommandButton", () => SendSelectedControl("play"));
            WireControl("PauseCommandButton", () => SendSelectedControl("pause"));
            WireControl("ReplayCommandButton", () => SendSelectedControl("replay"));
            WireControl("VolumeUpCommandButton", () => ChangeVolume(.1f));
            WireControl("VolumeDownCommandButton", () => ChangeVolume(-.1f));
            WireControl("MuteCommandButton", ToggleMute);
            var audioRoot = transform.Find("VideoModeRoot/VideoControlsRoot");
            var muteImage = audioRoot?.Find("MuteCommandButton")?.GetComponent<Image>();
            var amplifierImage = audioRoot?.Find("AmplifierCommandButton")?.GetComponent<Image>();
            if (muteActionSprite == null && muteImage != null) muteActionSprite = muteImage.sprite;
            if (amplifierActionSprite == null && amplifierImage != null) amplifierActionSprite = amplifierImage.sprite;
            if (amplifierImage != null) amplifierImage.gameObject.SetActive(false);
            ApplyAudioVisuals();
            ApplyModeLayout();
        }

        private void LateUpdate()
        {
            if (mediaTypeFilter == "video")
            {
                UpdateVideoCardOpacity();
                return;
            }
            if (mediaTypeFilter != "image" || imageContent == null) return;
            var scroll = imageContent.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return;
            ThumbnailCardView nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var card in cards)
            {
                if (card == null) continue;
                var center = scroll.viewport.InverseTransformPoint(card.transform.position);
                var view = card.GetComponent<ThumbnailCardView>();
                float distance = Mathf.Abs(center.x);
                if (view != null && distance < nearestDistance)
                {
                    nearest = view;
                    nearestDistance = distance;
                }
            }
            if (nearest == null || nearest.MediaItem == null) return;
            if (selectedItem == null || nearest.AssetId != selectedItem.id)
                UpdateSelection(nearest.MediaItem, false);
            // Selection updates button visuals; restore continuous carousel scaling afterwards.
            foreach (var card in cards)
            {
                if (card == null) continue;
                float distance = Mathf.Abs(scroll.viewport.InverseTransformPoint(card.transform.position).x);
                card.GetComponent<ThumbnailCardView>()?.SetCarouselDistance(distance / (scroll.viewport.rect.width * .5f));
            }
            if (selectedThumbnailOverlay != null)
            {
                // The foreground copy only supplies overlap ordering. It follows the actual
                // moving card, rather than occupying a fixed slot in the middle of the strip.
                var foreground = selectedThumbnailOverlay.transform.parent as RectTransform;
                var cardRect = nearest.transform as RectTransform;
                if (foreground != null && cardRect != null)
                {
                    foreground.sizeDelta = cardRect.rect.size;
                    foreground.position = cardRect.TransformPoint(cardRect.rect.center);
                    foreground.rotation = cardRect.rotation;
                    var parentScale = foreground.parent.lossyScale;
                    var cardScale = cardRect.lossyScale;
                    foreground.localScale = new Vector3(cardScale.x / parentScale.x, cardScale.y / parentScale.y, 1f);
                }
            }
            if (nearest.ThumbnailSprite != null && selectedPreview != null && selectedPreview.sprite != nearest.ThumbnailSprite)
            {
                ++previewVersion;
                ReleasePreview();
                selectedPreview.sprite = nearest.ThumbnailSprite;
                if (selectedThumbnailOverlay != null) selectedThumbnailOverlay.sprite = nearest.ThumbnailSprite;
            }
        }

        private void UpdateVideoCardOpacity()
        {
            var scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null || scroll.viewport == null) return;
            foreach (var card in cards)
            {
                if (card == null) continue;
                var group = card.GetComponent<CanvasGroup>();
                if (group == null) continue;
                float distance = Mathf.Abs(scroll.viewport.InverseTransformPoint(card.transform.position).y);
                float edge = Mathf.Clamp01(distance / (scroll.viewport.rect.height * .5f));
                group.alpha = Mathf.Lerp(1f, .2f, edge * edge);
            }
        }

        public void Configure(ControlPadConfig source, UdpCommandClient client, string id)
        {
            config = source;
            udpClient = client;
            targetId = id;
            // A new navigation entry always starts on the animation/video tab.
            mediaTypeFilter = "video";
            ApplyModeLayout();
            Refresh();
        }

        public void SetMediaType(string mediaType)
        {
            mediaTypeFilter = string.Equals(mediaType, "image", StringComparison.OrdinalIgnoreCase) ? "image" : "video";
            selectedItem = null;
            ApplyModeLayout();
            Refresh();
        }

        public void Refresh()
        {
            StopAllCoroutines();
            StartCoroutine(LoadManifest());
        }

        public void ConfirmSelection()
        {
            if (selectedItem == null)
            {
                SetStatus("请先选择素材");
                return;
            }
            if (udpClient != null && udpClient.Send(targetId, "play", selectedItem.id))
                SetStatus("已发送播放指令：" + selectedItem.title);
            else
                SetStatus("发送失败，请检查目标电脑网络配置");
        }

        private IEnumerator LoadManifest()
        {
            ClearCards();
            var target = config != null ? config.GetTarget(targetId) : null;
            if (target == null)
            {
                SetStatus("未配置目标电脑：" + targetId);
                yield break;
            }
            SetStatus("正在读取缩略图…");
            yield return ThumbnailManifest.Fetch(target.thumbnailManifestUrl, (manifest, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    SetStatus("读取失败：" + error);
                    return;
                }
                Populate(manifest);
            });
        }

        private void Populate(ThumbnailManifest manifest)
        {
            selectedItem = null;
            if (manifest == null || manifest.items == null) return;
            bool imageMode = mediaTypeFilter == "image";
            var destination = imageMode ? imageContent : content;
            var template = imageMode ? imageCardTemplate : cardTemplate;
            if (destination == null || template == null)
            {
                SetStatus("素材列表尚未绑定，请检查 DetailPanel 的 Gallery 配置");
                return;
            }
            foreach (var item in manifest.items)
            {
                if (item == null || (mediaTypeFilter.Length > 0 &&
                    !string.Equals(item.mediaType, mediaTypeFilter, StringComparison.OrdinalIgnoreCase))) continue;
                var card = Instantiate(template, destination);
                card.gameObject.SetActive(true);
                card.Bind(item, SelectItem);
                cards.Add(card.gameObject);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(destination);
            var scroll = destination.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.normalizedPosition = new Vector2(0, 1);
            SetStatus(cards.Count == 0 ? (imageMode ? "暂无图片素材" : "暂无动画素材，可切换图片查看") : "");
            if (cards.Count > 0)
            {
                var firstCard = cards[0].GetComponent<ThumbnailCardView>();
                if (firstCard != null) UpdateSelection(firstCard.MediaItem, false);
            }
        }

        private void SelectItem(ThumbnailManifest.MediaItem item)
        {
            UpdateSelection(item, true);
            // A deliberate video-card tap uses the same `play` command as the
            // Play control; S/ in assetId identifies the selected video.
            if (item != null && string.Equals(item.mediaType, "video", StringComparison.OrdinalIgnoreCase))
                SendSelectedControl("play");
        }

        private void UpdateSelection(ThumbnailManifest.MediaItem item, bool centerOnSelection)
        {
            selectedItem = item;
            SetStatus("");
            foreach (var card in cards)
                if (card != null) card.GetComponent<ThumbnailCardView>()?.SetSelected(item.id);
            if (centerOnSelection && mediaTypeFilter == "video" && content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                var selectedCard = cards.Find(card => card != null && card.GetComponent<ThumbnailCardView>().AssetId == item.id);
                var scroll = content.GetComponentInParent<ScrollRect>();
                if (selectedCard != null && scroll != null && scroll.viewport != null)
                {
                    var rect = selectedCard.GetComponent<RectTransform>();
                    float center = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                    var position = content.anchoredPosition;
                    position.y = Mathf.Clamp(position.y - center, 0, Mathf.Max(0, content.rect.height - scroll.viewport.rect.height));
                    content.anchoredPosition = position;
                    scroll.StopMovement();
                }
            }
            if (centerOnSelection && mediaTypeFilter == "image" && imageContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(imageContent);
                var selectedCard = cards.Find(card => card != null && card.GetComponent<ThumbnailCardView>().AssetId == item.id);
                var scroll = imageContent.GetComponentInParent<ScrollRect>();
                if (selectedCard != null && scroll != null)
                {
                    var center = scroll.viewport.InverseTransformPoint(selectedCard.transform.TransformPoint(selectedCard.GetComponent<RectTransform>().rect.center));
                    var position = imageContent.anchoredPosition;
                    position.x = Mathf.Clamp(position.x - center.x, Mathf.Min(0, scroll.viewport.rect.width - imageContent.rect.width), 0);
                    imageContent.anchoredPosition = position;
                    scroll.StopMovement();
                }
            }
            if (selectedTitleLabel != null) selectedTitleLabel.text = $"— {item.title} —";
            if (mediaTypeFilter == "image" && selectedPreview != null)
            {
                var view = cards.Find(card => card != null && card.GetComponent<ThumbnailCardView>().AssetId == item.id)?.GetComponent<ThumbnailCardView>();
                ++previewVersion;
                if (view != null && view.ThumbnailSprite != null)
                {
                    ReleasePreview();
                    selectedPreview.sprite = view.ThumbnailSprite;
                    if (selectedThumbnailOverlay != null) selectedThumbnailOverlay.sprite = view.ThumbnailSprite;
                }
                else StartCoroutine(LoadPreview(item.thumbnailUrl, previewVersion));
            }
        }

        private IEnumerator LoadPreview(string url, int version)
        {
            using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (version != previewVersion) yield break;
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    ReleasePreview();
                    ownedPreviewSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                    selectedPreview.sprite = ownedPreviewSprite;
                    if (selectedThumbnailOverlay != null) selectedThumbnailOverlay.sprite = selectedPreview.sprite;
                }
                else SetStatus("预览加载失败：" + request.error);
            }
        }

        private void ApplyModeLayout()
        {
            bool imageMode = mediaTypeFilter == "image";
            if (imageModeRoot != null) imageModeRoot.SetActive(imageMode);
            if (videoModeRoot != null) videoModeRoot.SetActive(!imageMode);

            SetButtonSprite(imageTabButton, imageMode ? imageTabSelected : imageTabUnselected);
            SetButtonSprite(videoTabButton, imageMode ? videoTabUnselected : videoTabSelected);
        }

        private static void SetButtonSprite(Button button, Sprite sprite)
        {
            if (button != null && sprite != null && button.targetGraphic is Image image) image.sprite = sprite;
        }

        private void WireControl(string childName, UnityEngine.Events.UnityAction action)
        {
            var button = transform.Find("VideoModeRoot/VideoControlsRoot/" + childName)?.GetComponent<Button>();
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private bool SendSelectedControl(string command, string value = "")
        {
            if (selectedItem == null)
            {
                SetStatus("请先选择素材");
                return false;
            }
            if (udpClient != null && udpClient.Send(targetId, command, selectedItem.id, value))
            {
                SetStatus("已发送：" + command + " · " + selectedItem.title);
                if (command == "play" || command == "pause" || command == "replay")
                {
                    SetControlHighlight("PlayCommandButton", command != "pause");
                    SetControlHighlight("PauseCommandButton", command == "pause");
                }
                return true;
            }
            SetStatus("发送失败，请检查目标电脑网络配置");
            return false;
        }

        private void SetControlHighlight(string name, bool selected)
        {
            var button = transform.Find("VideoModeRoot/VideoControlsRoot/" + name)?.GetComponent<Button>();
            if (button == null) return;
            button.transition = Selectable.Transition.None;
            var color = selected ? new Color(.46f, .62f, 1f, 1f) : Color.white;
            var feedback = button.GetComponent<ButtonPressFeedback>();
            if (feedback != null) feedback.SetRestingColor(color);
            else if (button.targetGraphic != null) button.targetGraphic.color = color;
        }

        private void ChangeVolume(float delta)
        {
            volume = Mathf.Clamp01(volume + delta);
            SendSelectedControl("volume", volume.ToString("0.0", CultureInfo.InvariantCulture));
        }

        private void ToggleMute()
        {
            if (!SendSelectedControl("mute", !muted ? "true" : "false")) return;
            muted = !muted;
            if (!muted) SendSelectedControl("amplifier", "true");
            ApplyAudioVisuals();
        }

        private void ApplyAudioVisuals()
        {
            var button = transform.Find("VideoModeRoot/VideoControlsRoot/MuteCommandButton")?.GetComponent<Button>();
            SetButtonSprite(button, muted ? amplifierActionSprite : muteActionSprite);
            SetControlHighlight("MuteCommandButton", false);
        }

        private void SetLanguage(string value)
        {
            language = value;
            ApplyHeaderLanguageVisuals();
            udpClient?.Send(targetId, "language", value: language);
        }

        private void ApplyHeaderLanguageVisuals()
        {
            bool chinese = language == "zh-CN";
            SetHeaderArtwork(headerChineseArtwork, chinese ? headerChineseSelected : headerChineseUnselected,
                chinese ? new Vector2(108, 108) : new Vector2(71, 37));
            SetHeaderArtwork(headerEnglishArtwork, chinese ? headerEnglishUnselected : headerEnglishSelected,
                chinese ? new Vector2(47, 28) : new Vector2(108, 108));
        }

        private static void SetHeaderArtwork(Image image, Sprite sprite, Vector2 size)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.rectTransform.sizeDelta = size;
        }

        private void ClearCards()
        {
            selectedItem = null;
            ++previewVersion;
            if (selectedPreview != null) selectedPreview.sprite = null;
            if (selectedThumbnailOverlay != null) selectedThumbnailOverlay.sprite = null;
            ReleasePreview();
            if (selectedTitleLabel != null) selectedTitleLabel.text = "— 影片名称 —";
            SetControlHighlight("PlayCommandButton", false);
            SetControlHighlight("PauseCommandButton", false);
            foreach (var card in cards) if (card != null) Destroy(card);
            cards.Clear();
        }

        private void ReleasePreview()
        {
            if (ownedPreviewSprite == null) return;
            Destroy(ownedPreviewSprite.texture);
            Destroy(ownedPreviewSprite);
            ownedPreviewSprite = null;
        }

        private void OnDestroy() => ReleasePreview();

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }
    }
}
