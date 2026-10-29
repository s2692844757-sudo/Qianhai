#if UNITY_EDITOR
using System.Collections.Generic;
using Qianhai.ControlPad;
using Qianhai.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Qianhai.EditorTools
{
    [InitializeOnLoad]
    public static class QianhaiSceneSetup
    {
        private const string RootName = "QianhaiControlPad";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ConfigPath = "Assets/ControlPad/ControlPadConfig.asset";
        private const string CardPrefabPath = "Assets/ControlPad/ThumbnailCard.prefab";
        private static Font font;

        static QianhaiSceneSetup()
        {
            EditorApplication.delayCall += CreateOnFirstBlankScene;
        }

        [MenuItem("Qianhai/Setup Control Pad Scene")]
        public static void SetupMenu()
        {
            BuildScene(true);
        }

        [MenuItem("Qianhai/Fix Button Labels")]
        public static void FixButtonLabels()
        {
            var root = GameObject.Find(RootName);
            if (root == null) return;
            var removed = 0;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var image = button.targetGraphic as Image;
                if (image == null || image.sprite == null) continue;
                var label = button.transform.Find("Label");
                if (label == null) continue;
                Undo.DestroyObjectImmediate(label.gameObject);
                removed++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"已移除 {removed} 个与按钮素材重复的文本标签。");
        }

        [MenuItem("Qianhai/Fix Header Logo")]
        public static void FixHeaderLogo()
        {
            var canvas = GameObject.Find(RootName + "/Canvas");
            if (canvas == null) return;
            var oldLogo = canvas.transform.Find("HeaderLogo");
            if (oldLogo != null) Undo.DestroyObjectImmediate(oldLogo.gameObject);
            if (canvas.transform.Find("HeaderLogoLabel") == null)
                AddText(canvas.transform, "HeaderLogoLabel", "LOGO", new Rect(0, 500, 600, 48), 18,
                    new Color(.65f, .75f, .9f), TextAnchor.MiddleCenter);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Qianhai/Fix UI Layout for 2560x1600")]
        public static void FixUiLayoutFor2560x1600()
        {
            var canvasObject = GameObject.Find(RootName + "/Canvas");
            if (canvasObject == null)
            {
                Debug.LogError("找不到 QianhaiControlPad/Canvas，请先打开已生成的控制面板 Scene。");
                return;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            var previousResolution = scaler.referenceResolution;
            var targetResolution = new Vector2(2560, 1600);
            if (Vector2.Distance(previousResolution, targetResolution) > .1f)
            {
                var scaleX = targetResolution.x / previousResolution.x;
                var scaleY = targetResolution.y / previousResolution.y;
                foreach (var rect in canvasObject.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.gameObject.name == "Background") continue;
                    Undo.RecordObject(rect, "Scale UI layout for 2560x1600");
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x * scaleX, rect.anchoredPosition.y * scaleY);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x * scaleX, rect.sizeDelta.y * scaleY);
                }
                foreach (var text in canvasObject.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(text, "Scale UI text for 2560x1600");
                    text.fontSize = Mathf.RoundToInt(text.fontSize * Mathf.Sqrt(scaleX * scaleY));
                }
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = targetResolution;
            scaler.matchWidthOrHeight = .5f;

            var homeTitle = canvasObject.transform.Find("HomePanel/Title");
            if (homeTitle != null)
            {
                var titleRect = homeTitle.GetComponent<RectTransform>();
                Undo.RecordObject(titleRect, "Move home title above entry cards");
                titleRect.anchoredPosition = new Vector2(0, 340 * (1600f / 1080f));
            }

            var background = canvasObject.transform.Find("Background");
            if (background != null)
            {
                var rect = background.GetComponent<RectTransform>();
                Undo.RecordObject(rect, "Fit background to screen");
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                var image = background.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    image.preserveAspect = true;
                    var fitter = background.GetComponent<AspectRatioFitter>();
                    if (fitter == null) fitter = Undo.AddComponent<AspectRatioFitter>(background.gameObject);
                    fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
                }
            }

            var adjustedImages = 0;
            foreach (var image in canvasObject.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || image.gameObject.name == "Background") continue;
                if (!image.preserveAspect)
                {
                    Undo.RecordObject(image, "Preserve UI image proportions");
                    image.preserveAspect = true;
                    adjustedImages++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"UI 已校正为 2560×1600；图片已保持原始比例（本次调整 {adjustedImages} 张）。");
        }

        [MenuItem("Qianhai/Align UI to Effect Image")]
        public static void AlignUiToEffectImage()
        {
            var root = GameObject.Find(RootName);
            var canvas = root != null ? root.transform.Find("Canvas") : null;
            if (canvas == null)
            {
                Debug.LogError("找不到 QianhaiControlPad/Canvas。");
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var logo = canvas.Find("HeaderLogoLabel");
            if (logo != null) Undo.DestroyObjectImmediate(logo.gameObject);
            ReplaceTextWithSprite(canvas.Find("StandbyPanel/WelcomeTitle"), "Assets/中控/切图/中控名称.png");
            ReplaceTextWithSprite(canvas.Find("HomePanel/Title"), "Assets/中控/切图/智能中控系统.png");
            var standbySubtitle = canvas.Find("StandbyPanel/WelcomeSubtitle");
            if (standbySubtitle != null) standbySubtitle.gameObject.SetActive(false);
            EnsureImage(canvas, "LanguageSwitchBackground", "Assets/中控/切图/语言切换bg.png", new Rect(815, 505, 165, 84));
            EnsureButton(canvas, "HeaderChineseButton", "Assets/中控/切图/切换为中文.png", new Rect(785, 505, 72, 38));
            EnsureButton(canvas, "HeaderEnglishButton", "Assets/中控/切图/EN.png", new Rect(845, 505, 52, 32));
            var languageBackground = canvas.Find("LanguageSwitchBackground").GetComponent<Image>();
            languageBackground.raycastTarget = false;
            PlaceCanvasRect(languageBackground.rectTransform, new Vector2(1087, 700), new Vector2(242, 124));
            PlaceCanvasRect(canvas.Find("HeaderChineseButton").GetComponent<RectTransform>(), new Vector2(1033, 700), new Vector2(108, 108));
            PlaceCanvasRect(canvas.Find("HeaderEnglishButton").GetComponent<RectTransform>(), new Vector2(1144, 700), new Vector2(60, 42));
            PlaceCanvasRect(canvas.Find("StandbyPanel/WelcomeTitle").GetComponent<RectTransform>(), new Vector2(0, 112), new Vector2(1130, 441));
            var startButton = canvas.Find("StandbyPanel/StartButton");
            if (startButton != null) PlaceCanvasRect(startButton.GetComponent<RectTransform>(), new Vector2(0, -360), new Vector2(410, 120));
            PlaceCanvasRect(canvas.Find("HomePanel/Title").GetComponent<RectTransform>(), new Vector2(0, 430), new Vector2(500, 80));

            var homePanel = canvas.Find("HomePanel");
            var corePanel = canvas.Find("CoreMoviePanel");
            var layoutPanel = canvas.Find("LayoutPanel");
            var detailPanel = canvas.Find("DetailPanel");
            ReplaceTextWithSprite(corePanel.Find("PageTitle"), "Assets/中控/切图/核心影片/核心影片.png");
            PlaceCanvasRect(corePanel.Find("PageTitle").GetComponent<RectTransform>(), new Vector2(0, 441), new Vector2(328, 76));
            PlaceCanvasRect(corePanel.Find("MovieControlBackdrop").GetComponent<RectTransform>(), new Vector2(0, -125), new Vector2(1888, 963));
            PlaceCanvasRect(corePanel.Find("ChineseButton").GetComponent<RectTransform>(), new Vector2(-395, 151), new Vector2(283, 96));
            PlaceCanvasRect(corePanel.Find("EnglishButton").GetComponent<RectTransform>(), new Vector2(-70, 151), new Vector2(283, 96));
            PlaceCanvasRect(corePanel.Find("PlayButton").GetComponent<RectTransform>(), new Vector2(-395, -125), new Vector2(280, 97));
            PlaceCanvasRect(corePanel.Find("PauseButton").GetComponent<RectTransform>(), new Vector2(-70, -125), new Vector2(280, 97));
            PlaceCanvasRect(corePanel.Find("ReplayButton").GetComponent<RectTransform>(), new Vector2(253, -125), new Vector2(280, 97));
            PlaceCanvasRect(corePanel.Find("VolumeUpButton").GetComponent<RectTransform>(), new Vector2(-388, -306), new Vector2(280, 97));
            PlaceCanvasRect(corePanel.Find("VolumeDownButton").GetComponent<RectTransform>(), new Vector2(-70, -306), new Vector2(280, 97));
            PlaceCanvasRect(corePanel.Find("MuteButton").GetComponent<RectTransform>(), new Vector2(250, -306), new Vector2(280, 97));
            var amplifier = EnsureButton(corePanel, "AmplifierButton", "Assets/中控/切图/核心影片/功放.png", new Rect(569, -306, 280, 97));
            PlaceCanvasRect(amplifier.GetComponent<RectTransform>(), new Vector2(569, -306), new Vector2(280, 97));
            var homeBack = EnsureButton(homePanel, "BackButton", "Assets/中控/切图/返回.png", new Rect(790, -405, 230, 90));
            var homeHome = EnsureButton(homePanel, "HomeButton", "Assets/中控/切图/首页.png", new Rect(790, -485, 230, 90));
            var coreBack = EnsureButton(corePanel, "BackButton", "Assets/中控/切图/返回.png", new Rect(790, -405, 230, 90));
            var coreHome = EnsureButton(corePanel, "HomeButton", "Assets/中控/切图/首页.png", new Rect(790, -485, 230, 90));
            var layoutBack = EnsureButton(layoutPanel, "BackButton", "Assets/中控/切图/返回.png", new Rect(790, -405, 230, 90));
            var layoutHome = EnsureButton(layoutPanel, "HomeButton", "Assets/中控/切图/首页.png", new Rect(790, -485, 230, 90));
            var detailBack = EnsureButton(detailPanel, "BackButton", "Assets/中控/切图/返回.png", new Rect(790, -405, 230, 90));
            var detailHome = EnsureButton(detailPanel, "HomeButton", "Assets/中控/切图/首页.png", new Rect(790, -485, 230, 90));

            PlaceCanvasRect(homeBack.GetComponent<RectTransform>(), new Vector2(1076, -520), new Vector2(310, 120));
            homeHome.gameObject.SetActive(false);
            PlaceCanvasRect(coreBack.GetComponent<RectTransform>(), new Vector2(1050, -515), new Vector2(310, 120));
            PlaceCanvasRect(coreHome.GetComponent<RectTransform>(), new Vector2(1050, -657), new Vector2(310, 120));
            PlaceCanvasRect(layoutBack.GetComponent<RectTransform>(), new Vector2(1076, -470), new Vector2(310, 120));
            PlaceCanvasRect(layoutHome.GetComponent<RectTransform>(), new Vector2(1076, -625), new Vector2(310, 120));
            PlaceCanvasRect(detailBack.GetComponent<RectTransform>(), new Vector2(1076, -470), new Vector2(310, 120));
            PlaceCanvasRect(detailHome.GetComponent<RectTransform>(), new Vector2(1076, -625), new Vector2(310, 120));

            PlaceCanvasRect(homePanel.Find("CoreMovieButton").GetComponent<RectTransform>(), new Vector2(-390, -150), new Vector2(779, 838));
            PlaceCanvasRect(homePanel.Find("GraphicInteractionButton").GetComponent<RectTransform>(), new Vector2(450, -150), new Vector2(779, 838));

            ReplaceTextWithSprite(layoutPanel.Find("PageTitle"), "Assets/中控/切图/图文交互/图文交互.png");
            PlaceCanvasRect(layoutPanel.Find("PageTitle").GetComponent<RectTransform>(), new Vector2(0, 455), new Vector2(330, 81));
            var layoutNames = new[] { "布局3", "布局4", "布局5+6", "布局9+10", "布局11", "布局12" };
            var layoutXs = new[] { -714f, 0f, 714f, -714f, 0f, 714f };
            var layoutYs = new[] { 110f, 110f, 110f, -255f, -255f, -255f };
            for (var i = 0; i < layoutNames.Length; i++)
            {
                var tile = layoutPanel.Find("Layout_" + layoutNames[i]);
                var selected = layoutPanel.Find("Selected_" + layoutNames[i]);
                var center = new Vector2(layoutXs[i], layoutYs[i]);
                var size = new Vector2(646, 294);
                if (tile != null) PlaceCanvasRect(tile.GetComponent<RectTransform>(), center, size);
                if (selected != null)
                {
                    PlaceCanvasRect(selected.GetComponent<RectTransform>(), center, size + new Vector2(12, 12));
                    var highlight = selected.GetComponent<Image>();
                    if (highlight != null)
                    {
                        highlight.enabled = false;
                        highlight.raycastTarget = false;
                    }
                }
            }
            var coreBackdrop = corePanel.Find("MovieControlBackdrop");
            if (coreBackdrop != null) PlaceCanvasRect(coreBackdrop.GetComponent<RectTransform>(), new Vector2(0, -125), new Vector2(1888, 963));
            var detailTitle = detailPanel.Find("TargetTitle");
            if (detailTitle != null) Place(detailTitle.GetComponent<RectTransform>(), new Rect(0, 390, 800, 56));

            var sidebar = EnsureImage(detailPanel, "Layout3Sidebar", "Assets/中控/切图/布局3/sidebar.png", new Rect(-800, 0, 320, 570));
            sidebar.raycastTarget = false;
            sidebar.transform.SetAsFirstSibling();

            var controller = root.GetComponent<ControlPadController>();
            if (controller != null)
            {
                SetObjectReference(controller, "chineseSelectedSprite", SpriteAt("Assets/中控/切图/核心影片/中文版-选中.png"));
                SetObjectReference(controller, "chineseUnselectedSprite", SpriteAt("Assets/中控/切图/核心影片/中文版-未选中 .png"));
                SetObjectReference(controller, "englishSelectedSprite", SpriteAt("Assets/中控/切图/核心影片/英文版-选中.png"));
                SetObjectReference(controller, "englishUnselectedSprite", SpriteAt("Assets/中控/切图/核心影片/英文版-未选中.png"));
                SetObjectReference(controller, "amplifierButton", amplifier);
                SetObjectReference(controller, "homeBackButton", homeBack);
                SetButtonArray(controller, "goHomeButtons", new[] { homeHome, coreHome, layoutHome, detailHome });
                SetObjectReference(controller, "coreBackButton", coreBack);
                SetObjectReference(controller, "layoutBackButton", layoutBack);
                SetObjectReference(controller, "backButton", detailBack);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("已按 Assets/中控/效果图.png 调整中控页眉、二级页导航、核心影片面板与图文侧栏。");
        }

        private static void ReplaceTextWithSprite(Transform target, string spritePath)
        {
            if (target == null) return;
            EnsureSingleSprite(spritePath);
            var sprite = SpriteAt(spritePath);
            if (sprite == null)
            {
                Debug.LogError($"找不到标题切图：{spritePath}");
                return;
            }

            var text = target.GetComponent<Text>();
            if (text != null) Undo.DestroyObjectImmediate(text);
            var image = target.GetComponent<Image>();
            if (image == null) image = Undo.AddComponent<Image>(target.gameObject);
            Undo.RecordObject(image, "Use supplied title sprite");
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var canvasRenderer = target.GetComponent<CanvasRenderer>();
            if (canvasRenderer == null) Undo.AddComponent<CanvasRenderer>(target.gameObject);
            EditorUtility.SetDirty(image);
        }

        private static void EnsureSingleSprite(string spritePath)
        {
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null || importer.spriteImportMode == SpriteImportMode.Single) return;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        private static void PlaceCanvasRect(RectTransform rectTransform, Vector2 centerPosition, Vector2 size)
        {
            if (rectTransform == null) return;
            Undo.RecordObject(rectTransform, "Align UI to 2560x1600 effect image");
            rectTransform.anchorMin = new Vector2(.5f, .5f);
            rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.pivot = new Vector2(.5f, .5f);
            rectTransform.anchoredPosition = centerPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private static Button EnsureButton(Transform parent, string name, string spritePath, Rect rect)
        {
            if (parent == null) return null;
            var existing = parent.Find(name);
            var button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null) button = MakeButton(parent, name, spritePath, "", rect, 18);
            else
            {
                var image = button.targetGraphic as Image;
                if (image != null) image.sprite = SpriteAt(spritePath);
                Place(button.GetComponent<RectTransform>(), rect);
            }
            return button;
        }

        private static Image EnsureImage(Transform parent, string name, string spritePath, Rect rect)
        {
            var existing = parent.Find(name);
            Image image;
            if (existing != null) image = existing.GetComponent<Image>();
            else image = MakeImage(parent, name, SpriteAt(spritePath), Color.white);
            if (image == null) return null;
            image.sprite = SpriteAt(spritePath);
            image.preserveAspect = true;
            Place(image.rectTransform, rect);
            return image;
        }

        private static void SetText(Transform root, string path, string value)
        {
            var target = root.Find(path);
            if (target == null) return;
            var text = target.GetComponent<Text>();
            if (text != null) text.text = value;
        }

        private static void CreateOnFirstBlankScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (GameObject.Find(RootName) != null) return;
            var activePath = SceneManager.GetActiveScene().path;
            if (activePath != ScenePath) return;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                if (root.name != "Main Camera" && root.name != "Global Light 2D") return;
            BuildScene(false);
        }

        private static void BuildScene(bool allowReplace)
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (!allowReplace)
                {
                    Debug.Log("前海中控 Scene 已存在，未重建。");
                    return;
                }
                if (!EditorUtility.DisplayDialog("重建中控界面", "将移除当前场景中的 QianhaiControlPad 界面对象并重新生成。其他场景对象会保留。继续吗？", "重建", "取消")) return;
                Object.DestroyImmediate(existing);
            }

            EnsureFolders();
            var config = LoadOrCreateConfig();
            var cardPrefab = LoadOrCreateCardPrefab();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Qianhai control pad");
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2560, 1600);
            scaler.matchWidthOrHeight = .5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");
            }

            var backdrop = MakeImage(canvasObject.transform, "Background", new Rect(0, 0, 2560, 1600), SpriteAt("Assets/中控/切图/bg.png"), new Color(1, 1, 1, 1));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.sizeDelta = Vector2.zero;
            var backdropFitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            backdropFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backdropFitter.aspectRatio = backdrop.sprite.rect.width / backdrop.sprite.rect.height;
            backdrop.transform.SetAsFirstSibling();
            AddText(canvasObject.transform, "HeaderLogoLabel", "LOGO", new Rect(0, 500, 600, 48), 18,
                new Color(.65f, .75f, .9f), TextAnchor.MiddleCenter);

            var standby = MakePanel(canvasObject.transform, "StandbyPanel", true);
            MakeImage(standby.transform, "WelcomeTitle", new Rect(0, 170, 1400, 90), SpriteAt("Assets/中控/切图/中控名称.png"), Color.white);
            var start = MakeButton(standby.transform, "StartButton", "Assets/中控/切图/点击开启.png", "点击开启", new Rect(0, -70, 300, 84), 30);

            var home = MakePanel(canvasObject.transform, "HomePanel", false);
            MakeImage(home.transform, "Title", new Rect(0, 340, 1920, 70), SpriteAt("Assets/中控/切图/智能中控系统.png"), Color.white);
            var coreButton = MakeButton(home.transform, "CoreMovieButton", "Assets/中控/切图/btn-核心影片.png", "核心影片", new Rect(-230, 205, 290, 190), 28);
            var graphicButton = MakeButton(home.transform, "GraphicInteractionButton", "Assets/中控/切图/btn-图文交互.png", "图文交互", new Rect(230, 205, 290, 190), 28);

            var core = MakePanel(canvasObject.transform, "CoreMoviePanel", false);
            AddText(core.transform, "PageTitle", "核心影片", new Rect(0, 385, 1000, 56), 34, new Color(.12f, .2f, .4f), TextAnchor.MiddleCenter);
            AddImage(core.transform, "MovieControlBackdrop", "Assets/中控/切图/核心影片/背板.png", new Rect(0, 40, 1100, 480));
            var chinese = MakeButton(core.transform, "ChineseButton", "Assets/中控/切图/核心影片/中文版-选中.png", "中文", new Rect(-350, 195, 190, 60), 22);
            var english = MakeButton(core.transform, "EnglishButton", "Assets/中控/切图/核心影片/英文版-未选中.png", "English", new Rect(-140, 195, 190, 60), 22);
            var play = MakeButton(core.transform, "PlayButton", "Assets/中控/切图/核心影片/播放.png", "播放", new Rect(-350, 60, 190, 76), 22);
            var pause = MakeButton(core.transform, "PauseButton", "Assets/中控/切图/核心影片/暂停.png", "暂停", new Rect(-140, 60, 190, 76), 22);
            var replay = MakeButton(core.transform, "ReplayButton", "Assets/中控/切图/核心影片/重播.png", "重播", new Rect(70, 60, 190, 76), 22);
            var mute = MakeButton(core.transform, "MuteButton", "Assets/中控/切图/核心影片/静音.png", "静音", new Rect(-350, -65, 190, 76), 22);
            var volumeDown = MakeButton(core.transform, "VolumeDownButton", "Assets/中控/切图/核心影片/音量—.png", "音量 -", new Rect(-140, -65, 190, 76), 22);
            var volumeUp = MakeButton(core.transform, "VolumeUpButton", "Assets/中控/切图/核心影片/音量+.png", "音量 +", new Rect(70, -65, 190, 76), 22);
            var coreBack = MakeButton(core.transform, "BackButton", "Assets/中控/切图/返回.png", "返回", new Rect(-770, 430, 120, 56), 20);

            var layout = MakePanel(canvasObject.transform, "LayoutPanel", false);
            AddText(layout.transform, "PageTitle", "图文交互 · 选择布局", new Rect(0, 390, 1000, 56), 34, new Color(.12f, .2f, .4f), TextAnchor.MiddleCenter);
            var layoutBack = MakeButton(layout.transform, "BackButton", "Assets/中控/切图/返回.png", "返回", new Rect(-770, 430, 120, 56), 20);
            var layoutEntries = new ControlPadController.LayoutEntry[6];
            string[] ids = { "layout-3", "layout-4", "layout-5-6", "layout-9-10", "layout-11", "layout-12" };
            string[] labels = { "布局3", "布局4", "布局5+6", "布局9+10", "布局11", "布局12" };
            string[] tilePaths = {
                "Assets/中控/切图/图文交互/布局3-未点击.png",
                "Assets/中控/切图/图文交互/布局4-未点击.png",
                "Assets/中控/切图/图文交互/布局5+6-未点击.png",
                "Assets/中控/切图/图文交互/布局9+10 未点击.png",
                "Assets/中控/切图/图文交互/布局11.png",
                "Assets/中控/切图/图文交互/布局12.png"
            };
            float[] xs = { -340, 0, 340, -340, 0, 340 };
            float[] ys = { 145, 145, 145, -135, -135, -135 };
            for (var i = 0; i < ids.Length; i++)
            {
                var button = MakeButton(layout.transform, "Layout_" + labels[i], tilePaths[i], labels[i], new Rect(xs[i], ys[i], 270, 190), 25);
                var mark = MakeImage(layout.transform, "Selected_" + labels[i], null, new Color(0, .43f, 1, .9f));
                Place(mark.rectTransform, new Rect(xs[i], ys[i], 278, 198));
                mark.type = Image.Type.Sliced;
                mark.raycastTarget = false;
                mark.gameObject.SetActive(false);
                layoutEntries[i] = new ControlPadController.LayoutEntry { id = ids[i], button = button, selectedMark = mark.gameObject };
            }

            var detail = MakePanel(canvasObject.transform, "DetailPanel", false);
            var detailTitle = AddText(detail.transform, "TargetTitle", "布局3", new Rect(0, 425, 800, 56), 34, new Color(.12f, .2f, .4f), TextAnchor.MiddleCenter);
            var detailBack = MakeButton(detail.transform, "BackButton", "Assets/中控/切图/返回.png", "返回布局", new Rect(-780, 430, 150, 56), 19);
            var imageTab = MakeButton(detail.transform, "ImagesTabButton", "Assets/中控/切图/布局3/图片-点击.png", "图片", new Rect(-740, 300, 180, 70), 22);
            var videoTab = MakeButton(detail.transform, "VideosTabButton", "Assets/中控/切图/布局3/动画-未点击.png", "视频", new Rect(-740, 210, 180, 70), 22);
            AddText(detail.transform, "LibraryLabel", "素材库 · 选择缩略图", new Rect(-185, 365, 800, 46), 24, new Color(.18f, .27f, .44f), TextAnchor.MiddleCenter);
            var scrollObject = new GameObject("ThumbnailScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollObject.transform.SetParent(detail.transform, false);
            var scrollRect = scrollObject.GetComponent<RectTransform>();
            Place(scrollRect, new Rect(-175, 55, 1080, 570));
            scrollObject.GetComponent<Image>().color = new Color(1, 1, 1, .5f);
            scrollObject.GetComponent<Mask>().showMaskGraphic = false;
            var viewport = scrollObject.transform;
            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, .5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            var horizontal = content.GetComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18;
            horizontal.padding = new RectOffset(20, 20, 20, 20);
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollObject.GetComponent<ScrollRect>().content = contentRect;
            scrollObject.GetComponent<ScrollRect>().viewport = scrollRect;
            scrollObject.GetComponent<ScrollRect>().horizontal = true;
            scrollObject.GetComponent<ScrollRect>().vertical = false;
            var card = MakeCardTemplate(content.transform);
            card.gameObject.SetActive(false);

            AddImage(detail.transform, "SelectedPreviewFrame", "Assets/中控/切图/布局3/影片背板.png", new Rect(700, 80, 400, 300));
            var preview = MakeImage(detail.transform, "SelectedPreview", null, new Color(1, 1, 1, .98f));
            Place(preview.rectTransform, new Rect(700, 80, 350, 220));
            preview.preserveAspect = true;
            var status = AddText(detail.transform, "StatusLabel", "正在连接素材库…", new Rect(620, -125, 540, 62), 19, new Color(.15f, .23f, .4f), TextAnchor.MiddleCenter);
            var confirm = MakeButton(detail.transform, "ConfirmPlayButton", "Assets/中控/切图/布局3/确认.png", "确认播放", new Rect(700, -260, 250, 74), 22);
            var refresh = MakeButton(detail.transform, "RefreshButton", null, "刷新素材", new Rect(700, -350, 220, 58), 19);

            var udp = root.AddComponent<UdpCommandClient>();
            udp.Configure(config);
            var gallery = detail.AddComponent<DynamicThumbnailGallery>();
            SetObjectReference(gallery, "config", config);
            SetObjectReference(gallery, "udpClient", udp);
            SetObjectReference(gallery, "content", contentRect);
            SetObjectReference(gallery, "cardTemplate", card);
            SetObjectReference(gallery, "selectedPreview", preview);
            SetObjectReference(gallery, "statusLabel", status);
            SetObjectReference(gallery, "targetId", "layout-3");

            var controller = root.AddComponent<ControlPadController>();
            SetObjectReference(controller, "config", config);
            SetObjectReference(controller, "udpClient", udp);
            SetObjectReference(controller, "gallery", gallery);
            SetObjectReference(controller, "standbyPanel", standby);
            SetObjectReference(controller, "homePanel", home);
            SetObjectReference(controller, "coreMoviePanel", core);
            SetObjectReference(controller, "layoutPanel", layout);
            SetObjectReference(controller, "detailPanel", detail);
            SetObjectReference(controller, "startButton", start);
            SetObjectReference(controller, "coreMovieButton", coreButton);
            SetObjectReference(controller, "graphicInteractionButton", graphicButton);
            SetObjectReference(controller, "coreBackButton", coreBack);
            SetObjectReference(controller, "layoutBackButton", layoutBack);
            SetObjectReference(controller, "refreshButton", refresh);
            SetObjectReference(controller, "chineseButton", chinese);
            SetObjectReference(controller, "englishButton", english);
            SetObjectReference(controller, "playButton", play);
            SetObjectReference(controller, "pauseButton", pause);
            SetObjectReference(controller, "replayButton", replay);
            SetObjectReference(controller, "muteButton", mute);
            SetObjectReference(controller, "volumeUpButton", volumeUp);
            SetObjectReference(controller, "volumeDownButton", volumeDown);
            SetObjectReference(controller, "imageTabButton", imageTab);
            SetObjectReference(controller, "videoTabButton", videoTab);
            SetObjectReference(controller, "confirmPlayButton", confirm);
            SetObjectReference(controller, "backButton", detailBack);
            SetObjectReference(controller, "detailTitle", detailTitle);
            SetObjectArray(controller, "layouts", layoutEntries);
            AlignUiToEffectImage();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorUtility.SetDirty(root);
            Debug.Log("前海中控场景已创建：Canvas、布局选择、素材缩略图浏览、UDP 目标配置均可在 Hierarchy / Inspector 中编辑。 ");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ControlPad")) AssetDatabase.CreateFolder("Assets", "ControlPad");
        }

        private static ControlPadConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ControlPadConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<ControlPadConfig>();
            var serialized = new SerializedObject(config);
            var targets = serialized.FindProperty("targets");
            string[] ids = { "layout-3", "layout-4", "layout-5-6", "layout-9-10", "layout-11", "layout-12" };
            string[] names = { "布局3", "布局4", "布局5+6", "布局9+10", "布局11", "布局12" };
            targets.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
            {
                var target = targets.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("id").stringValue = ids[i];
                target.FindPropertyRelative("displayName").stringValue = names[i];
                target.FindPropertyRelative("host").stringValue = "192.168.1." + (101 + i);
                target.FindPropertyRelative("port").intValue = 5005;
                target.FindPropertyRelative("thumbnailManifestUrl").stringValue = $"http://192.168.1.{101 + i}:8080/manifest.json";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static ThumbnailCardView LoadOrCreateCardPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            if (prefab != null) return prefab.GetComponent<ThumbnailCardView>();
            var root = new GameObject("ThumbnailCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ThumbnailCardView), typeof(LayoutElement));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(280, 390);
            root.GetComponent<Image>().color = new Color(1, 1, 1, .82f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            var image = MakeImage(root.transform, "Thumbnail", null, Color.white);
            Place(image.rectTransform, new Rect(0, 55, 250, 270));
            image.preserveAspect = true;
            var label = AddText(root.transform, "Title", "素材标题", new Rect(0, -145, 250, 52), 18, new Color(.12f, .18f, .3f), TextAnchor.MiddleCenter);
            var cardView = root.GetComponent<ThumbnailCardView>();
            SetObjectReference(cardView, "thumbnail", image);
            SetObjectReference(cardView, "titleLabel", label);
            SetObjectReference(cardView, "selectButton", button);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<ThumbnailCardView>();
        }

        private static GameObject MakePanel(Transform parent, string name, bool active)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Place(panel.GetComponent<RectTransform>(), new Rect(0, 0, 1920, 1080));
            panel.SetActive(active);
            return panel;
        }

        private static Button MakeButton(Transform parent, string name, string spritePath, string label, Rect rect, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = SpriteAt(spritePath);
            image.color = image.sprite == null ? new Color(.28f, .53f, .93f, .95f) : Color.white;
            image.preserveAspect = image.sprite != null;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(.86f, .93f, 1), pressedColor = new Color(.7f, .84f, 1), selectedColor = Color.white, disabledColor = new Color(1, 1, 1, .5f), colorMultiplier = 1, fadeDuration = .08f };
            Place(go.GetComponent<RectTransform>(), rect);
            if (!string.IsNullOrWhiteSpace(label) && image.sprite == null)
                AddText(go.transform, "Label", label, new Rect(0, 0, rect.width - 14, rect.height - 10), fontSize, new Color(.08f, .19f, .38f), TextAnchor.MiddleCenter);
            return button;
        }

        private static GameObject MakeCardTemplate(Transform parent)
        {
            var go = new GameObject("ThumbnailCardTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ThumbnailCardView), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 390);
            go.GetComponent<Image>().color = new Color(1, 1, 1, .9f);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 280;
            element.preferredHeight = 390;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var image = MakeImage(go.transform, "Thumbnail", null, Color.white);
            Place(image.rectTransform, new Rect(0, 55, 250, 270));
            image.preserveAspect = true;
            var title = AddText(go.transform, "Title", "素材标题", new Rect(0, -145, 250, 52), 18, new Color(.12f, .18f, .3f), TextAnchor.MiddleCenter);
            SetObjectReference(go.GetComponent<ThumbnailCardView>(), "thumbnail", image);
            SetObjectReference(go.GetComponent<ThumbnailCardView>(), "titleLabel", title);
            SetObjectReference(go.GetComponent<ThumbnailCardView>(), "selectButton", button);
            return go;
        }

        private static Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            return MakeImage(parent, name, sprite, color, true);
        }

        private static Image MakeImage(Transform parent, string name, Rect rect, Sprite sprite, Color color)
        {
            var image = MakeImage(parent, name, sprite, color, true);
            Place(image.rectTransform, rect);
            return image;
        }

        private static Image MakeImage(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite == null ? color : Color.white;
            image.preserveAspect = sprite != null;
            image.raycastTarget = raycast;
            return image;
        }

        private static void AddImage(Transform parent, string name, string spritePath, Rect rect)
        {
            var image = MakeImage(parent, name, SpriteAt(spritePath), Color.white);
            Place(image.rectTransform, rect);
        }

        private static Text AddText(Transform parent, string name, string value, Rect rect, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = Mathf.RoundToInt(size * Mathf.Sqrt((2560f / 1920f) * (1600f / 1080f)));
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            Place(go.GetComponent<RectTransform>(), rect);
            return text;
        }

        // Existing design rectangles use a 1920 x 1080 coordinate grid; map it to the 2560 x 1600 reference canvas.
        private static void Place(RectTransform rectTransform, Rect rect)
        {
            rectTransform.anchorMin = new Vector2(.5f, .5f);
            rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.pivot = new Vector2(.5f, .5f);
            rectTransform.anchoredPosition = new Vector2(rect.x * (2560f / 1920f), rect.y * (1600f / 1080f));
            rectTransform.sizeDelta = new Vector2(rect.width * (2560f / 1920f), rect.height * (1600f / 1080f));
            rectTransform.localScale = Vector3.one;
        }

        private static Sprite SpriteAt(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets) if (asset is Sprite sprite) return sprite;
            return null;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(ControlPadController controller, string propertyName, ControlPadController.LayoutEntry[] values)
        {
            var serialized = new SerializedObject(controller);
            var array = serialized.FindProperty(propertyName);
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                var item = array.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("id").stringValue = values[i].id;
                item.FindPropertyRelative("button").objectReferenceValue = values[i].button;
                item.FindPropertyRelative("selectedMark").objectReferenceValue = values[i].selectedMark;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetButtonArray(ControlPadController controller, string propertyName, Button[] values)
        {
            var serialized = new SerializedObject(controller);
            var array = serialized.FindProperty(propertyName);
            if (array == null) return;
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
#endif
