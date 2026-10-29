using System;
using Qianhai.ControlPad;
using UnityEngine;
using UnityEngine.UI;

namespace Qianhai.UI
{
    public sealed class ControlPadController : MonoBehaviour
    {
        private const string CoreMovieTargetId = "core-movie";

        [Serializable]
        public sealed class LayoutEntry
        {
            public string id;
            public Button button;
            public GameObject selectedMark;
        }

        [Header("配置与通讯")]
        [SerializeField] private ControlPadConfig config;
        [SerializeField] private UdpCommandClient udpClient;
        [SerializeField] private DynamicThumbnailGallery gallery;

        [Header("全局语言切换")]
        [SerializeField] private Button globalChineseButton;
        [SerializeField] private Button globalEnglishButton;
        [SerializeField] private Image globalChineseArtwork;
        [SerializeField] private Image globalEnglishArtwork;
        [SerializeField] private Sprite globalChineseSelected;
        [SerializeField] private Sprite globalChineseUnselected;
        [SerializeField] private Sprite globalEnglishSelected;
        [SerializeField] private Sprite globalEnglishUnselected;

        [Header("页面")]
        [SerializeField] private GameObject standbyPanel;
        [SerializeField] private GameObject homePanel;
        [SerializeField] private GameObject coreMoviePanel;
        [SerializeField] private GameObject layoutPanel;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private LayoutSubmenuPage[] layoutSubmenus = Array.Empty<LayoutSubmenuPage>();

