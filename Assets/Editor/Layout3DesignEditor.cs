using System;
using System.Linq;
using Qianhai.ControlPad;
using Qianhai.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Layout3DesignEditor
{
    private const string Art = "Assets/中控/切图/布局3/";
    private const string GeneratedArt = "Assets/ControlPad/Layout3Sprites";

    [MenuItem("Qianhai/Update Video Layout With Thumbnails")]
    public static void ApplyVideoThumbnailLayout()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing the layout.");
        var gallery = UnityEngine.Object.FindObjectsByType<DynamicThumbnailGallery>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(g => g.gameObject.scene.IsValid());
        Undo.RegisterFullObjectHierarchyUndo(gallery.gameObject, "Update video thumbnail layout");
        var video = gallery.transform.Find("VideoModeRoot");
        var controls = video.Find("VideoControlsRoot");
        var backdrop = controls.Find("VideoControlsBackdrop").GetComponent<UnityEngine.UI.Image>();
        backdrop.sprite = SpriteFor("影片背板.png");
        backdrop.type = UnityEngine.UI.Image.Type.Simple;
        backdrop.preserveAspect = true;
        Place(backdrop.transform, 1447, 927, 1334, 969);
        Place(video.Find("SelectedTitleLabel"), 1448, 586, 1000, 80);
        Place(controls.Find("PlayCommandButton"), 1110, 742, 269, 93);
        Place(controls.Find("PauseCommandButton"), 1448, 742, 269, 93);
        Place(controls.Find("ReplayCommandButton"), 1780, 742, 270, 93);
        Place(controls.Find("VolumeUpCommandButton"), 1110, 914, 270, 93);
        Place(controls.Find("VolumeDownCommandButton"), 1448, 914, 270, 93);
        Place(controls.Find("MuteCommandButton"), 1110, 1090, 269, 93);
        // The existing merged audio button and its UDP behaviour are untouched.
        controls.Find("AmplifierCommandButton").gameObject.SetActive(false);

        var scroll = video.Find("ThumbnailScroll").GetComponent<UnityEngine.UI.ScrollRect>();
        Place(scroll.transform, 625, 861, 450, 860);
        var group = scroll.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        group.childControlWidth = group.childControlHeight = false;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        group.childAlignment = TextAnchor.UpperCenter;
        group.spacing = 20;
        group.padding = new RectOffset(12, 12, 305, 305);
        var serialized = new SerializedObject(gallery);
        var template = (ThumbnailCardView)serialized.FindProperty("cardTemplate").objectReferenceValue;
        var rect = template.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(322, 183);
        rect.localScale = Vector3.one;
        var layout = template.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = 322;
        layout.preferredHeight = 183;
        if (template.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(template.gameObject);
        var image = template.GetComponent<UnityEngine.UI.Image>();
        image.sprite = SpriteFor("未选中-底图.png");
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;

        var clip = EnsureImage(template.transform, "ThumbnailClip", SpriteFor("未选中-底图.png"));
        Stretch(clip.rectTransform);
        clip.rectTransform.offsetMin = new Vector2(12, 12);
        clip.rectTransform.offsetMax = new Vector2(-12, -12);
        clip.preserveAspect = false;
        var mask = clip.GetComponent<UnityEngine.UI.Mask>() ?? Undo.AddComponent<UnityEngine.UI.Mask>(clip.gameObject);
        mask.showMaskGraphic = false;
        var thumb = template.transform.Find("Thumbnail")?.GetComponent<UnityEngine.UI.Image>();
        if (thumb == null) thumb = EnsureImage(clip.transform, "Thumbnail", null);
        else thumb.transform.SetParent(clip.transform, false);
        thumb.gameObject.SetActive(true);
        Stretch(thumb.rectTransform);
        thumb.sprite = null;
        thumb.enabled = false;
        thumb.type = UnityEngine.UI.Image.Type.Simple;
        thumb.preserveAspect = false;
        thumb.raycastTarget = false;
        if (thumb.GetComponent<UIAspectFill>() == null) Undo.AddComponent<UIAspectFill>(thumb.gameObject);
        var title = template.GetComponentInChildren<UnityEngine.UI.Text>(true);
        title.transform.SetAsLastSibling();
        Stretch(title.rectTransform);
        title.fontSize = 44;
        var card = new SerializedObject(template);
        Assign(card, "thumbnail", thumb);
        Assign(card, "normalSprite", SpriteFor("未选中-底图.png"));
        Assign(card, "selectedSprite", SpriteFor("选中底图.png"));
        card.FindProperty("selectedScale").floatValue = 1f;
        card.FindProperty("resizeForSelection").boolValue = true;
        card.FindProperty("normalSize").vector2Value = new Vector2(322, 183);
        card.FindProperty("selectedSize").vector2Value = new Vector2(426, 250);
        card.FindProperty("hideTitleWhenThumbnailLoaded").boolValue = true;
        card.ApplyModifiedProperties();
        template.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
        EditorSceneManager.SaveScene(gallery.gameObject.scene);
    }

    [MenuItem("Qianhai/Align Layout 3 To Design")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing the layout.");
        var root = UnityEngine.Object.FindObjectsByType<DynamicThumbnailGallery>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(g => g.gameObject.scene.IsValid());
        if (root == null) throw new InvalidOperationException("The open scene has no DynamicThumbnailGallery.");
        var detail = root.transform;
        Undo.RegisterFullObjectHierarchyUndo(detail.gameObject, "Align layout 3 to design");
        if (!AssetDatabase.IsValidFolder(GeneratedArt)) AssetDatabase.CreateFolder("Assets/ControlPad", "Layout3Sprites");

        var video = EnsureRoot(detail, "VideoModeRoot");
        var images = EnsureRoot(detail, "ImageModeRoot");
        var controls = EnsureRoot(video, "VideoControlsRoot");
        var sidebar = detail.Find("Layout3Sidebar");
        Place(sidebar, 240, 930, 481, 965);
        var title = detail.Find("TargetTitle").GetComponent<UnityEngine.UI.Text>();
        Place(title.transform, 1228, 355, 800, 100);
        title.fontSize = 72;
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(.12f, .13f, .15f);
        title.alignment = TextAnchor.MiddleCenter;
        title.resizeTextForBestFit = false;
        detail.Find("LibraryLabel")?.gameObject.SetActive(false);
        detail.Find("RefreshButton")?.gameObject.SetActive(false);

        var animationSelected = SpriteFor("动画-点击.png");
        var animationNormal = SpriteFor("动画-未点击.png", new Rect(33, 130, 315, 131));
        var imageSelected = SpriteFor("图片-点击.png");
        var imageNormal = SpriteFor("图片未点击.png", new Rect(33, 130, 315, 131));
        var animationTab = detail.Find("VideosTabButton").GetComponent<UnityEngine.UI.Button>();
        var imageTab = detail.Find("ImagesTabButton").GetComponent<UnityEngine.UI.Button>();
        ConfigureButton(animationTab, animationNormal);
        ConfigureButton(imageTab, imageSelected);
        Place(animationTab.transform, 193, 739, 315, 131);
        Place(imageTab.transform, 193, 963, 315, 131);

        var header = EnsureRoot(detail, "HeaderLanguageRoot");
        var headerBackground = EnsureImage(header, "Background", GeneralSprite("语言切换bg.png"));
        Place(headerBackground.transform, 2262, 86, 233, 119);
        var headerChinese = AddHeaderButton(header, "ChineseButton", GeneralSprite("切换为中文.png"), 2208, 86, new Vector2(108, 108));
        var headerEnglish = AddHeaderButton(header, "EnglishButton", GeneralSprite("EN.png"), 2313, 86, new Vector2(47, 28));

        var oldScroll = detail.Find("ThumbnailScroll") ?? video.Find("ThumbnailScroll");
        oldScroll.SetParent(video, false);
        Place(oldScroll, 565, 880, 310, 650);
        var videoContent = oldScroll.GetComponent<UnityEngine.UI.ScrollRect>().content;
        ConfigureScroll(oldScroll, videoContent, true);
        var videoTemplate = videoContent.GetComponentInChildren<ThumbnailCardView>(true);
        ConfigureVideoTemplate(videoTemplate);

        var backdrop = EnsureImage(controls, "VideoControlsBackdrop", SpriteFor("影片背板.png"));
        Place(backdrop.transform, 1388, 927, 1452, 969);
        backdrop.transform.SetAsFirstSibling();
        var selectedTitle = EnsureText(video, "SelectedTitleLabel", title.font);
        Place(selectedTitle.transform, 1388, 586, 1000, 80);
        selectedTitle.text = "— 影片名称 —";
        selectedTitle.fontSize = 48;
        selectedTitle.color = new Color(.18f, .18f, .2f);
        AddControl(controls, "PlayCommandButton", "播放.png", 1040, 742);
        AddControl(controls, "PauseCommandButton", "暂停.png", 1384, 742);
        AddControl(controls, "ReplayCommandButton", "重播.png", 1730, 742);
        AddControl(controls, "VolumeUpCommandButton", "音量+.png", 1040, 914);
        AddControl(controls, "VolumeDownCommandButton", "音量—.png", 1384, 914);
        AddControl(controls, "MuteCommandButton", "静音.png", 1040, 1090);
        AddControl(controls, "AmplifierCommandButton", "功放.png", 1384, 1090);

        var frameTransform = detail.Find("SelectedPreviewFrame") ?? images.Find("SelectedPreviewFrame");
        frameTransform.SetParent(images, false);
        var frame = frameTransform.GetComponent<UnityEngine.UI.Image>();
        frame.sprite = SpriteFor("图片缩略图（无阴影）.png", null, new Vector4(35, 35, 35, 35));
        frame.type = UnityEngine.UI.Image.Type.Sliced;
        frame.preserveAspect = false;
        frame.color = Color.white;
        frame.raycastTarget = false;
        Place(frameTransform, 1228, 774, 1080, 610);
        var outline = frame.GetComponent<UnityEngine.UI.Outline>() ?? Undo.AddComponent<UnityEngine.UI.Outline>(frame.gameObject);
        outline.effectColor = new Color(.44f, .53f, 1f, .85f);
        outline.effectDistance = new Vector2(3, -3);
        var previewTransform = detail.Find("SelectedPreview") ?? images.Find("SelectedPreview") ?? images.Find("SelectedPreviewFrame/SelectedPreview");
        previewTransform.SetParent(images, false);
        Place(previewTransform, 1228, 774, 1058, 588);
        var preview = previewTransform.GetComponent<UnityEngine.UI.Image>();
        preview.preserveAspect = true;
        preview.raycastTarget = false;
        preview.color = Color.white;

        var imageScroll = images.Find("ImageThumbnailScroll") ?? EnsureRoot(images, "ImageThumbnailScroll");
        Place(imageScroll, 1228, 1236, 1120, 270);
        var imageContent = imageScroll.Find("Viewport/Content") ?? EnsureRoot(imageScroll, "Content");
        ConfigureScroll(imageScroll, imageContent.GetComponent<RectTransform>(), false);
        var imageTemplate = imageContent.GetComponentInChildren<ThumbnailCardView>(true);
        if (imageTemplate == null)
        {
            var go = new GameObject("ImageThumbnailTemplate", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(ThumbnailCardView), typeof(UnityEngine.UI.LayoutElement));
            Undo.RegisterCreatedObjectUndo(go, "Create image thumbnail template");
            go.transform.SetParent(imageContent, false);
            imageTemplate = go.GetComponent<ThumbnailCardView>();
        }
        ConfigureImageTemplate(imageTemplate);

        var selectedThumbFrame = EnsureImage(images, "SelectedThumbnailFrame", frame.sprite);
        selectedThumbFrame.type = UnityEngine.UI.Image.Type.Sliced;
        selectedThumbFrame.preserveAspect = false;
        Place(selectedThumbFrame.transform, 1228, 1236, 400, 239);
        var shadow = selectedThumbFrame.GetComponent<UnityEngine.UI.Shadow>() ?? Undo.AddComponent<UnityEngine.UI.Shadow>(selectedThumbFrame.gameObject);
        shadow.effectColor = new Color(.1f, .2f, .4f, .22f);
        shadow.effectDistance = new Vector2(0, -9);
        var selectedThumb = selectedThumbFrame.transform.Find("SelectedThumbnailOverlay")?.GetComponent<UnityEngine.UI.Image>()
            ?? EnsureImage(images, "SelectedThumbnailOverlay", null);
        Place(selectedThumb.transform, 1228, 1236, 380, 219);
        selectedThumb.preserveAspect = true;

        var confirmTransform = detail.Find("ConfirmPlayButton") ?? images.Find("ConfirmPlayButton");
        confirmTransform.SetParent(images, false);
        Place(confirmTransform, 2235, 774, 296, 116);
        Place(detail.Find("BackButton"), 2235, 1315, 296, 116);
        Place(detail.Find("HomeButton"), 2235, 1460, 296, 116);
        var status = detail.Find("StatusLabel").GetComponent<UnityEngine.UI.Text>();
        Place(status.transform, 1228, 1405, 1100, 45);
        status.fontSize = 24;
        status.alignment = TextAnchor.MiddleCenter;
        status.text = "";

        var serialized = new SerializedObject(root);
        Assign(serialized, "content", videoContent);
        Assign(serialized, "cardTemplate", videoTemplate);
        Assign(serialized, "imageContent", imageContent);
        Assign(serialized, "imageCardTemplate", imageTemplate);
        Assign(serialized, "selectedPreview", preview);
        Assign(serialized, "selectedThumbnailOverlay", selectedThumb);
        Assign(serialized, "imageTabSelected", imageSelected);
        Assign(serialized, "imageTabUnselected", imageNormal);
        Assign(serialized, "videoTabSelected", animationSelected);
        Assign(serialized, "videoTabUnselected", animationNormal);
        Assign(serialized, "headerChineseSelected", GeneralSprite("切换为中文.png"));
        Assign(serialized, "headerChineseUnselected", GeneralSprite("中文.png"));
        Assign(serialized, "headerEnglishSelected", GeneralSprite("切换为英文.png"));
        Assign(serialized, "headerEnglishUnselected", GeneralSprite("EN.png"));
        serialized.FindProperty("mediaTypeFilter").stringValue = "image";
        serialized.ApplyModifiedProperties();
        ApplyThumbnailFill(false);
        video.gameObject.SetActive(false);
        images.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(detail.gameObject.scene);
        EditorSceneManager.SaveScene(detail.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Layout 3 design aligned. ImageModeRoot and VideoModeRoot can be edited directly in Scene.");
    }

    [MenuItem("Qianhai/Fill Thumbnail Frames")]
    public static void ApplyThumbnailFill() => ApplyThumbnailFill(true);

    private static void ApplyThumbnailFill(bool save)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing thumbnail frames.");
        var gallery = UnityEngine.Object.FindObjectsByType<DynamicThumbnailGallery>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(g => g.gameObject.scene.IsValid());
        Undo.RegisterFullObjectHierarchyUndo(gallery.gameObject, "Fill thumbnail frames");
        var data = new SerializedObject(gallery);
        var imageRoot = gallery.transform.Find("ImageModeRoot");
        var preview = data.FindProperty("selectedPreview").objectReferenceValue as UnityEngine.UI.Image;
        var overlay = data.FindProperty("selectedThumbnailOverlay").objectReferenceValue as UnityEngine.UI.Image;
        var template = data.FindProperty("imageCardTemplate").objectReferenceValue as ThumbnailCardView;
        var templateData = new SerializedObject(template);
        var thumbnail = templateData.FindProperty("thumbnail").objectReferenceValue as UnityEngine.UI.Image;
        FillImage(preview, imageRoot.Find("SelectedPreviewFrame"));
        FillImage(overlay, imageRoot.Find("SelectedThumbnailFrame"));
        FillImage(thumbnail, template.transform);
        EditorSceneManager.MarkSceneDirty(gallery.gameObject.scene);
        if (save) EditorSceneManager.SaveScene(gallery.gameObject.scene);
    }

    private static void FillImage(UnityEngine.UI.Image image, Transform frame)
    {
        image.transform.SetParent(frame, false);
        Stretch(image.rectTransform);
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        if (image.GetComponent<UIAspectFill>() == null) Undo.AddComponent<UIAspectFill>(image.gameObject);
        var mask = frame.GetComponent<UnityEngine.UI.Mask>() ?? Undo.AddComponent<UnityEngine.UI.Mask>(frame.gameObject);
        mask.showMaskGraphic = true;
    }

    private static void ConfigureVideoTemplate(ThumbnailCardView template)
    {
        var rect = template.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(267, 100);
        template.transform.localScale = Vector3.one;
        var layout = template.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = 267;
        layout.preferredHeight = 100;
        var background = template.GetComponent<UnityEngine.UI.Image>();
        var normal = SpriteFor("未选中-底图.png");
        var selected = SpriteFor("选中底图.png");
        background.sprite = normal;
        background.color = Color.white;
        background.preserveAspect = true;
        var button = template.GetComponent<UnityEngine.UI.Button>();
        ConfigureButton(button, normal);
        var thumbnail = template.transform.Find("Thumbnail");
        if (thumbnail != null) thumbnail.gameObject.SetActive(false);
        var label = template.transform.Find("Title").GetComponent<UnityEngine.UI.Text>();
        PlaceLocal(label.rectTransform, Vector2.zero, new Vector2(245, 90));
        label.fontSize = 36;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(.18f, .2f, .25f);
        var serialized = new SerializedObject(template);
        Assign(serialized, "thumbnail", null);
        Assign(serialized, "normalSprite", normal);
        Assign(serialized, "selectedSprite", selected);
        serialized.FindProperty("selectedScale").floatValue = 1.06f;
        serialized.ApplyModifiedProperties();
        template.gameObject.SetActive(false);
    }

    private static void ConfigureImageTemplate(ThumbnailCardView template)
    {
        template.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 197);
        if (template.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(template.gameObject);
        var layout = template.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = 330;
        layout.preferredHeight = 197;
        var normal = SpriteFor("图片缩略图（无阴影）.png", null, new Vector4(25, 25, 25, 25));
        var background = template.GetComponent<UnityEngine.UI.Image>();
        background.type = UnityEngine.UI.Image.Type.Sliced;
        ConfigureButton(template.GetComponent<UnityEngine.UI.Button>(), normal);
        var thumbnail = EnsureImage(template.transform, "Thumbnail", null);
        PlaceLocal(thumbnail.rectTransform, Vector2.zero, new Vector2(312, 179));
        thumbnail.preserveAspect = true;
        var serialized = new SerializedObject(template);
        Assign(serialized, "thumbnail", thumbnail);
        Assign(serialized, "titleLabel", null);
        Assign(serialized, "selectButton", template.GetComponent<UnityEngine.UI.Button>());
        Assign(serialized, "normalSprite", normal);
        Assign(serialized, "selectedSprite", normal);
        serialized.FindProperty("selectedScale").floatValue = 1f;
        serialized.ApplyModifiedProperties();
        template.gameObject.SetActive(false);
    }

    private static void ConfigureScroll(Transform scrollTransform, RectTransform content, bool vertical)
    {
        var scroll = scrollTransform.GetComponent<UnityEngine.UI.ScrollRect>() ?? Undo.AddComponent<UnityEngine.UI.ScrollRect>(scrollTransform.gameObject);
        var oldMask = scrollTransform.GetComponent<UnityEngine.UI.Mask>();
        if (oldMask != null) oldMask.enabled = false;
        var oldImage = scrollTransform.GetComponent<UnityEngine.UI.Image>();
        if (oldImage != null) oldImage.enabled = false;
        var viewport = scrollTransform.Find("Viewport") ?? EnsureRoot(scrollTransform, "Viewport");
        Stretch(viewport.GetComponent<RectTransform>());
        var image = viewport.GetComponent<UnityEngine.UI.Image>() ?? Undo.AddComponent<UnityEngine.UI.Image>(viewport.gameObject);
        image.color = Color.white;
        var mask = viewport.GetComponent<UnityEngine.UI.Mask>() ?? Undo.AddComponent<UnityEngine.UI.Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;
        content.SetParent(viewport, false);
        content.anchorMin = vertical ? new Vector2(0, 1) : new Vector2(0, 0);
        content.anchorMax = vertical ? new Vector2(1, 1) : new Vector2(0, 1);
        content.pivot = vertical ? new Vector2(.5f, 1) : new Vector2(0, .5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var existingLayout = content.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
        if (existingLayout != null && (vertical != (existingLayout is UnityEngine.UI.VerticalLayoutGroup)))
            Undo.DestroyObjectImmediate(existingLayout);
        var group = content.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
        if (group == null) group = vertical
            ? (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)Undo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(content.gameObject)
            : Undo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(content.gameObject);
        group.enabled = true;
        group.childControlWidth = false;
        group.childControlHeight = false;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.spacing = vertical ? 20 : -100;
        group.padding = vertical ? new RectOffset(16, 16, 25, 25) : new RectOffset(395, 395, 30, 30);
        var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>() ?? Undo.AddComponent<UnityEngine.UI.ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = vertical ? UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained : UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = vertical ? UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize : UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        scroll.content = content;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = !vertical;
        scroll.vertical = vertical;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
    }

    private static Transform EnsureRoot(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create layout group");
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        return go.transform;
    }

    private static UnityEngine.UI.Image EnsureImage(Transform parent, string name, Sprite sprite)
    {
        var child = EnsureRoot(parent, name);
        var image = child.GetComponent<UnityEngine.UI.Image>() ?? Undo.AddComponent<UnityEngine.UI.Image>(child.gameObject);
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = sprite != null;
        return image;
    }

    private static UnityEngine.UI.Text EnsureText(Transform parent, string name, Font font)
    {
        var child = EnsureRoot(parent, name);
        var text = child.GetComponent<UnityEngine.UI.Text>() ?? Undo.AddComponent<UnityEngine.UI.Text>(child.gameObject);
        text.font = font;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void AddControl(Transform parent, string name, string art, float x, float y)
    {
        var image = EnsureImage(parent, name, SpriteFor(art));
        var materialPath = GeneratedArt + "/PlaybackBlue.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("UI/Default"));
            material.name = "PlaybackBlue";
            material.SetColor("_Color", new Color(.5f, .9f, 1.3f, 1f));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        image.material = material;
        var button = image.GetComponent<UnityEngine.UI.Button>() ?? Undo.AddComponent<UnityEngine.UI.Button>(image.gameObject);
        ConfigureButton(button, image.sprite);
        Place(image.transform, x, y, 269, 93);
    }

    private static void ConfigureButton(UnityEngine.UI.Button button, Sprite sprite)
    {
        var image = button.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = true;
        image.preserveAspect = true;
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
    }

    private static UnityEngine.UI.Button AddHeaderButton(Transform parent, string name, Sprite sprite, float x, float y, Vector2 artworkSize)
    {
        var image = EnsureImage(parent, name, null);
        image.color = Color.clear;
        image.raycastTarget = true;
        var button = image.GetComponent<UnityEngine.UI.Button>() ?? Undo.AddComponent<UnityEngine.UI.Button>(image.gameObject);
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        Place(image.transform, x, y, 108, 108);
        var artwork = EnsureImage(image.transform, "Artwork", sprite);
        PlaceLocal(artwork.rectTransform, Vector2.zero, artworkSize);
        return button;
    }

    private static Sprite GeneralSprite(string file) => AssetDatabase.LoadAllAssetsAtPath("Assets/中控/切图/" + file).OfType<Sprite>().First();

    private static Sprite SpriteFor(string file, Rect? crop = null, Vector4 border = default)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + file);
        if (texture == null) throw new InvalidOperationException("Missing art: " + file);
        if (crop == null && border == Vector4.zero)
            return AssetDatabase.LoadAllAssetsAtPath(Art + file).OfType<Sprite>().First();
        var name = System.IO.Path.GetFileNameWithoutExtension(file) + (crop != null ? "_Cropped" : "_Sliced");
        var path = GeneratedArt + "/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (saved != null) return saved;
        var sprite = Sprite.Create(texture, crop ?? new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
        sprite.name = name;
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    private static void Assign(SerializedObject serialized, string property, UnityEngine.Object value) => serialized.FindProperty(property).objectReferenceValue = value;

    private static void Place(Transform target, float x, float y, float width, float height)
    {
        if (target == null) return;
        PlaceLocal(target.GetComponent<RectTransform>(), new Vector2((x - 1228) * 2560 / 2456, 800 - y), new Vector2(width, height));
    }

    private static void PlaceLocal(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
