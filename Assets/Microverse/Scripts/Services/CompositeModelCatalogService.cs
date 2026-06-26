using System;
using System.Collections.Generic;
using Microverse.Data;
using UnityEngine;

namespace Microverse.Services
{
    public class CompositeModelCatalogService : IModelCatalogService
    {
        private readonly IModelCatalogService localCatalog;
        private readonly IModelCatalogService remoteCatalog;
        private readonly List<BiologicalModel> cachedModels = new List<BiologicalModel>();
        private readonly List<string> cachedCategories = new List<string>();
        private bool isLoaded;

        public CompositeModelCatalogService(IModelCatalogService localCatalog, IModelCatalogService remoteCatalog)
        {
            this.localCatalog = localCatalog;
            this.remoteCatalog = remoteCatalog;
        }

        public IReadOnlyList<BiologicalModel> GetModels()
        {
            return cachedModels;
        }

        public IReadOnlyList<string> GetCategories()
        {
            return cachedCategories;
        }

        public void LoadModels(Action<IReadOnlyList<BiologicalModel>> onComplete, Action<string> onError)
        {
            if (isLoaded)
            {
                onComplete?.Invoke(cachedModels);
                return;
            }

            localCatalog.LoadModels(
                localModels =>
                {
                    SetCatalog(localModels, localCatalog.GetCategories());
                    remoteCatalog.LoadModels(
                        remoteModels =>
                        {
                            MergeRemoteModels(remoteModels, remoteCatalog.GetCategories());
                            isLoaded = true;
                            onComplete?.Invoke(cachedModels);
                        },
                        remoteError =>
                        {
                            Debug.LogWarning("Remote model catalog unavailable. Keeping bundled models only. Details: " + remoteError);
                            isLoaded = true;
                            onComplete?.Invoke(cachedModels);
                        });
                },
                localError =>
                {
                    onError?.Invoke(localError);
                });
        }

        private void SetCatalog(IReadOnlyList<BiologicalModel> models, IReadOnlyList<string> categories)
        {
            cachedModels.Clear();
            cachedCategories.Clear();
            AddModels(models);
            AddCategories(categories);
        }

        private void MergeRemoteModels(IReadOnlyList<BiologicalModel> models, IReadOnlyList<string> categories)
        {
            AddModels(models);
            AddCategories(categories);
        }

        private void AddModels(IReadOnlyList<BiologicalModel> models)
        {
            if (models == null)
            {
                return;
            }

            HashSet<string> knownIds = new HashSet<string>();
            for (int i = 0; i < cachedModels.Count; i++)
            {
                knownIds.Add(cachedModels[i].Id);
            }

            for (int i = 0; i < models.Count; i++)
            {
                BiologicalModel model = models[i];
                if (model == null || string.IsNullOrWhiteSpace(model.Id) || knownIds.Contains(model.Id))
                {
                    continue;
                }

                cachedModels.Add(model);
                knownIds.Add(model.Id);
            }
        }

        private void AddCategories(IReadOnlyList<string> categories)
        {
            if (categories == null)
            {
                return;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                if (!string.IsNullOrWhiteSpace(category) && !cachedCategories.Contains(category))
                {
                    cachedCategories.Add(category);
                }
            }
        }
    }
}
