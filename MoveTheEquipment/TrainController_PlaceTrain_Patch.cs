using Game.State;
using HarmonyLib;
using Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Track;
using UI.Builder;

namespace MoveTheEquipment
{
    [HarmonyPatch(typeof(TrainController), nameof(TrainController.PlaceTrain), new Type[] {typeof(List<TrackSpan>), typeof(List<CarDescriptor>), typeof(List<string>)} )]
    internal static class TrainController_PlaceTrain_Patch
    {
        static ILogger logger = Log.ForContext(typeof(TrainController_PlaceTrain_Patch));
        static void Prefix(TrainController __instance, List<TrackSpan> spans, List<CarDescriptor> descriptors, List<string> carIds)
        {
            StateManager shared = StateManager.Shared;
            GameStorage gameStorage = shared.Storage;

            string rrReportingMark = gameStorage.RailroadMark;
            bool buyingCompanyEquipment = false;

            MethodInfo carsOnSpan = typeof(TrainController).GetMethod("CarsOnSpan", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (CarDescriptor car in descriptors)
            {
                if(car.Ident.ReportingMark.Equals(rrReportingMark))
                {
                    // player is buying a new car/engine, and we should not move anything
                    logger.Information("player is buying a new car/engine, and we should not move anything");
                    buyingCompanyEquipment = true;
                    break;
                }
            }

            if(!buyingCompanyEquipment)
            {
                Location? location;

                int trackNum = 1;
                // check for non company cars on spans
                foreach(TrackSpan span in spans)
                {
                    List<Car> list = ((IEnumerable <Car>)carsOnSpan.Invoke(__instance, new object[] { span })).ToList();

                    List<CarDescriptor> equipmentDescriptors = new List<CarDescriptor>();
                    List<string> equipmentCarIds = new List<string>();
                    List<string> equipmentNames = new List<string>();

                    location = span.lower;

                    bool moveCars = true;

                    if (!location.HasValue)
                    {
                        logger.Information("Track span {0} {1} does not have a lower location to move cars to. Will not move equipment on this track span", span.name, trackNum);
                        continue;
                    }

                    Location moveToLocation = location.Value.Flipped();

                    foreach (Car car in list)
                    {
                        string carReportingMark = car.Descriptor().Ident.ReportingMark;

                        if(!carReportingMark.Equals(rrReportingMark))
                        {
                            logger.Information("There are non company cars mixed in with the company cars on {0} Track {1}", span.name, trackNum);
                            equipmentDescriptors.Clear();
                            equipmentCarIds.Clear();
                            equipmentNames.Clear();

                            moveCars = false;
                            break;
                        }

                        //if(car.Descriptor().Ident.RoadNumber.EndsWith("T"))
                        //{
                        //    logger.Information("Car on track is a tender. Moving of steam engines is not currently supported. Will not move equipment.");
                        //    equipmentDescriptors.Clear();
                        //    equipmentCarIds.Clear();
                        //    equipmentNames.Clear();

                        //    moveCars = false; 
                        //    break;
                        //}

                        equipmentDescriptors.Add(car.Descriptor());
                        equipmentCarIds.Add(car.id);
                        equipmentNames.Add(car.name);
                    }

                    if(moveCars)
                    {
                        logger.Information("Moving the following euipment on span {0} {1}: {2}", span.name, trackNum, equipmentNames);
                        MoveEquipment(__instance, moveToLocation, equipmentDescriptors, equipmentCarIds);
                    }

                    trackNum++;
                }
            }

            // continue as normal
        }

        static void MoveEquipment(TrainController trainController, Location location, List<CarDescriptor> descriptors, List<string> carIds)
        {
            foreach(string carId in carIds)
            {
                trainController.RemoveCar(carId);
            }

            trainController.PlaceTrain(location, descriptors, carIds);
        }
    }
}
