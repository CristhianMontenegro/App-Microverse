using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Microverse.UI
{
    public class BottomNavigationBar
    {
        private readonly GameObject root;
        private readonly Action<string> onSelect;
        private string selected = "home";

        public BottomNavigationBar(Transform parent, Action<string> onSelect)
        {
            this.onSelect = onSelect;
            root = UiFactory.Panel("BottomNavigation", parent, new Color(0.01f, 0.04f, 0.10f, 0.96f), 28);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(28f, 18f);
            rect.offsetMax = new Vector2(-28f, 142f);

            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddTab("home", "Home");
            AddTab("categories", "Categorias");
            AddTab("scan", "Scan AR");
            AddTab("learn", "Aprender");
            AddTab("profile", "Perfil");
        }

        public void SetSelected(string id)
        {
            selected = id;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                bool active = child.name == id;
                Image image = child.GetComponent<Image>();
                image.color = active ? new Color(0.0f, 0.24f, 0.48f, 0.95f) : new Color(0f, 0f, 0f, 0f);
                TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>();
                label.color = active ? MicroverseTheme.Cyan : MicroverseTheme.Text;
            }
        }

        private void AddTab(string id, string label)
        {
            Button button = UiFactory.Button(id, root.transform, label, () =>
            {
                selected = id;
                SetSelected(id);
                onSelect(id);
            }, new Color(0f, 0f, 0f, 0f), MicroverseTheme.Text, id == "scan" ? 24 : 20);
            button.name = id;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = id == "scan" ? new Vector2(190f, 110f) : new Vector2(160f, 96f);
        }
    }
}
