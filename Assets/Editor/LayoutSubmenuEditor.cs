using System;
using System.Linq;
using Qianhai.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LayoutSubmenuEditor
{
    private sealed class Entry
    {
        public string id, normal, selected;
        public Vector2 position;
        public bool gallery;
        public Entry(string id, string normal, string selected, float x, float y, bool gallery = false)
        {
            this.id = id; this.normal = normal; this.selected = selected;
            position = new Vector2(x, y); this.gallery = gallery;
        }
    }

    [MenuItem("Qianhai/Add Layout 4 5+6 11 Submenus")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var controller = UnityEngine.Object.FindObjectsByType<ControlPadController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(c => c.gameObject.scene.IsValid());
        var data = new SerializedObject(controller);
        var detail = (GameObject)data.FindProperty("detailPanel").objectReferenceValue;
        var canvas = detail.transform.parent;
        var back = (UnityEngine.UI.Button)data.FindProperty("backButton").objectReferenceValue;
        var home = detail.transform.Find("HomeButton").GetComponent<UnityEngine.UI.Button>();
        var pages = new[]
        {
            Page(canvas, "Layout4SubmenuPanel", "layout-4", "布局4", "布局4.png", back, home, new[]
            {
                new Entry("business-scenes", "业务场景-未选中.png", "业务场景-选中.png", -726, 110),
                new Entry("institutions", "接入机构-未选中.png", "接入机构-选中.png", 0, 110),
                new Entry("social-mainland", "社会价内地-未选中.png", "社会价内地-选中 .png", 726, 110),
                new Entry("social-hk", "社会价值香港-未选中.png", "社会价值香港-选中.png", -726, -240),
                new Entry("other", "其他-未选中.png", "其他-选中.png", 0, -240, true),
            }),
            Page(canvas, "Layout5_6SubmenuPanel", "layout-5-6", "布局5+6", "布局5+6.png", back, home, new[]
            {
                new Entry("monthly", "月报-未点击.png", "月报-点击.png", -366, 110),
                new Entry("annual", "年报-未点击.png", "年报-点击.png", 366, 110),
                new Entry("other", "其他-未点击.png", "其他-点击.png", -366, -240, true),
            }),
            Page(canvas, "Layout11SubmenuPanel", "layout-11", "布局11", "图层 14 拷贝 9.png", back, home, new[]
            {
                new Entry("apec", "apec-未点击.png", "apec-已点击.png", -366, 110),
                new Entry("belt-road", "一带一路-未点击.png", "一带一路-点击.png", 366, 110),
                new Entry("international-nodes", "国际节点-未点击.png", "国际节点-点击.png", -366, -240),
                new Entry("other", "其他-未点击.png", "其他-点击.png", 366, -240, true),
            }),
        };
        var property = data.FindProperty("layoutSubmenus");
        property.arraySize = pages.Length;
        for (int i = 0; i < pages.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
        data.ApplyModifiedProperties();
        canvas.Find("HeaderLanguageRoot")?.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
    }

    private static LayoutSubmenuPage Page(Transform parent, string name, string id, string folder, string title,
        UnityEngine.UI.Button back, UnityEngine.UI.Button home, Entry[] entries)
    {
        var root = Ensure(parent, name);
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Add layout submenu");
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        var page = root.GetComponent<LayoutSubmenuPage>() ?? Undo.AddComponent<LayoutSubmenuPage>(root.gameObject);
        var titleImage = Image(root, "PageTitle", Art(folder, title));
        Place(titleImage.rectTransform, new Vector2(0, 445), titleImage.sprite.rect.size);
        var backButton = Navigation(root, "BackButton", back);
        var homeButton = Navigation(root, "HomeButton", home);
        var data = new SerializedObject(page);
        data.FindProperty("targetId").stringValue = id;
        data.FindProperty("backButton").objectReferenceValue = backButton;
        data.FindProperty("homeButton").objectReferenceValue = homeButton;
        var categories = data.FindProperty("categories");
        categories.arraySize = entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var normal = Art(folder, entry.normal);
            var selected = Art(folder, entry.selected);
            var image = Image(root, entry.id, i == 0 ? selected : normal);
            image.raycastTarget = true;
            Place(image.rectTransform, entry.position, new Vector2(623, 283));
            var button = image.GetComponent<UnityEngine.UI.Button>() ?? Undo.AddComponent<UnityEngine.UI.Button>(image.gameObject);
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            Feedback(button);
            var category = categories.GetArrayElementAtIndex(i);
            category.FindPropertyRelative("id").stringValue = entry.id;
            category.FindPropertyRelative("button").objectReferenceValue = button;
            category.FindPropertyRelative("normalSprite").objectReferenceValue = normal;
            category.FindPropertyRelative("selectedSprite").objectReferenceValue = selected;
            category.FindPropertyRelative("opensMediaGallery").boolValue = entry.gallery;
            category.FindPropertyRelative("udpCommand").stringValue = "select-category";
            category.FindPropertyRelative("udpValue").stringValue = entry.id;
        }
        data.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        return page;
    }

    private static UnityEngine.UI.Button Navigation(Transform parent, string name, UnityEngine.UI.Button source)
    {
        var existing = parent.Find(name);
        if (existing == null)
        {
            var copy = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            Undo.RegisterCreatedObjectUndo(copy, "Add navigation button");
            existing = copy.transform;
        }
        var button = existing.GetComponent<UnityEngine.UI.Button>();
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        Feedback(button);
        return button;
    }

    private static void Feedback(UnityEngine.UI.Button button)
    {
        var feedback = button.GetComponent<ButtonPressFeedback>() ?? Undo.AddComponent<ButtonPressFeedback>(button.gameObject);
        var data = new SerializedObject(feedback);
        data.FindProperty("pressedScale").floatValue = .92f;
        data.FindProperty("reboundDuration").floatValue = .1f;
        data.FindProperty("pressedTint").colorValue = new Color(.5f, .7f, 1f, 1f);
        data.ApplyModifiedProperties();
    }

    private static RectTransform Ensure(Transform parent, string name)
    {
        var child = parent.Find(name) as RectTransform;
        if (child != null) return child;
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add submenu UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static UnityEngine.UI.Image Image(Transform parent, string name, Sprite sprite)
    {
        var rect = Ensure(parent, name);
        var image = rect.GetComponent<UnityEngine.UI.Image>() ?? Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite Art(string folder, string name)
    {
        var source = "Assets/中控/切图/" + folder + "/" + name;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>().ToArray();
        if (sprites.Length == 1 && sprites[0].rect.size == new Vector2(texture.width, texture.height)) return sprites[0];
        // Some title textures were auto-sliced into individual characters. Keep
        // the import settings intact and reference the complete original artwork.
        const string generated = "Assets/ControlPad/SubmenuSprites";
        if (!AssetDatabase.IsValidFolder(generated)) AssetDatabase.CreateFolder("Assets/ControlPad", "SubmenuSprites");
        var asset = generated + "/" + folder + "_" + System.IO.Path.GetFileNameWithoutExtension(name) + "_Full.asset";
        var saved = AssetDatabase.LoadAssetAtPath<Sprite>(asset);
        if (saved != null) return saved;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = folder + "_" + System.IO.Path.GetFileNameWithoutExtension(name) + "_Full";
        AssetDatabase.CreateAsset(sprite, asset);
        return sprite;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