        [Header("首页按钮")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button coreMovieButton;
        [SerializeField] private Button graphicInteractionButton;
        [SerializeField] private Button coreBackButton;
        [SerializeField] private Button layoutBackButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button homeBackButton;
        [SerializeField] private Button[] goHomeButtons = Array.Empty<Button>();

        [Header("核心影片控制")]
        [SerializeField] private Button chineseButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Button volumeUpButton;
        [SerializeField] private Button volumeDownButton;
        [SerializeField] private Button amplifierButton;

        [Header("核心影片按钮状态素材")]
        [SerializeField] private Sprite chineseSelectedSprite;
        [SerializeField] private Sprite chineseUnselectedSprite;
        [SerializeField] private Sprite englishSelectedSprite;
        [SerializeField] private Sprite englishUnselectedSprite;
        [SerializeField] private Sprite muteActionSprite;
        [SerializeField] private Sprite amplifierActionSprite;

        [Header("图文交互")]
        [SerializeField] private LayoutEntry[] layouts = Array.Empty<LayoutEntry>();
        [SerializeField] private Button imageTabButton;
        [SerializeField] private Button videoTabButton;
        [SerializeField] private Button confirmPlayButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text detailTitle;

        private string selectedTargetId = "layout-3";
        private string language = "zh-CN";
        private float volume = 1f;
        private bool muted;
        private enum CorePlaybackState { None, Playing, Paused }
        private CorePlaybackState corePlaybackState = CorePlaybackState.Playing;
        private static readonly Color PlaybackSelectedColor = new Color(.46f, .62f, 1f, 1f);

        private void Awake()
        {
            if (udpClient != null) udpClient.Configure(config);
            Wire(startButton, () => { SendAll("wake"); SendCore("wake"); Show(homePanel); });
            Wire(coreMovieButton, () => Show(coreMoviePanel));
            Wire(graphicInteractionButton, () => Show(layoutPanel));
            SetButtonTransition(chineseButton, Selectable.Transition.None);
            SetButtonTransition(englishButton, Selectable.Transition.None);
            SetButtonTransition(playButton, Selectable.Transition.None);
            SetButtonTransition(pauseButton, Selectable.Transition.None);
            SetButtonTransition(muteButton, Selectable.Transition.None);
            if (muteActionSprite == null && muteButton != null && muteButton.targetGraphic is Image muteImage)
                muteActionSprite = muteImage.sprite;
            if (amplifierActionSprite == null && amplifierButton != null && amplifierButton.targetGraphic is Image amplifierImage)
                amplifierActionSprite = amplifierImage.sprite;
            if (amplifierButton != null) amplifierButton.gameObject.SetActive(false);
            ApplyLanguageVisuals();
            ApplyPlaybackVisuals();
            ApplyAudioVisuals();
            Wire(chineseButton, () => SetLanguage("zh-CN"));
            Wire(englishButton, () => SetLanguage("en-US"));
            Wire(globalChineseButton, () => SetGlobalLanguage("zh-CN"));
            Wire(globalEnglishButton, () => SetGlobalLanguage("en-US"));
            Wire(playButton, PlayCoreMovie);
            Wire(pauseButton, PauseCoreMovie);
            Wire(replayButton, ReplayCoreMovie);
            Wire(muteButton, ToggleMute);
            Wire(volumeUpButton, () => SetVolume(volume + .1f));
            Wire(volumeDownButton, () => SetVolume(volume - .1f));
            Wire(imageTabButton, () => gallery?.SetMediaType("image"));
            Wire(videoTabButton, () => gallery?.SetMediaType("video"));
            Wire(confirmPlayButton, () => gallery?.ConfirmSelection());
            Wire(backButton, BackFromDetail);
            Wire(coreBackButton, ReturnHome);
            Wire(layoutBackButton, ReturnHome);
            Wire(refreshButton, () => gallery?.Refresh());
            Wire(homeBackButton, () => Show(standbyPanel));
            foreach (var homeButton in goHomeButtons) Wire(homeButton, () => Show(homePanel));
            foreach (var entry in layouts)
            {
                if (entry == null || entry.button == null) continue;
                var id = entry.id;
                Wire(entry.button, () => SelectLayout(id));
            }
            foreach (var submenu in layoutSubmenus)
                if (submenu != null) submenu.Initialize(this);
            Show(standbyPanel);
        }

        public void SelectLayout(string targetId)
        {
            selectedTargetId = targetId;
            foreach (var entry in layouts)
                if (entry != null && entry.selectedMark != null)
                    entry.selectedMark.SetActive(string.Equals(entry.id, targetId, StringComparison.OrdinalIgnoreCase));
            var target = config != null ? config.GetTarget(targetId) : null;
            if (detailTitle != null) detailTitle.text = target != null ? target.displayName : targetId;
            var submenu = FindSubmenu(targetId);
            if (submenu != null) Show(submenu.gameObject);
            else
            {
                Show(detailPanel);
                gallery?.Configure(config, udpClient, targetId);
            }
            udpClient?.Send(targetId, "select-layout");
        }

        public void ReturnHome() => Show(homePanel);
        public void ReturnToLayouts() => Show(layoutPanel);

        public void SelectCategory(LayoutSubmenuPage page, LayoutSubmenuPage.Category category)
        {
            if (page == null || category == null) return;
            selectedTargetId = page.TargetId;
            if (!string.IsNullOrWhiteSpace(category.udpCommand))
                udpClient?.Send(selectedTargetId, category.udpCommand,
                    value: string.IsNullOrWhiteSpace(category.udpValue) ? category.id : category.udpValue);
            if (!category.opensMediaGallery) return;
            var target = config != null ? config.GetTarget(selectedTargetId) : null;
            if (detailTitle != null) detailTitle.text = target != null ? target.displayName : selectedTargetId;
            Show(detailPanel);
            gallery?.Configure(config, udpClient, selectedTargetId);
        }

        private LayoutSubmenuPage FindSubmenu(string id)
        {
            foreach (var page in layoutSubmenus)
                if (page != null && string.Equals(page.TargetId, id, StringComparison.OrdinalIgnoreCase)) return page;
            return null;
        }

        private void BackFromDetail()
        {
            var submenu = FindSubmenu(selectedTargetId);
            Show(submenu != null ? submenu.gameObject : layoutPanel);
        }

        private void SetLanguage(string value)
        {
            language = value;
            ApplyLanguageVisuals();
            SendCore("language", value: language);
        }

        private void SetGlobalLanguage(string value)
        {
            language = value;
            ApplyLanguageVisuals();
            // Only control the computer belonging to the currently open page.
            var submenu = FindSubmenu(selectedTargetId);
            if ((detailPanel != null && detailPanel.activeSelf) || (submenu != null && submenu.gameObject.activeSelf))
                udpClient?.Send(selectedTargetId, "language", value: language);
            else if (coreMoviePanel != null && coreMoviePanel.activeSelf)
                SendCore("language", value: language);
        }

        private void PlayCoreMovie()
        {
            SendCore("play-core");
            corePlaybackState = CorePlaybackState.Playing;
            ApplyPlaybackVisuals();
        }

        private void PauseCoreMovie()
        {
            SendCore("pause");
            corePlaybackState = CorePlaybackState.Paused;
            ApplyPlaybackVisuals();
        }

        private void ReplayCoreMovie()
        {
            SendCore("replay");
            corePlaybackState = CorePlaybackState.Playing;
            ApplyPlaybackVisuals();
        }

        private void ApplyLanguageVisuals()
        {
            SetButtonSprite(chineseButton, language == "zh-CN" ? chineseSelectedSprite : chineseUnselectedSprite);
            SetButtonSprite(englishButton, language == "en-US" ? englishSelectedSprite : englishUnselectedSprite);
            bool chinese = language == "zh-CN";
            SetGlobalArtwork(globalChineseArtwork, chinese ? globalChineseSelected : globalChineseUnselected,
                chinese ? new Vector2(108, 108) : new Vector2(71, 37));
            SetGlobalArtwork(globalEnglishArtwork, chinese ? globalEnglishUnselected : globalEnglishSelected,
                chinese ? new Vector2(47, 28) : new Vector2(108, 108));
        }

        private static void SetGlobalArtwork(Image image, Sprite sprite, Vector2 size)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.rectTransform.sizeDelta = size;
        }

