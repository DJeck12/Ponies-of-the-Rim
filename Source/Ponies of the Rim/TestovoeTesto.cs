using AlienRace;
using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LudeonTK;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using VanillaGenesExpanded;

namespace PoniesOfTheRim
{

	[StaticConstructorOnStartup]
	public static class TestTypePatch
	{
		static TestTypePatch()
		{
			new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnRenderNode_Fur), "GraphicFor"), null, new HarmonyMethod(typeof(TestTypePatch).GetMethod("TestoPatch")));
		}

		[HarmonyPostfix]
		public static void TestoPatch(Pawn pawn, ref Graphic __result, PawnRenderNode_Fur __instance)
		{
			if (!ModLister.CheckBiotech("Fur"))
			{
				return;
			}
			PonyGeneExtension ponyModExtension = __instance.gene.def.GetModExtension<PonyGeneExtension>();
			if (ponyModExtension == null)
			{
				return;
			}

			if (pawn.IsPony())
			{
				Pawn_StoryTracker story = pawn.story;
				if (((story != null) ? story.furDef : null) == null)
				{
					return;
				}
				Pawn_StoryTracker story2 = pawn.story;
				__result = GraphicDatabase.Get<Graphic_Multi>((story2 != null) ? ponyModExtension.pony : null, ShaderDatabase.Cutout, Vector2.one, pawn.story.HairColor);














			}









			//if (ModsConfig.BiotechActive)
			//{
			//	if (pawn.DevelopmentalStage.Baby() && pawn.story.Childhood.defName == "PonyChild" || pawn.DevelopmentalStage.Newborn())
			//	{
			//		pawn.story


			//		Log.Message("Baby");
			//		__result = Pony_DefOf.PonyBaby;
			//		Log.Message("Baby2");
			//	}
			//	if ((pawn.DevelopmentalStage.Child() || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyChild" || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyPreTeenager" || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyTeenager") && pawn.story.Childhood.defName == "Child27")
			//	{
			//		string s = pawn.story.Childhood.defName.ToString();
			//		Log.Message("Child");
			//		__result = Pony_DefOf.PonyChild;
			//		Log.Message("Child2");
			//	}
			//}
			//if (pawn.DevelopmentalStage.Adult())
			//{
			//	Log.Message("Adult");
			//	__result = Pony_DefOf.Pony;
			//	Log.Message("Adult2");
			//}































			//  [StaticConstructorOnStartup]
			//  public static class TestTypePatch
			//  {
			//      static TestTypePatch()
			//      {
			//          new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(HarmonyPatches), "CheckBodyType"), null, new HarmonyMethod(typeof(TestTypePatch).GetMethod("TestoPatch")));
			//      }

			//      [HarmonyPostfix]
			//      public static void TestoPatch(ref BodyTypeDef __result, Pawn pawn)
			//      {
			//      if (!pawn.IsPony())
			//{
			//	return;
			//}
			//if (ModsConfig.BiotechActive)
			//{
			//	if (pawn.DevelopmentalStage.Baby() && pawn.story.Childhood.defName == "PonyChild" || pawn.DevelopmentalStage.Newborn())
			//	{
			//                  pawn.story
			//                  Log.Message("Baby");
			//		__result = Pony_DefOf.PonyBaby;
			//                  Log.Message("Baby2");
			//	}
			//	if ((pawn.DevelopmentalStage.Child() || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyChild" || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyPreTeenager" || pawn.ageTracker.CurLifeStageRace.def.defName == "PonyTeenager") && pawn.story.Childhood.defName == "Child27")
			//	{
			//                  string s = pawn.story.Childhood.defName.ToString();
			//                  Log.Message("Child");
			//		__result = Pony_DefOf.PonyChild;
			//                  Log.Message("Child2");
			//	}
			//}
			//if (pawn.DevelopmentalStage.Adult())
			//{
			//              Log.Message("Adult");
			//	__result = Pony_DefOf.Pony;
			//              Log.Message("Adult2");
			//}































			//          //if (pawn.IsPony())
			//          //{
			//          //    if (ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
			//          //    {
			//          //        if (pawn.DevelopmentalStage == DevelopmentalStage.Baby)
			//          //        {
			//          //            Log.Message("Baby");
			//          //            __result = Pony_DefOf.PonyBaby;
			//          //            Log.Message("Baby2");
			//          //        }
			//          //        Log.Message("Child");
			//          //        //__result = Pony_DefOf.PonyChild;
			//          //        __result = pawn.story.bodyType = Pony_DefOf.PonyChild;
			//          //        Log.Message("Child2");
			//          //    }
			//          //    Log.Message("Adult");
			//          //    __result = Pony_DefOf.Pony;
			//          //    Log.Message("Adult2");
			//          //}
			//          //if (pawn.DevelopmentalStage == DevelopmentalStage.Adult) 
			//          //{

			//          //}














			//              //if (pawn.ageTracker.CurLifeStage.developmentalStage.Baby() || pawn.ageTracker.CurLifeStage.developmentalStage.Newborn() || pawn.ageTracker.CurLifeStage.developmentalStage.Juvenile())
			//              //{
			//              //    __result = Pony_DefOf.PonyBaby;
			//              //}
			//              //if (pawn.ageTracker.CurLifeStage.developmentalStage.Child())
			//              //{
			//              //    __result = Pony_DefOf.PonyChild;
			//              //}
			//              //if (pawn.ageTracker.CurLifeStage.developmentalStage.Adult())
			//              //{
			//              //    __result = Pony_DefOf.Pony;
			//              //}
			//              //}
			//      }
			//}
		}
	}
}