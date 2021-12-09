﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Inventory
{
	// Add bill options under 'X per Tag' in the bill config
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
    public static class BillConfig
    {
        private static FieldInfo bill = AccessTools.Field(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.bill));
        private static MethodInfo drawConfig = AccessTools.Method(typeof(BillConfig), nameof(BillConfig.DrawConfig));
        private static FieldInfo repeatMode = AccessTools.Field(typeof(Bill_Production), nameof(Bill_Production.repeatMode));
        private static FieldInfo w_PerTag = AccessTools.Field(typeof(Inventory.BillRepeatModeDefOf), nameof(BillRepeatModeDefOf.W_PerTag));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var insts = instructions.ToList();

            for (int i = 0; i < insts.Count; i++) {
                if (Matches(insts, i))
                {
                    var label = generator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(insts[i]);
                    yield return new CodeInstruction(OpCodes.Ldfld, bill);
                    yield return new CodeInstruction(OpCodes.Ldfld, repeatMode);
                    yield return new CodeInstruction(OpCodes.Ldsfld, w_PerTag);
                    yield return new CodeInstruction(OpCodes.Bne_Un, label);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return insts[i + 1];
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, bill);
                    yield return new CodeInstruction(OpCodes.Call, drawConfig);
                    
                    yield return insts[i].WithLabels(label);
                } else {
                    yield return insts[i];
                }
            }
        }

        // todo: move to GUI/ and clean up
        public static void DrawConfig(Dialog_BillConfig window, Listing_Standard standard, Bill_Production bill)
        {
	        Widgets.Dropdown(standard.GetRect(30f), bill, LoadoutManager.TagFor, GenerateTagOptions, LoadoutManager.TagFor(bill) == null ? "Pick tag to Target" : $"Change tag from: {LoadoutManager.TagFor(bill).name}");
	        var text = $"{"CurrentlyHave".Translate()}: {bill.recipe.WorkerCounter.CountProducts(bill)} / {bill.targetCount * LoadoutManager.ColonistCountFor(bill)}";
	        var str = bill.recipe.WorkerCounter.ProductsDescription(bill);
	        if (!str.NullOrEmpty())
	        {
		        text += "\n" + "CountingProducts".Translate() + ": " + str.CapitalizeFirst();
	        }

	        standard.Label(text);
	        int targetCount = bill.targetCount;
	        standard.IntEntry(ref bill.targetCount, ref window.targetCountEditBuffer, bill.recipe.targetCountAdjustment);
	        bill.unpauseWhenYouHave = Mathf.Max(0, bill.unpauseWhenYouHave + (bill.targetCount - targetCount));
	        var producedThingDef = bill.recipe.ProducedThingDef;
	        if (producedThingDef == null) return;

	        if (producedThingDef.IsWeapon || producedThingDef.IsApparel)
	        {
		        standard.CheckboxLabeled("IncludeEquipped".Translate(), ref bill.includeEquipped);
	        }

	        if (producedThingDef.IsApparel && producedThingDef.apparel.careIfWornByCorpse)
	        {
		        standard.CheckboxLabeled("IncludeTainted".Translate(), ref bill.includeTainted);
	        }

	        if (bill.recipe.products.Any((ThingDefCountClass prod) => prod.thingDef.useHitPoints))
	        {
		        Widgets.FloatRange(standard.GetRect(28f), 975643279, ref bill.hpRange, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
		        bill.hpRange.min = Mathf.Round(bill.hpRange.min * 100f) / 100f;
		        bill.hpRange.max = Mathf.Round(bill.hpRange.max * 100f) / 100f;
	        }

	        if (producedThingDef.HasComp(typeof(CompQuality)))
	        {
		        Widgets.QualityRange(standard.GetRect(28f), 1098906561, ref bill.qualityRange);
	        }

	        if (producedThingDef.MadeFromStuff)
	        {
		        standard.CheckboxLabeled("LimitToAllowedStuff".Translate(), ref bill.limitToAllowedStuff, null);
	        }
        }
        
        private static IEnumerable<Widgets.DropdownMenuElement<Tag>> GenerateTagOptions(Bill_Production bill)
        {
	        foreach (var tag in LoadoutManager.Tags) {
		        yield return new Widgets.DropdownMenuElement<Tag>
		        {
			        option = new FloatMenuOption(tag.name, delegate()
			        {
				        LoadoutManager.SetTagForBill(bill, tag);
			        }),
			        payload = null
		        };
	        }
	        
        }

        public static bool Matches(List<CodeInstruction> insts, int i)
        {
            return i > 3 && i < insts.Count - 3
                         && insts[i - 4].opcode == OpCodes.Ldfld
                         && insts[i - 3].opcode == OpCodes.Ldfld
                         && insts[i - 2].opcode == OpCodes.Call
                         && insts[i - 1].opcode == OpCodes.Stfld
                         && insts[i + 0].opcode == OpCodes.Ldloc_S
                         && insts[i + 1].opcode == OpCodes.Ldloc_S
                         && insts[i + 2].opcode == OpCodes.Callvirt
                         && insts[i + 3].opcode == OpCodes.Ldloc_S;
        }
    }
}