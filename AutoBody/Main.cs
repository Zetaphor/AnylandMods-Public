using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using Harmony;

namespace AnylandMods.AutoBody
{
    public class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;

            ModMenu.AddButton(harmony, "Saved Body Parts...", SavedBodyParts_Action);

            return true;
        }

        private static void BodyTellManager_ToldByBody(string data, bool byScript)
        {
            throw new NotImplementedException();
        }

        private static void SavedBodyParts_Action(string id, Dialog dialog)
        {

        }
    }
}
