using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace Build_From_Inventory
{
	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
	class UseInventory
	{
		//GenClosest::ClosestThingReachable
		//protected Job ResourceDeliverJobFor(Pawn pawn, IConstructible c, bool canRemoveExistingFloorUnderNearbyNeeders = true)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			instructions = Harmony.Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(GenClosest), nameof(GenClosest.ClosestThingReachable)),
				AccessTools.Method(typeof(UseInventory), nameof(OrUseInventory)));

			return Harmony.Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(GenClosest), nameof(ItemAvailability.ThingsAvailableAnywhere)),
				AccessTools.Method(typeof(UseInventory), nameof(OrInInventory)));
		}

		//public static Thing ClosestThingReachable(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
		public static Thing OrUseInventory(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
		{
			Log.Message($"Looking for {thingReq.singleDef}");
			Thing thing = GenClosest.ClosestThingReachable(root, map, thingReq, peMode, traverseParams, maxDistance, validator, customGlobalSearchSet, searchRegionsMin, searchRegionsMax, forceGlobalSearch, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
			if (thing != null) return thing;

			ThingDef def = thingReq.singleDef;
			Log.Message($"Didn't found {def}");
			return null;
		}

		//public bool ThingsAvailableAnywhere(ThingDefCountClass need, Pawn pawn)
		public bool OrInInventory(ItemAvailability item, ThingDefCountClass need, Pawn pawn)
		{
			if (item.ThingsAvailableAnywhere(need, pawn)) return true;

			ThingDef def = need.thingDef;
			Log.Message($"No {def} on map");
			return pawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
				.Any(p => p.inventory?.GetDirectlyHeldThings().Any(t => t.def == def) ?? false);
		}
	}
}