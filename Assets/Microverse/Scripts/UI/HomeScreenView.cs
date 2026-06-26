using System;
using System.Collections.Generic;
using System.Linq;
using Microverse.Data;
using Microverse.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Microverse.UI
{
    public class HomeScreenView
    {
        private const int InlineCategoryLimit = 3;

        private enum CatalogMode
        {
            Ar,
            Library,
            Favorites
        }

        public GameObject Root { get; private set; }

        private readonly IReadOnlyList<BiologicalModel> models;
        private readonly IReadOnlyList<string> categories;
        private readonly MicroverseLanguage language;
        private readonly Action<BiologicalModel> onOpenModel;
        private readonly Action onCycleLanguage;
        private readonly Func<string, string> getText;
        private readonly Transform gridContent;
        private readonly Dictionary<string, Image> filterImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, TextMeshProUGUI> filterLabels = new Dictionary<string, TextMeshProUGUI>();
        private readonly Dictionary<string, Image> featureImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, TextMeshProUGUI> featureLabels = new Dictionary<string, TextMeshProUGUI>();
        private readonly HashSet<string> inlineFilterValues = new HashSet<string>();
        private TextMeshProUGUI catalogTitle;
        private TextMeshProUGUI emptyStateText;
        private Image moreFilterImage;
        private TextMeshProUGUI moreFilterLabel;
        private GameObject categoryPickerOverlay;
        private string searchTerm = string.Empty;
        private string categoryFilter = string.Empty;
        private CatalogMode catalogMode = CatalogMode.Ar;

        public HomeScreenView(Transform parent, IReadOnlyList<BiologicalModel> models, IReadOnlyList<string> categories, MicroverseLanguage language, Action<BiologicalModel> onOpenModel, Action onCycleLanguage, Func<string, string> getText)
        {
            this.models = models;
            this.categories = categories;
            this.language = language;
            this.onOpenModel = onOpenModel;
            this.onCycleLanguage = onCycleLanguage;
            this.getText = getText;

            Root = new GameObject("HomeScreen", typeof(RectTransform));
            Root.transform.SetParent(parent, false);
            UiFactory.Stretch(Root.GetComponent<RectTransform>());

            BuildHeader();
            BuildFeatureRow();
            BuildCatalogHeading();
            BuildSearchAndFilters();
            gridContent = BuildCatalogGrid();
            RefreshGrid();
        }

        private void BuildHeader()
        {
            TextMeshProUGUI logo = UiFactory.Text("Logo", Root.transform, "MicroVerse\nAR", 46, FontStyles.Bold, MicroverseTheme.Text);
            logo.enableWordWrapping = false;
            RectTransform logoRect = logo.rectTransform;
            logoRect.anchorMin = new Vector2(0f, 1f);
            logoRect.anchorMax = new Vector2(0f, 1f);
            logoRect.pivot = new Vector2(0f, 1f);
            logoRect.anchoredPosition = new Vector2(54f, -34f);
            logoRect.sizeDelta = new Vector2(360f, 110f);

            Button search = UiFactory.Button("SearchButton", Root.transform, getText("common.search"), () => { }, MicroverseTheme.PanelLight, MicroverseTheme.Text, 18);
            RectTransform searchRect = search.GetComponent<RectTransform>();
            searchRect.anchorMin = new Vector2(1f, 1f);
            searchRect.anchorMax = new Vector2(1f, 1f);
            searchRect.pivot = new Vector2(1f, 1f);
            searchRect.anchoredPosition = new Vector2(-154f, -44f);
            searchRect.sizeDelta = new Vector2(104f, 70f);

            Button settings = UiFactory.Button("LanguageButton", Root.transform, LanguageLabel(), () => onCycleLanguage(), MicroverseTheme.PanelLight, MicroverseTheme.Text, 18);
            RectTransform settingsRect = settings.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.pivot = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-42f, -44f);
            settingsRect.sizeDelta = new Vector2(96f, 70f);

            TextMeshProUGUI hero = UiFactory.Text("Hero", Root.transform, getText("home.hero"), 32, FontStyles.Bold, MicroverseTheme.Text);
            RectTransform heroRect = hero.rectTransform;
            heroRect.anchorMin = new Vector2(0f, 1f);
            heroRect.anchorMax = new Vector2(1f, 1f);
            heroRect.offsetMin = new Vector2(54f, -220f);
            heroRect.offsetMax = new Vector2(-54f, -130f);
        }

        private void BuildFeatureRow()
        {
            GameObject row = new GameObject("FeatureRow", typeof(RectTransform));
            row.transform.SetParent(Root.transform, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(54f, -320f);
            rect.offsetMax = new Vector2(-54f, -242f);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddFeature(row.transform, "ar", getText("home.feature.ar.title"), getText("home.feature.ar.subtitle"), true, () => SetCatalogMode(CatalogMode.Ar));
            AddFeature(row.transform, "library", getText("home.feature.library.title"), getText("home.feature.library.subtitle"), false, () => SetCatalogMode(CatalogMode.Library));
            AddFeature(row.transform, "favorites", getText("home.feature.favorites.title"), getText("home.feature.favorites.subtitle"), false, () => SetCatalogMode(CatalogMode.Favorites));
            RefreshFeatureSelection();
        }

        private void BuildSearchAndFilters()
        {
            TMP_InputField input = UiFactory.Input("SearchInput", Root.transform, getText("home.search.placeholder"));
            RectTransform inputRect = input.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 1f);
            inputRect.anchorMax = new Vector2(1f, 1f);
            inputRect.offsetMin = new Vector2(54f, -446f);
            inputRect.offsetMax = new Vector2(-54f, -390f);
            input.onValueChanged.AddListener(value =>
            {
                searchTerm = value.ToLowerInvariant();
                RefreshGrid();
            });

            GameObject filters = new GameObject("Filters", typeof(RectTransform));
            filters.transform.SetParent(Root.transform, false);
            RectTransform filtersRect = filters.GetComponent<RectTransform>();
            filtersRect.anchorMin = new Vector2(0f, 1f);
            filtersRect.anchorMax = new Vector2(1f, 1f);
            filtersRect.offsetMin = new Vector2(54f, -518f);
            filtersRect.offsetMax = new Vector2(-54f, -460f);

            HorizontalLayoutGroup layout = filters.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddFilter(filters.transform, getText("home.filter.all"), string.Empty);
            if (categories != null)
            {
                int inlineCount = Mathf.Min(categories.Count, InlineCategoryLimit);
                for (int i = 0; i < inlineCount; i++)
                {
                    string cat = categories[i];
                    AddFilter(filters.transform, cat, cat.ToLowerInvariant());
                }

                if (categories.Count > InlineCategoryLimit)
                {
                    AddMoreFilter(filters.transform);
                }
            }
            RefreshFilterSelection();
        }

        private void BuildCatalogHeading()
        {
            catalogTitle = UiFactory.Text("SectionTitle", Root.transform, getText("home.section.life"), 28, FontStyles.Bold, MicroverseTheme.Text);
            RectTransform titleRect = catalogTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(54f, -376f);
            titleRect.offsetMax = new Vector2(-54f, -326f);
        }

        private Transform BuildCatalogGrid()
        {
            GameObject viewport = UiFactory.Panel("CatalogViewport", Root.transform, new Color(0f, 0f, 0f, 0f), 0);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(54f, 172f);
            viewportRect.offsetMax = new Vector2(-54f, -552f);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            GameObject mask = new GameObject("Mask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            mask.transform.SetParent(viewport.transform, false);
            UiFactory.Stretch(mask.GetComponent<RectTransform>());
            Image maskImage = mask.GetComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.02f);
            mask.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(mask.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 1000f);

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(294f, 324f);
            grid.spacing = new Vector2(24f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = mask.GetComponent<RectTransform>();
            scroll.content = contentRect;

            emptyStateText = UiFactory.Text("EmptyState", Root.transform, EmptyStateText(), 22, FontStyles.Bold, MicroverseTheme.MutedText, TextAlignmentOptions.Center);
            RectTransform emptyRect = emptyStateText.rectTransform;
            emptyRect.anchorMin = new Vector2(0f, 0f);
            emptyRect.anchorMax = new Vector2(1f, 1f);
            emptyRect.offsetMin = new Vector2(70f, 172f);
            emptyRect.offsetMax = new Vector2(-70f, -552f);
            emptyStateText.gameObject.SetActive(false);
            return content.transform;
        }

        private void RefreshGrid()
        {
            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(gridContent.GetChild(i).gameObject);
            }

            catalogTitle.text = CatalogTitle();

            IEnumerable<BiologicalModel> filtered = models.Where(MatchesCatalogMode).Where(MatchesSearch).Where(MatchesCategory);
            List<BiologicalModel> visibleModels = filtered.ToList();
            foreach (BiologicalModel model in visibleModels)
            {
                bool available = ModelDownloadStore.IsAvailable(model);
                bool canDownload = catalogMode == CatalogMode.Library && !available && !string.IsNullOrWhiteSpace(model.ModelFileUrl);
                new ModelCardView(gridContent, model, language, onOpenModel, getText, RefreshGrid, available, canDownload, DownloadModel);
            }

            if (emptyStateText != null)
            {
                emptyStateText.text = EmptyStateText();
                emptyStateText.gameObject.SetActive(visibleModels.Count == 0);
            }
        }

        private bool MatchesCatalogMode(BiologicalModel model)
        {
            switch (catalogMode)
            {
                case CatalogMode.Library:
                    return IsPendingDownload(model);
                case CatalogMode.Favorites:
                    return FavoriteModelsStore.IsFavorite(model.Id);
                default:
                    return ModelDownloadStore.IsAvailable(model);
            }
        }

        private bool IsPendingDownload(BiologicalModel model)
        {
            return model != null
                && !model.IsBundledModel
                && !ModelDownloadStore.IsAvailable(model)
                && !string.IsNullOrWhiteSpace(model.ModelFileUrl);
        }

        private bool MatchesSearch(BiologicalModel model)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return true;
            }

            string value = (model.Name.Get(language) + " " + model.Subtitle.Get(language) + " " + model.ScientificName).ToLowerInvariant();
            return value.Contains(searchTerm);
        }

        private bool MatchesCategory(BiologicalModel model)
        {
            if (string.IsNullOrWhiteSpace(categoryFilter))
            {
                return true;
            }

            string value = (
                model.Category.Get(MicroverseLanguage.Spanish) + " " +
                model.Category.Get(MicroverseLanguage.English) + " " +
                model.Category.Get(MicroverseLanguage.Portuguese) + " " +
                model.Subtitle.Get(MicroverseLanguage.Spanish) + " " +
                model.Subtitle.Get(MicroverseLanguage.English) + " " +
                model.Subtitle.Get(MicroverseLanguage.Portuguese)).ToLowerInvariant();
            return value.Contains(categoryFilter);
        }

        private void AddFeature(Transform parent, string key, string title, string subtitle, bool active, Action onClick)
        {
            GameObject item = UiFactory.Panel("Feature-" + title, parent, active ? new Color(0.02f, 0.14f, 0.30f, 0.95f) : MicroverseTheme.Panel, 18);
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            button.colors = UiFactory.ButtonColors(active ? new Color(0.02f, 0.14f, 0.30f, 0.95f) : MicroverseTheme.Panel);
            button.onClick.AddListener(() => onClick?.Invoke());

            VerticalLayoutGroup layout = item.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 18, 12, 8);
            layout.spacing = 0;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            TextMeshProUGUI titleText = UiFactory.Text("Title", item.transform, title, 21, FontStyles.Bold, MicroverseTheme.Text);
            UiFactory.Text("Subtitle", item.transform, subtitle, 15, FontStyles.Normal, MicroverseTheme.MutedText);
            featureImages[key] = item.GetComponent<Image>();
            featureLabels[key] = titleText;
        }

        private void SetCatalogMode(CatalogMode mode)
        {
            catalogMode = mode;
            categoryFilter = string.Empty;
            CloseCategoryPicker();
            RefreshFeatureSelection();
            RefreshFilterSelection();
            RefreshGrid();
        }

        private void RefreshFeatureSelection()
        {
            foreach (KeyValuePair<string, Image> entry in featureImages)
            {
                bool active = entry.Key == CatalogModeKey();
                entry.Value.color = active ? new Color(0.02f, 0.14f, 0.30f, 0.95f) : MicroverseTheme.Panel;
            }

            foreach (KeyValuePair<string, TextMeshProUGUI> entry in featureLabels)
            {
                bool active = entry.Key == CatalogModeKey();
                entry.Value.color = active ? MicroverseTheme.Cyan : MicroverseTheme.Text;
            }
        }

        private string CatalogModeKey()
        {
            switch (catalogMode)
            {
                case CatalogMode.Library:
                    return "library";
                case CatalogMode.Favorites:
                    return "favorites";
                default:
                    return "ar";
            }
        }

        private string CatalogTitle()
        {
            switch (catalogMode)
            {
                case CatalogMode.Library:
                    return getText("home.feature.library.title");
                case CatalogMode.Favorites:
                    return getText("home.feature.favorites.title");
                default:
                    return getText("home.feature.ar.title");
            }
        }

        private void DownloadModel(BiologicalModel model)
        {
            MonoBehaviour runner = Root.GetComponentInParent<MonoBehaviour>();
            if (runner == null)
            {
                Debug.LogWarning("Cannot download model without a coroutine runner.");
                return;
            }

            runner.StartCoroutine(ModelDownloadStore.DownloadModelRoutine(model, (success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning("Model download failed: " + error);
                }

                RefreshGrid();
            }));
        }

        private void AddFilter(Transform parent, string label, string value)
        {
            Button button = UiFactory.Button("Filter-" + label, parent, label, () =>
            {
                categoryFilter = value;
                RefreshFilterSelection();
                RefreshGrid();
            }, new Color(0.03f, 0.08f, 0.17f, 0.88f), MicroverseTheme.Text, 16);

            filterImages[value] = button.GetComponent<Image>();
            filterLabels[value] = button.GetComponentInChildren<TextMeshProUGUI>();
            inlineFilterValues.Add(value);
        }

        private void AddMoreFilter(Transform parent)
        {
            Button button = UiFactory.Button("Filter-More", parent, MoreLabel(), ShowCategoryPicker, new Color(0.03f, 0.08f, 0.17f, 0.88f), MicroverseTheme.Text, 16);
            moreFilterImage = button.GetComponent<Image>();
            moreFilterLabel = button.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void RefreshFilterSelection()
        {
            foreach (KeyValuePair<string, Image> entry in filterImages)
            {
                bool active = entry.Key == categoryFilter;
                entry.Value.color = active ? new Color(0.0f, 0.24f, 0.48f, 0.95f) : new Color(0.03f, 0.08f, 0.17f, 0.88f);
            }

            foreach (KeyValuePair<string, TextMeshProUGUI> entry in filterLabels)
            {
                bool active = entry.Key == categoryFilter;
                entry.Value.color = active ? MicroverseTheme.Cyan : MicroverseTheme.Text;
            }

            if (moreFilterImage != null && moreFilterLabel != null)
            {
                bool active = !string.IsNullOrWhiteSpace(categoryFilter) && !inlineFilterValues.Contains(categoryFilter);
                moreFilterImage.color = active ? new Color(0.0f, 0.24f, 0.48f, 0.95f) : new Color(0.03f, 0.08f, 0.17f, 0.88f);
                moreFilterLabel.color = active ? MicroverseTheme.Cyan : MicroverseTheme.Text;
                moreFilterLabel.text = active ? SelectedCategoryLabel() : MoreLabel();
            }
        }

        private void ShowCategoryPicker()
        {
            if (categoryPickerOverlay != null)
            {
                UnityEngine.Object.Destroy(categoryPickerOverlay);
            }

            categoryPickerOverlay = UiFactory.Panel("CategoryPickerOverlay", Root.transform, new Color(0f, 0f, 0f, 0.58f), 0);
            UiFactory.Stretch(categoryPickerOverlay.GetComponent<RectTransform>());

            GameObject panel = UiFactory.Panel("CategoryPickerPanel", categoryPickerOverlay.transform, new Color(0.02f, 0.06f, 0.14f, 0.98f), 26);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.22f);
            panelRect.anchorMax = new Vector2(0.92f, 0.72f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI title = UiFactory.Text("Title", panel.transform, getText("nav.categories"), 28, FontStyles.Bold, MicroverseTheme.Text);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(34f, -74f);
            titleRect.offsetMax = new Vector2(-170f, -24f);

            Button close = UiFactory.Button("Close", panel.transform, CloseLabel(), CloseCategoryPicker, MicroverseTheme.PanelLight, MicroverseTheme.Text, 16);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-28f, -24f);
            closeRect.sizeDelta = new Vector2(124f, 50f);

            GameObject viewport = UiFactory.Panel("CategoryListViewport", panel.transform, new Color(0f, 0f, 0f, 0f), 0);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(28f, 28f);
            viewportRect.offsetMax = new Vector2(-28f, -92f);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject mask = new GameObject("Mask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            mask.transform.SetParent(viewport.transform, false);
            UiFactory.Stretch(mask.GetComponent<RectTransform>());
            Image maskImage = mask.GetComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.02f);
            mask.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(mask.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddPickerRow(content.transform, getText("home.filter.all"), string.Empty);
            if (categories != null)
            {
                foreach (string cat in categories)
                {
                    AddPickerRow(content.transform, cat, cat.ToLowerInvariant());
                }
            }

            scroll.viewport = mask.GetComponent<RectTransform>();
            scroll.content = contentRect;
        }

        private void AddPickerRow(Transform parent, string label, string value)
        {
            bool active = value == categoryFilter;
            Button button = UiFactory.Button("Category-" + label, parent, label, () =>
            {
                categoryFilter = value;
                CloseCategoryPicker();
                RefreshFilterSelection();
                RefreshGrid();
            }, active ? new Color(0.0f, 0.24f, 0.48f, 0.95f) : MicroverseTheme.PanelLight, active ? MicroverseTheme.Cyan : MicroverseTheme.Text, 18);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 54f);

            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 54f;
        }

        private void CloseCategoryPicker()
        {
            if (categoryPickerOverlay == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(categoryPickerOverlay);
            categoryPickerOverlay = null;
        }

        private string MoreLabel()
        {
            return language == MicroverseLanguage.English ? "More" : "Mas";
        }

        private string CloseLabel()
        {
            return language == MicroverseLanguage.English ? "Close" : "Cerrar";
        }

        private string SelectedCategoryLabel()
        {
            if (categories != null)
            {
                foreach (string cat in categories)
                {
                    if (cat.ToLowerInvariant() == categoryFilter)
                    {
                        return cat;
                    }
                }
            }

            return MoreLabel();
        }

        private string EmptyStateText()
        {
            if (catalogMode == CatalogMode.Favorites)
            {
                return language == MicroverseLanguage.English ? "No favorite models yet" : "Aun no hay modelos favoritos";
            }

            if (catalogMode == CatalogMode.Library)
            {
                return language == MicroverseLanguage.English ? "No new models to download" : "No hay modelos nuevos para descargar";
            }

            return language == MicroverseLanguage.English ? "No local AR models available" : "No hay modelos locales para RA";
        }

        private string LanguageLabel()
        {
            switch (language)
            {
                case MicroverseLanguage.English:
                    return "EN";
                case MicroverseLanguage.Portuguese:
                    return "PT";
                default:
                    return "ES";
            }
        }

    }
}
