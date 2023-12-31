using HarmonyLib;
using Model;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace MoveTheEquipment
{

    [HarmonyPatch(typeof(TrainPlacementHelper), "FindLocationForCutFromEnd")]
    internal static class EquipmentMover
    {
        public static void Prefix(this TrainController trainController, List<TrackSpan> spans, float requiredLength, float buffer = 10f)
        {
            Debug.Log("Checking for existing company owned cars in the interchange tracks.");
            Location? moveTo;
            foreach (TrackSpan span in spans)
            {
                List<Car> list = CarsOnSpan(trainController, span).ToList();
                Debug.Log(span.name);
                moveTo = span.lower;
                if(list.Count > 0)
                {     
                    List<string> carsToSkip = new List<string>();
                    if(moveTo.HasValue)
                    {
                        Location value = moveTo.Value;
                        
                        Car? prevCar = null;
                        foreach (Car car in list)
                        {
                            if (!carsToSkip.Contains(car.id))
                            {
                                Debug.Log($"Current location to moveTo is: {value}");
                                if (car.IsOwnedByPlayer)
                                {
                                    Debug.Log("Car is owned by player and is on track span", span);
                                    Debug.Log(car.name);
                                    if (car.IsLocomotive || car.name.Contains("T"))
                                    {
                                        Debug.Log("Car is a locomotive or a tender. Checking to see if it is a steam engine or tender");   
                                        
                                        if (car.name.Contains("T"))
                                        {
                                            // car is tender
                                            Debug.Log("Current car is a tender");
                                            Debug.LogError("Currently, moving Steam engines and tenders is not supported.");
                                            break;
                                            // if we get here, then it must be tender | steam engine
                                            //Car steamEngine = car.CoupledTo(car.EndToLogical(Car.End.F));

                                            //trainController.IntegrationSetRequestsBreakConnections(car, car.EndToLogical(Car.End.F));
                                            //if (prevCar == null)
                                            //{
                                            //    trainController.MoveCar(car, value);
                                            //}
                                            //else
                                            //{
                                            //    trainController.MoveCarCoupleTo(car, value, prevCar);
                                            //}
                                            //Debug.Log($"Moving steam engine to tender at: {car.LocationF}");
                                            //trainController.MoveCar(steamEngine, car.LocationF);
                                            //trainController.IntegrationSetRequestsReconnect(steamEngine, car);
                                            //prevCar = steamEngine;
                                            //// skip steamEngine in list of cars on track, as it was already moved
                                            //carsToSkip.Add(steamEngine.id);
                                            //value = steamEngine.LocationF;
                                            //prevCar = steamEngine;
                                            //continue;
                                        } else
                                        {
                                            Debug.Log("Current car is a engine");
                                            Car tender = car.CoupledTo(car.EndToLogical(Car.End.R));
                                            if(tender != null && tender.name.Contains("T"))
                                            {
                                                Debug.Log("Current car is a steam engine and it is connected to a tender");
                                                Debug.LogError("Currently, moving Steam engines and tenders is not supported.");
                                                break;
                                            //    trainController.IntegrationSetRequestsBreakConnections(car, car.EndToLogical(Car.End.R));
                                            //    if (prevCar == null)
                                            //    {
                                            //        trainController.MoveCar(car, value);
                                            //    }
                                            //    else
                                            //    {
                                            //        trainController.MoveCarCoupleTo(car, value, prevCar);
                                            //    }
                                            //    Debug.Log($"Moving tender to steam engine at: {car.LocationR}");
                                            //    trainController.MoveCar(tender, car.LocationR);
                                            //    trainController.IntegrationSetRequestsReconnect(car, tender);
                                            //// skip tender in list of cars on track, as it was already moved
                                            //    carsToSkip.Add(tender.id);
                                            //    prevCar = tender;
                                            //    value = tender.LocationR;
                                            //    continue;
                                            }
                                        }
                                    }
                                    if(prevCar == null)
                                    {
                                        trainController.MoveCar(car, value);
                                    } else
                                    {
                                        trainController.MoveCarCoupleTo(car, value, prevCar);
                                    }
                                    prevCar = car;

                                    value = car.LocationA;      
                                }
                                else
                                {
                                    Debug.Log("There are non company cars mixed in with the company cars. Abadoning attempt to move equipment.");
                                    break;
                                }
                            }
                           
                        }
                    }      
                }
            }

            Debug.Log("Continuing as normal.");
        }

        internal static IEnumerable<Car> CarsOnSpan(this TrainController trainController, TrackSpan span)
        {
            Car? car = null;
            foreach (Vector3 point in span.GetPoints())
            {
                Car maybeCar = trainController.CheckForCarAtPoint(point);
                if (!(maybeCar == null) && !(car == maybeCar))
                {
                    yield return maybeCar;
                    car = maybeCar;
                }
            }
        }
    }
}
