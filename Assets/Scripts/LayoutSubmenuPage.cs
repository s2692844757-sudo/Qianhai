using System;
using UnityEngine;

namespace Qianhai.UI
{
    public sealed class LayoutSubmenuPage : MonoBehaviour
    {
        [Serializable]
        public sealed class Category
        {
            public string id;
            public UnityEngine.UI.Button button;
            public Sprite normalSprite;
            public Sprite selectedSprite;
            public bool opensMediaGallery;
            public string udpCommand = "select-category";
            public string udpValue;
        }

        [SerializeField] private string targetId;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private UnityEngine.UI.Button homeButton;
        [SerializeField] private Category[] categories = Array.Empty<Category>();
        public string TargetId => targetId;
        public Category[] Categories => categories;

        public void Initialize(ControlPadController controller)
        {
            if (backButton != null) backButton.onClick.AddListener(() => controller.ReturnToLayouts(targetId));
            if (homeButton != null) homeButton.onClick.AddListener(controller.ReturnHome);
            foreach (var entry in categories)
            {
                if (entry == null || entry.button == null) continue;
                var category = entry;
                entry.button.onClick.AddListener(() =>
                {
                    Select(category.id);
                    controller.SelectCategory(this, category);
                });
            }
            ClearSelection();
        }

        public void ClearSelection() => Select(null);

        private void Select(string id)
        {
            foreach (var category in categories)
            {
                if (category == null || category.button == null) continue;
                if (category.button.targetGraphic is UnityEngine.UI.Image image)
                    image.sprite = category.id == id ? category.selectedSprite : category.normalSprite;
                category.button.GetComponent<ButtonPressFeedback>()?.SetRestingColor(Color.white);
            }
        }
    }
}
