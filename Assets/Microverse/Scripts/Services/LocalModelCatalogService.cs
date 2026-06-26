using System.Collections.Generic;
using Microverse.Data;
using UnityEngine;

namespace Microverse.Services
{
    public class LocalModelCatalogService : IModelCatalogService
    {
        private readonly List<BiologicalModel> models = new List<BiologicalModel>
        {
            new BiologicalModel(
                "cromossomo",
                new LocalizedText("Cromosoma", "Chromosome", "Cromossomo"),
                new LocalizedText("Estructura genetica", "Genetic Structure", "Estrutura genetica"),
                new LocalizedText("Tipos de celulas", "Types of Cells", "Tipos de celulas"),
                new LocalizedText(
                    "Modelo base incluido en la app para pruebas del visor RA local.",
                    "Base model included in the app for local AR viewer tests.",
                    "Modelo base incluido no app para testes do visor RA local."),
                "Chromosoma",
                new Color(0.88f, 0.22f, 0.44f),
                new Color(0.36f, 0.65f, 1.0f),
                7,
                false,
                "builtin:Cromossomo.fbx",
                "resource:ModelPreviews/cromossomo-preview",
                true)
        };

        public IReadOnlyList<BiologicalModel> GetModels()
        {
            return models;
        }

        public void LoadModels(System.Action<IReadOnlyList<BiologicalModel>> onComplete, System.Action<string> onError)
        {
            onComplete?.Invoke(models);
        }

        public IReadOnlyList<string> GetCategories()
        {
            List<string> cats = new List<string>();
            foreach (BiologicalModel model in models)
            {
                string catName = model.Category.Get(MicroverseLanguage.Spanish);
                if (!cats.Contains(catName))
                {
                    cats.Add(catName);
                }
            }

            return cats;
        }
    }
}
