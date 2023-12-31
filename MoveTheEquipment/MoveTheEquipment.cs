using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Railloader;
using Serilog;


namespace MoveTheEquipment
{
    public class MoveTheEquipment : PluginBase
    {
        public MoveTheEquipment()
        {
            new Harmony("Moloch.MoveTheEquipment").PatchAll(GetType().Assembly);
        }

    }
}
