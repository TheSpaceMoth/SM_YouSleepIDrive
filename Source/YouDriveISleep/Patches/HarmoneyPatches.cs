using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SM.YouDriveISleep
{
	[StaticConstructorOnStartup]
	static public class HarmonyPatches
	{
		public static Harmony harmonyInstance;
		
		static HarmonyPatches()
		{
			harmonyInstance = new Harmony("Spacemoth.YouDriveISleep");
			harmonyInstance.PatchAll();
		}
	}

	[HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyPawnsNeeds")]
	internal static class PatchedTrySatisfyPawnNeeds
	{
		[HarmonyPostfix]
		public static void PostFix(Caravan_NeedsTracker __instance)
		{
			List<Pawn> PawnList = new List<Pawn>();
			PawnList.AddRange((IEnumerable<Pawn>)__instance.caravan.PawnsListForReading.ToList<Pawn>());
			//PawnList.Sort((Comparison<Pawn>)((p1, p2) => p1.RaceProps.Humanlike.CompareTo(p2.RaceProps.Humanlike)));

			for (int i = 0; i < PawnList.Count; i++)
				HandleNeeds(PawnList[i]);
		}

		private static void HandleNeeds(Pawn pawn)
		{
			if (pawn.Dead)
				return;

			Caravan pawnsCaravan = pawn.GetCaravan();

			// Only do this while in motion. Default handling works when stationary
			if ((pawnsCaravan is VehicleCaravan) && (((VehicleCaravan)pawnsCaravan).vehiclePather.MovingNow == true))
			{
				VehicleCaravan VCaravan = (VehicleCaravan)pawnsCaravan;

				// Get rest need
				Need_Rest RNeed = pawn.needs.TryGetNeed<Need_Rest>();

				if (RNeed != null)
				{
					// Check where they are in the caravan.

					if (pawn.ParentHolder is VehicleHandler)
					{
						VehicleHandler rootVehicle = (VehicleHandler)pawn.ParentHolder;

						float restEffectiveness = StatDefOf.BedRestEffectiveness.valueIfMissing;

						if (rootVehicle.vehicle != null)
							restEffectiveness = rootVehicle.vehicle.GetStatValue(StatDefOf.BedRestEffectiveness);
						
						//Log.Message("Pawn Role Is " + rootVehicle.role.label);

						if (rootVehicle.role.RequiredForCaravan == true)
						{
							// No Sleep
						}
						else if ((rootVehicle.role.TurretIds != null) && (rootVehicle.role.TurretIds.Count > 0))
						{
							// Gunner, half sleep.
							restEffectiveness /= 2.0f;
							RNeed.TickResting(restEffectiveness);
						}
						else
						{
							List<Pawn> MonitoredPawns = new List<Pawn>(rootVehicle.vehicle.Passengers.Where<Pawn>((Func<Pawn, bool>)(x => (x.IsColonist == false))));
							List<Pawn> MoniteringPawns = new List<Pawn>(rootVehicle.vehicle.Passengers.Where<Pawn>((Func<Pawn, bool>)(x => ((x.IsColonist == true) && (rootVehicle.role.RequiredForCaravan == false)))));

							if ((MonitoredPawns != null) && (MoniteringPawns  != null) && (MonitoredPawns.Count > 0))
							{
								if (MonitoredPawns.Count > MoniteringPawns.Count)
								{
									// Overworked, outnumbered. No sleeping.
									restEffectiveness = 0;
								}
								else if ((MonitoredPawns.Count * 2) <= MoniteringPawns.Count)
								{
									// Underworked, more than double guards. Can sleep in shifts. Don't adjust sleep.
								}
								else
								{
									// Less than double guards to prisoners/animals. Less effective sleep.
									restEffectiveness /= 1.5f;

								}
							}

							if(restEffectiveness > 0)
								RNeed.TickResting(restEffectiveness);
						}
					}
					else
					{
						//Log.Message("Not Vehicle Handler");
						// Pawn is on foot, no resting
					}
				}
			}
			else
			{
				//Log.Message("No vehicles");
			}

			return;
		}

	}


}
