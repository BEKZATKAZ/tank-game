using System;
using System.Collections.Generic;
using Tanks.Core.Cosmetics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardPool : IDisposable
    {
        public CosmeticCardPool(
            VisualElement root,
            Action<string, string> purchase,
            Action<string> equip,
            Action<string> preview)
        {
            _root = root;
            _purchase = purchase;
            _equip = equip;
            _preview = preview;
        }

        private readonly VisualElement _root;
        private readonly Action<string, string> _purchase;
        private readonly Action<string> _equip;
        private readonly Action<string> _preview;
        private readonly Dictionary<string, CosmeticCard> _cards = new();
        private readonly Dictionary<CosmeticType, VisualElement> _pages = new();
        private readonly HashSet<string> _visible = new();
        private readonly CosmeticType[] _types = { CosmeticType.Body, CosmeticType.Turret };
        private VisualElement _activePage;

        public void Bind(StoreViewModel model)
        {
            _visible.Clear();

            foreach (CosmeticType type in _types)
            {
                foreach (CosmeticCardData item in model.GetPage(type))
                {
                    _visible.Add(item.Config.Identifier);
                }
            }

            foreach (KeyValuePair<string, CosmeticCard> entry in _cards)
            {
                SetVisible(entry.Value, _visible.Contains(entry.Key));
            }

            foreach (CosmeticCardData item in model.AllItems)
            {
                string id = item.Config.Identifier;

                if (_cards.TryGetValue(id, out CosmeticCard card) == false)
                {
                    card = new CosmeticCard(id, _purchase, _equip, _preview);
                    _cards.Add(id, card);
                    GetPage(item.Config.CosmeticType).Add(card);
                }

                card.Bind(item);
                SetVisible(card, _visible.Contains(id));
            }

            foreach (CosmeticType type in _types)
            {
                VisualElement page = GetPage(type);
                int index = 0;

                foreach (CosmeticCardData item in model.GetPage(type))
                {
                    CosmeticCard card = _cards[item.Config.Identifier];

                    if (page.ElementAt(index) != card) page.Insert(index, card);
                    index++;
                }
            }

            Show(model.SelectedTab.Value);
        }

        public void Show(CosmeticType type)
        {
            VisualElement selected = GetPage(type);
            if (_activePage == selected && selected.parent == _root) return;

            foreach (KeyValuePair<CosmeticType, VisualElement> page in _pages)
            {
                if (page.Value == selected) continue;
                page.Value.RemoveFromHierarchy();
                HideContent(page.Value);
            }

            _activePage = selected;
            if (selected.parent != _root) _root.Add(selected);
        }

        public void UpdateViewport(Rect viewport)
        {
            if (_activePage == null || viewport.width <= 0) return;
            VisualElement focused = _root.focusController?.focusedElement as VisualElement;

            // Empty shells preserve scrolling and keyboard navigation off screen.
            foreach (VisualElement element in _activePage.Children())
            {
                if (element is not CosmeticCard card) continue;
                Rect bounds = card.worldBound;
                Rect nearby = viewport;
                nearby.xMin -= bounds.width;
                nearby.xMax += bounds.width;
                bool keepFocus = focused != null && (focused == card ||
                    card.Contains(focused));

                card.SetContentVisible(_visible.Contains((string)card.userData) &&
                    (nearby.Overlaps(bounds) || keepFocus));
            }
        }

        public VisualElement FindFocus(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            int separator = key.IndexOf(':');
            string id = separator < 0 ? key : key.Substring(0, separator);
            return _visible.Contains(id) && _cards.TryGetValue(id, out CosmeticCard card)
                ? card.FindFocus(key) : null;
        }

        public void Release()
        {
            foreach (VisualElement page in _pages.Values)
            {
                page.RemoveFromHierarchy();
                HideContent(page);
            }

            _activePage = null;
            foreach (CosmeticCard card in _cards.Values) card.Release();
        }

        private static void HideContent(VisualElement page)
        {
            foreach (VisualElement element in page.Children())
            {
                if (element is CosmeticCard card) card.SetContentVisible(false);
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display.value != display) element.style.display = display;
        }

        private VisualElement GetPage(CosmeticType type)
        {
            if (_pages.TryGetValue(type, out VisualElement page)) return page;
            page = new VisualElement();
            page.style.flexDirection = FlexDirection.Row;
            page.style.flexShrink = 0;
            _pages.Add(type, page);
            return page;
        }

        public void Dispose()
        {
            Release();
            _root.Clear();
            _cards.Clear();
            _pages.Clear();
            _visible.Clear();
        }
    }
}