        private void ApplyPlaybackVisuals()
        {
            SetButtonColor(playButton, corePlaybackState == CorePlaybackState.Playing ? PlaybackSelectedColor : Color.white);
            SetButtonColor(pauseButton, corePlaybackState == CorePlaybackState.Paused ? PlaybackSelectedColor : Color.white);
        }

        private static void SetButtonTransition(Button button, Selectable.Transition transition)
        {
            if (button != null) button.transition = transition;
        }

        private static void SetButtonSprite(Button button, Sprite sprite)
        {
            var image = button != null ? button.targetGraphic as Image : null;
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            var image = button != null ? button.targetGraphic as Image : null;
            if (image != null) image.color = color;
        }

        private void ToggleMute()
        {
            muted = !muted;
            SendCore("mute", value: muted ? "true" : "false");
            if (!muted) SendCore("amplifier", value: "true");
            ApplyAudioVisuals();
        }

        private void ApplyAudioVisuals()
        {
            // Show the action the next click will perform, not the current audio state.
            SetButtonSprite(muteButton, muted ? amplifierActionSprite : muteActionSprite);
        }

        private void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            SendCore("volume", value: volume.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        private void SendCore(string command, string value = "")
        {
            udpClient?.Send(CoreMovieTargetId, command, value: value);
        }

        private void SendAll(string command, string value = "")
        {
            if (config == null || config.Targets == null || udpClient == null) return;
            foreach (var target in config.Targets)
                if (target != null) udpClient.Send(target.id, command, value: value);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void Show(GameObject activePanel)
        {
            if (activePanel == coreMoviePanel && coreMoviePanel != null && !coreMoviePanel.activeSelf)
            {
                // This is the page's default visual only; navigation must not trigger playback.
                corePlaybackState = CorePlaybackState.Playing;
                ApplyPlaybackVisuals();
                ApplyAudioVisuals();
            }
            if (standbyPanel != null) standbyPanel.SetActive(standbyPanel == activePanel);
            if (homePanel != null) homePanel.SetActive(homePanel == activePanel);
            if (coreMoviePanel != null) coreMoviePanel.SetActive(coreMoviePanel == activePanel);
            if (layoutPanel != null) layoutPanel.SetActive(layoutPanel == activePanel);
            if (detailPanel != null) detailPanel.SetActive(detailPanel == activePanel);
            foreach (var submenu in layoutSubmenus)
                if (submenu != null) submenu.gameObject.SetActive(submenu.gameObject == activePanel);
        }
    }
}
