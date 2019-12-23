using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;

namespace AnylandMods.FoundationPatches
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            ModMenu.AddButton(harmony, "Test", dotest);

            mod = modEntry;
            return true;
        }

        private class Temp : UnityEngine.MonoBehaviour {
            public void Update()
            {
                GetComponentInChildren<ThingPart>().currentState = (int)UnityEngine.Time.time % 2;
            }
        }

        private static void dotest(string id, Dialog dialog)
        {
            SyncAuthority.SyncLocallyForTesting = !SyncAuthority.SyncLocallyForTesting;
            SyncTools.SpawnThing("5df6a6e79153495bbf2e67f2", delegate(Thing thing) {
                thing.transform.position = Managers.personManager.ourPerson.transform.position;
                thing.destroyMeInTime = 300f;
                thing.gameObject.AddComponent<Temp>();
            }, true);
        }
    }
}
