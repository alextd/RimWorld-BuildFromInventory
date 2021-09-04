using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;


namespace Build_From_Inventory
{
	class ItemInvAvailabilityMapComp : MapComponent
	{
		//A ripoff of ItemAvailability but for Inventory items
		//Using ThingDef instead of ThingDefCount since that's too much a hassle I guess
		private Dictionary<int, bool> cachedResults = new Dictionary<int, bool>();

		public ItemInvAvailabilityMapComp(Map m) : base(m)
		{

		}

		public override void MapComponentTick()
		{
			this.cachedResults.Clear();
		}

		public bool ThingsAvailableInventories(ThingDef def, Faction faction)
		{
			int key = Gen.HashCombine(def.GetHashCode(), faction);
			bool result;
			if (!this.cachedResults.TryGetValue(key, out result))
			{
				result = map.mapPawns.SpawnedPawnsInFaction(faction)
					.Any(p => p.inventory.GetDirectlyHeldThings().Contains(def));
				//Log.Message($"caching {def}");
				this.cachedResults.Add(key, result);
			}
			//else Log.Message($"Using cache {def}");
			return result;
		}
	}
}
