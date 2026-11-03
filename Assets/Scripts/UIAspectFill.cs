using UnityEngine;

namespace Qianhai.UI
{
    // Crop the sprite UVs instead of resizing its RectTransform, so the scene
    // controls the visible frame and the artwork always covers it proportionally.
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class UIAspectFill : UnityEngine.UI.BaseMeshEffect
    {
        public override void ModifyMesh(UnityEngine.UI.VertexHelper mesh)
        {
            var image = graphic as UnityEngine.UI.Image;
            if (!IsActive() || image == null || image.sprite == null || mesh.currentVertCount == 0) return;
            var rect = image.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            float sourceAspect = image.sprite.rect.width / image.sprite.rect.height;
            float targetAspect = rect.width / rect.height;
            float cropX = Mathf.Min(1f, targetAspect / sourceAspect);
            float cropY = Mathf.Min(1f, sourceAspect / targetAspect);
            var uvMin = new Vector2(float.MaxValue, float.MaxValue);
            var uvMax = new Vector2(float.MinValue, float.MinValue);
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                uvMin = Vector2.Min(uvMin, vertex.uv0);
                uvMax = Vector2.Max(uvMax, vertex.uv0);
            }
            var center = (uvMin + uvMax) * .5f;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                var uv = vertex.uv0;
                uv.x = center.x + (uv.x - center.x) * cropX;
                uv.y = center.y + (uv.y - center.y) * cropY;
                vertex.uv0 = uv;
                mesh.SetUIVertex(vertex, i);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }
}
