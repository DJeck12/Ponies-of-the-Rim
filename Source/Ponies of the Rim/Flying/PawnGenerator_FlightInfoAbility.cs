using System;
using Verse;

namespace PoniesOfTheRim.Flying
{
    public static class Patch_PawnGenerator_FlightInfoAbility
    {
        public static void Postfix(Pawn __result)
        {
            if (__result == null)
            {
                return;
            }
            try
            {
                FlightInfoAbilityUtility.Sync(__result);
            }
            catch (Exception ex)
            {
                PonyLog.WarnCaught("Полёт: не удалось выдать значок постоянного полёта при генерации пешки — на карточке персонажа его не будет.", ex);
            }
        }
    }
}