﻿using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Inventory
{
    // triggers code to run after defs have been loaded.
    [HarmonyPatch(typeof(Verse.Messages), nameof(Verse.Messages.Notify_LoadedLevelChanged))]
    public class Notify_LoadedLevelChanged_Patch
    {
        private static bool patchedDefs = false;

        private static void Postfix()
        {
            if (patchedDefs) return;
            
            foreach (var thing in DefDatabase<PawnKindDef>.AllDefs.Where(t => t.race.race.Humanlike))
            {
                var race = thing.race;
                if (race.comps.Any(c => c.compClass == typeof(LoadoutComponent))) continue;

                race.comps.Add(new CompProperties(typeof(LoadoutComponent)));
            }

            Utility.CalculateDefLists();

            patchedDefs = true;
        }
    }
}