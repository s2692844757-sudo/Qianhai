using System;
using System.Collections;
using Qianhai.ControlPad;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Qianhai.UI
{
    public sealed class ThumbnailCardView : MonoBehaviour
    {
        [SerializeField] private Image thumbnail;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button selectButton;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private float selectedScale = 1f;
        [SerializeField] private bool resizeForSelection;
        [SerializeField] private Vector2 normalSize = new Vector2(322, 183);
        [SerializeField] private Vector2 selectedSize = new Vector2(426, 250);
        [SerializeField] private bool hideTitleWhenThumbnailLoaded;
        private ThumbnailManifest.MediaItem item;
        private Action<ThumbnailManifest.MediaItem> selected;
        private Vector3 originalScale = Vector3.one;
        private Sprite ownedSprite;
        public string AssetId => item != null ? item.id : "";
        public ThumbnailManifest.MediaItem MediaItem => item;
        public Sprite ThumbnailSprite => ownedSprite;

        private void Awake() => originalScale = transform.localScale;

        public void Bind(ThumbnailManifest.MediaItem mediaItem, Action<ThumbnailManifest.MediaItem> onSelected)
        {
            item = mediaItem;
            selected = onSelected;
            if (thumbnail != null) thumbnail.enabled = false;
            if (titleLabel != null) titleLabel.text = mediaItem != null ? mediaItem.title : "";
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(Select);
                selectButton.onClick.AddListener(Select);
            }
            if (thumbnail != null && mediaItem != null && !string.IsNullOrWhiteSpace(mediaItem.thumbnailUrl))
                StartCoroutine(LoadThumbnail(mediaItem.thumbnailUrl));
        }

        public void SetSelected(string assetId)
        {
            bool isSelected = item != null && item.id == assetId;
            var background = selectButton != null ? selectButton.targetGraphic as Image : null;
            if (background != null)
            {
                var sprite = isSelected ? selectedSprite : normalSprite;
                if (sprite != null) background.sprite = sprite;
            }
            if (titleLabel != null)
                titleLabel.color = isSelected && thumbnail == null ? Color.white : new Color(.18f, .2f, .25f);
            transform.localScale = originalScale * (isSelected ? selectedScale : 1f);
            if (resizeForSelection)
            {
                var size = isSelected ? selectedSize : normalSize;
                ((RectTransform)transform).sizeDelta = size;
                var layout = GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.preferredWidth = size.x;
                    layout.preferredHeight = size.y;
                }
            }
        }

        public void SelectCard() => Select();

        public void SetCarouselDistance(float distance)
        {
            var amount = Mathf.Clamp01(distance);
            transform.localScale = originalScale * Mathf.Lerp(1.2f, .8f, amount);
            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = Mathf.Lerp(1f, .3f, amount);
        }

        private IEnumerator LoadThumbnail(string url)
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    ApplyThumbnailTexture(texture);
                }
            }
        }

        private void ApplyThumbnailTexture(Texture2D texture)
        {
            if (texture == null) return;
            if (thumbnail == null)
            {
                Destroy(texture);
                return;
            }
            ownedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            thumbnail.sprite = ownedSprite;
            thumbnail.enabled = true;
            if (hideTitleWhenThumbnailLoaded && titleLabel != null)
                titleLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (ownedSprite == null) return;
            Destroy(ownedSprite.texture);
            Destroy(ownedSprite);
        }

        private void Select() => selected?.Invoke(item);
    }
}
