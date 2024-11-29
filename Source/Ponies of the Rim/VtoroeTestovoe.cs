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

//namespace PoniesOfTheRim
//{

//    ////[StaticConstructorOnStartup]
//    ////public static class PonyBodyTypePatch
//    ////{
//    ////    static PonyBodyTypePatch()
//    ////    {
//    ////        //new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),new System.Type[] { typeof(PawnGenerationRequest) }), null, new HarmonyMethod(typeof(PonyBodyTypePatch).GetMethod("BodyTypePatch"))) ;
//    ////        new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(HarmonyPatches), "GeneratePawnPostfix"), null, new HarmonyMethod(typeof(PonyBodyTypePatch).GetMethod("BodyTypePatch")));
//    ////    }

//    ////    [HarmonyPostfix]
//    ////    public static void BodyTypePatch(Pawn __result)
//    ////    {

//    ////        //Pawn pawn = GenerateOrRedressPawnInternal(request);
//    ////        if (__result.IsPony())
//    ////        {
//    ////            if (__result.story.hairDef.defName == "Pony_DashHair")
//    ////            {
//    ////                Log.Message("DashHair");
//    ////            }
//    ////        }
//    ////        //if (request.KindDef.race.defName == "Pony")
//    ////        //{
//    ////        //    request.KindDef.race = request.KindDef.forcedHair.defName == "Pony_DashHair";
//    ////        //    //ThingDef_AlienRace pawnrace = (ThingDef_AlienRace)__result.def;
//    ////        //    //List<AlienPartGenerator.BodyAddon> list = pawnrace.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat<AlienPartGenerator.BodyAddon>(Utilities.UniversalBodyAddons).ToList();
//    ////        //    //AlienPartGenerator.BodyAddon tailBodyAddon = list.Find(ba => ba.Name.Contains("Tail"));
//    ////        //    AlienPartGenerator.AlienComp alienComp = __result.TryGetComp<AlienPartGenerator.AlienComp>();
//    ////        //    string var = alienComp.addonVariants[0].ToString();
//    ////        //    Log.Message(var);
//    ////        //    //if (__result.IsEarthpony())
//    ////        //    //{

//    ////        //    //}

//    ////        //    //int variantE = alienComp.addonVariants[0];


//    ////        //    //if (__result.story.hairDef.defName == "Pony_DashHair")
//    ////        //    //{
//    ////        //    //    Log.Message("DashHair");
//    ////        //    //}
//    ////        //    //int index = __result.story.hairDef.index;
//    ////        //    //Log.Message(index.ToString());
//    ////        //}
//    ////    }





























//    ////    //[HarmonyPostfix]
//    ////    //public static void BodyTypePatch2(PawnGenerationRequest request)
//    ////    //{
//    ////    //    //Pawn pawn = GenerateOrRedressPawnInternal(request);
//    ////    //    if (pawn.IsPony())
//    ////    //    {
//    ////    //        if (pawn.story.hairDef.defName == "Pony_DashHair")
//    ////    //        {
//    ////    //            Log.Message("DashHair");
//    ////    //        }
//    ////    //        int index = pawn.story.hairDef.index;
//    ////    //        Log.Message(index.ToString());
//    ////    //    }
//    ////    //}
//    ////}









//    [StaticConstructorOnStartup]
//    public static class PonyBodyTypePatch
//    {
//        static PonyBodyTypePatch()
//        {
//            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(HarmonyPatches), "CheckBodyType"), null, new HarmonyMethod(typeof(PonyBodyTypePatch).GetMethod("BodyTypePatch")));
//        }

//        [HarmonyPostfix]
//        public static void BodyTypePatch(Pawn pawn, ref BodyTypeDef __result)
//        {
//            if (!pawn.IsPony())
//            {
//                return;
//            }
//            if (ModsConfig.BiotechActive)
//            {
//                if (pawn.DevelopmentalStage.Baby() || pawn.DevelopmentalStage.Newborn())
//                {
//                    __result = Pony_DefOf.PonyBaby;
//                }
//                if (pawn.DevelopmentalStage.Child())
//                {
//                    __result = Pony_DefOf.PonyChild;
//                }
//            }
//            if (pawn.DevelopmentalStage.Adult())
//            {
//                __result = Pony_DefOf.Pony;
//            }















//            //        //if (pawn.IsPony())
//            //        //{
//            //        //    if (pawn.ageTracker.CurLifeStage.developmentalStage.Baby() || pawn.ageTracker.CurLifeStage.developmentalStage.Newborn())
//            //        //    {
//            //        //        __result = Pony_DefOf.PonyBaby;
//            //        //    }
//            //        //    if (pawn.ageTracker.CurLifeStage.developmentalStage.Child())
//            //        //    {
//            //        //        __result = Pony_DefOf.PonyChild;
//            //        //    }
//            //        //    if (pawn.ageTracker.CurLifeStage.developmentalStage.Adult())
//            //        //    {
//            //        //        __result = Pony_DefOf.Pony;
//            //        //    }
//            //        //}
//            //    }
//            //}






























//            //[StaticConstructorOnStartup]
//            //public static class PonyBodyTypePatch
//            //{
//            //    static PonyBodyTypePatch()
//            //    {
//            //        new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(PawnGenerator), "GetBodyTypeFor"), null, new HarmonyMethod(typeof(PonyBodyTypePatch).GetMethod("BodyTypePatch")));
//            //    }

//            //    [HarmonyPostfix]
//            //    public static void BodyTypePatch(Pawn pawn, ref BodyTypeDef __result)
//            //    {
//            //        if (pawn.IsPony())
//            //        {
//            //            if (pawn.ageTracker.CurLifeStage.developmentalStage.Baby() || pawn.ageTracker.CurLifeStage.developmentalStage.Newborn())
//            //            {
//            //                __result = Pony_DefOf.PonyBaby;
//            //            }
//            //            if (pawn.ageTracker.CurLifeStage.developmentalStage.Child())
//            //            {
//            //                __result = Pony_DefOf.PonyChild;
//            //            }
//            //            //if (pawn.ageTracker.CurLifeStage.developmentalStage.Adult())
//            //            //{
//            //            //    __result = Pony_DefOf.Pony;
//            //            //}
//            //        }
//            //    }
//            //}
//        }
//    }
//}