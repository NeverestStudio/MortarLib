using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MortarLib
{
    public static class PerkRegistry
    {
        private static readonly Dictionary<string, List<Perk>> _registry = new Dictionary<string, List<Perk>>();

        /// <summary>
        /// Register a perk under your Mod Id.
        /// </summary>
        public static void Register(string modId, Perk perk)
        {
            if (!_registry.ContainsKey(modId))
                _registry[modId] = new List<Perk>();

            if (!_registry[modId].Contains(perk))
                _registry[modId].Add(perk);
        }

        /// <summary>
        /// Injects the perks for your Mod ID.
        /// </summary>
        public static void Inject(string modId, string databaseId, Perk.PerkPool pool = (Perk.PerkPool)5, string tag = "mortar")
        {
            if (!_registry.TryGetValue(modId, out var perksToInject) || perksToInject.Count == 0)
                return;

            WKAssetDatabase assetDb = ScriptableObject.CreateInstance<WKAssetDatabase>();
            assetDb.id = databaseId;

            foreach (var perk in perksToInject)
            {
                perk.spawnPool = pool;
                if (perk.tags == null) perk.tags = new List<string>();
                if (!perk.tags.Contains(tag)) perk.tags.Add(tag);
            }

            assetDb.perkAssets.AddRange(perksToInject);

            CL_AssetManager.AddNewDatabase(assetDb.id, assetDb);
        }
    }
}
