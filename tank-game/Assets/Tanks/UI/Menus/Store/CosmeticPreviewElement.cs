using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticPreviewElement : VisualElement
    {
        public CosmeticPreviewElement()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("cosmetic-preview");
            _body = CreateLayer(null);
            _turret = CreateLayer(null);
            Add(_body);
            Add(_turret);
            RegisterCallback<GeometryChangedEvent>(_ => LayoutPreview());
        }

        private readonly Image _body;
        private readonly Image _turret;
        private Rect _lastArea;
        private bool _dirty;

        public void SetSprites(Sprite body, Sprite turret)
        {
            if (_body.sprite == body && _turret.sprite == turret) return;
            _body.sprite = body;
            _turret.sprite = turret;
            _dirty = true;
            LayoutPreview();
        }
        private static Image CreateLayer(Sprite sprite)
        {
            Image image = new Image
            {
                sprite = sprite,
                scaleMode = ScaleMode.StretchToFill,
                pickingMode = PickingMode.Ignore
            };

            image.style.position = Position.Absolute;
            image.style.rotate = new Rotate(90);
            return image;
        }

        private void LayoutPreview()
        {
            if (_body.sprite == null || _turret.sprite == null) return;
            Rect body = GetBounds(_body.sprite, Vector2.zero);
            Rect turret = GetBounds(_turret.sprite, Vector2.right);
            Rect bounds = Rect.MinMaxRect(
                Mathf.Min(body.xMin, turret.xMin), Mathf.Min(body.yMin, turret.yMin),
                Mathf.Max(body.xMax, turret.xMax), Mathf.Max(body.yMax, turret.yMax));

            Rect area = contentRect;
            if (area == _lastArea && _dirty == false) return;
            _lastArea = area;
            _dirty = false;
            if (area.width <= 0 || area.height <= 0 || bounds.width <= 0 ||
                bounds.height <= 0) return;

            Vector2 center = body.center;
            float width = 2 * Mathf.Max(center.x - bounds.xMin, bounds.xMax - center.x);
            float height = 2 * Mathf.Max(center.y - bounds.yMin, bounds.yMax - center.y);
            float scale = Mathf.Min(area.width / width, area.height / height);
            Vector2 origin = area.center - new Vector2(center.x, -center.y) * scale;
            PlaceLayer(_body, Vector2.zero, origin, scale);
            PlaceLayer(_turret, Vector2.right, origin, scale);
        }

        private static Rect GetBounds(Sprite sprite, Vector2 offset)
        {
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;

            // Both renderers face right; the turret renderer is one world
            // unit ahead of the body.
            return Rect.MinMaxRect(
                offset.x - pivot.y, offset.y - (size.x - pivot.x),
                offset.x + size.y - pivot.y, offset.y + pivot.x);
        }

        private static void PlaceLayer(
            Image image,
            Vector2 offset,
            Vector2 origin,
            float scale)
        {
            Sprite sprite = image.sprite;
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;
            image.style.width = size.x * scale;
            image.style.height = size.y * scale;
            image.style.left = origin.x + (offset.x - pivot.x) * scale;
            image.style.top = origin.y - (offset.y + size.y - pivot.y) * scale;
            image.style.transformOrigin = new TransformOrigin(
                Length.Percent(pivot.x / size.x * 100), Length.Percent((1 - pivot.y /
                    size.y) * 100));
        }
    }
}
