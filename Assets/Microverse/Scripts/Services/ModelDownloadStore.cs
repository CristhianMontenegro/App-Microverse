using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microverse.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Microverse.Services
{
    public static class ModelDownloadStore
    {
        private const string PlayerPrefsKey = "microverse.downloaded_model_paths";
        private static readonly Dictionary<string, string> DownloadedPaths = new Dictionary<string, string>();
        private static bool loaded;

        public static bool IsAvailable(BiologicalModel model)
        {
            return model != null && (model.IsBundledModel || IsDownloaded(model.Id));
        }

        public static bool IsDownloaded(string modelId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(modelId) && DownloadedPaths.ContainsKey(modelId) && File.Exists(DownloadedPaths[modelId]);
        }

        public static bool TryGetLocalModelPath(BiologicalModel model, out string localPath)
        {
            localPath = string.Empty;
            if (model == null)
            {
                return false;
            }

            if (model.IsBundledModel)
            {
                localPath = model.ModelFileUrl;
                return true;
            }

            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(model.Id) && DownloadedPaths.TryGetValue(model.Id, out string path) && File.Exists(path))
            {
                localPath = path;
                return true;
            }

            return false;
        }

        public static IEnumerator DownloadModelRoutine(BiologicalModel model, Action<bool, string> onComplete)
        {
            if (model == null)
            {
                onComplete?.Invoke(false, "Model is missing.");
                yield break;
            }

            if (IsAvailable(model))
            {
                onComplete?.Invoke(true, string.Empty);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(model.ModelFileUrl))
            {
                onComplete?.Invoke(false, "Model does not have a download URL.");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(model.ModelFileUrl))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(false, request.error);
                    yield break;
                }

                string directory = Path.Combine(Application.persistentDataPath, "MicroverseModels");
                Directory.CreateDirectory(directory);

                string filePath = Path.Combine(directory, SafeFileName(model.Id) + ExtensionFromUrl(model.ModelFileUrl));
                File.WriteAllBytes(filePath, request.downloadHandler.data);

                EnsureLoaded();
                DownloadedPaths[model.Id] = filePath;
                Save();
                onComplete?.Invoke(true, string.Empty);
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            DownloadedPaths.Clear();

            string raw = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            string[] rows = raw.Split('\n');
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                int separator = row.IndexOf('|');
                if (separator <= 0)
                {
                    continue;
                }

                string modelId = row.Substring(0, separator);
                string path = row.Substring(separator + 1);
                if (!string.IsNullOrWhiteSpace(modelId) && !string.IsNullOrWhiteSpace(path))
                {
                    DownloadedPaths[modelId] = path;
                }
            }
        }

        private static void Save()
        {
            List<string> rows = new List<string>();
            foreach (KeyValuePair<string, string> entry in DownloadedPaths)
            {
                rows.Add(entry.Key + "|" + entry.Value);
            }

            PlayerPrefs.SetString(PlayerPrefsKey, string.Join("\n", rows.ToArray()));
            PlayerPrefs.Save();
        }

        private static string SafeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '-');
            }

            return value;
        }

        private static string ExtensionFromUrl(string url)
        {
            string clean = url.Split('?')[0];
            string extension = Path.GetExtension(clean);
            return string.IsNullOrWhiteSpace(extension) ? ".model" : extension;
        }
    }
}
