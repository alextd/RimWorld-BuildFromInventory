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
				AccessTools.Method(typeof(ItemAvailability), nameof(ItemAvailability.ThingsAvailableAnywhere)),
				AccessTools.Method(typeof(UseInventory), nameof(OrInInventory)));
		}

		//public static Thing ClosestThingReachable(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
		public static Thing OrUseInventory(IntVec3 root, Map map, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
		{
			Thing thing = GenClosest.ClosestThingReachable(root, map, thingReq, peMode, traverseParams, maxDistance, validator, customGlobalSearchSet, searchRegionsMin, searchRegionsMax, forceGlobalSearch, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
			if (thing != null) return thing;

			thing = FindInInventory(thingReq.singleDef, traverseParams.pawn);

			Log.Message($"Found: {thingReq.singleDef}? ({thing})");
		
			return thing;
		}

		public static Thing FindInInventory(ThingDef def, Pawn worker)
		{
			//Worker first
			foreach (Thing t in worker.inventory.GetDirectlyHeldThings())
				if (t.def == def)
					return t;

			//Others next. A little redundant on worker but it'll be empty
			foreach (Pawn p in worker.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
				.Where(p =>!p.Position.IsForbidden(worker) &&	worker.CanReach(p, PathEndMode.OnCell, Danger.Some)))
				foreach (Thing t in p.inventory.GetDirectlyHeldThings())
					if (t.def == def)
						return t;

			return null;
		}

		//public bool ThingsAvailableAnywhere(ThingDefCountClass need, Pawn pawn)
		public static bool OrInInventory(ItemAvailability item, ThingDefCountClass need, Pawn pawn)
		{
			if (item.ThingsAvailableAnywhere(need, pawn)) return true;

			return pawn.Map.GetComponent<ItemInvAvailabilityMapComp>().ThingsAvailableInventories(need.thingDef, pawn);
		}
	}

	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "FindAvailableNearbyResources")]
	public static class NothingNearbyDummy
	{
		public static FieldInfo resourcesAvailableInfo = AccessTools.Field(typeof(WorkGiver_ConstructDeliverResources), "resourcesAvailable");
		public static List<Thing> resourcesAvailable() =>
			(List<Thing>)resourcesAvailableInfo.GetValue(null);

		//private void FindAvailableNearbyResources(Thing firstFoundResource, Pawn pawn, out int resTotalAvailable)
		public static bool Prefix(Thing firstFoundResource, ref int resTotalAvailable)
		{
			if (firstFoundResource.Spawned) return true;

			//OMG please don't set static private lists and use them between methods.
			resourcesAvailable().Clear();
			resourcesAvailable().Add(firstFoundResource);
			resTotalAvailable = firstFoundResource.stackCount;
			return false;
		}
	}
}