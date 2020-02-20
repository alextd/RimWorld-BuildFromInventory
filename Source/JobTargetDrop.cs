using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace Build_From_Inventory
{
	[HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
	class JobTargetDrop
	{
		//protected override IEnumerable<Toil> MakeNewToils()
		public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_HaulToContainer __instance)
		{
			yield return DropThing(TargetIndex.A);

			foreach (Toil t in __result)
				yield return t;
		}

		public static Toil DropThing(TargetIndex haulableInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				Pawn actor = toil.actor;
				Job curJob = actor.jobs.curJob;
				Thing thing = curJob.GetTarget(haulableInd).Thing;
				
				if (thing.holdingOwner.Owner is Pawn_InventoryTracker holder)
				{
					int count = Mathf.Min(curJob.count, thing.stackCount);
					Log.Message($"{holder.pawn} dropping {thing}x{count} for {actor}");

					holder.innerContainer.TryDrop(thing, ThingPlaceMode.Near, count, out Thing droppedThing);
					if (droppedThing == null)
					{
						actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);//Shoooot
					}
					else
					{
						actor.Map.reservationManager.Release(thing, actor, curJob);//Someone else might want the rest of the stack

						ForbidUtility.SetForbidden(droppedThing, false, false);
						actor.Reserve(droppedThing, curJob, 100, count);//max pawns 100 why not
						curJob.SetTarget(haulableInd, droppedThing);
					}
				}
			};
			return toil;
		}
	}
}
