using AirDefenseSimulator;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var activeThreats = new ConcurrentDictionary<String, Threat>();
            var activeMissles = new ConcurrentDictionary<String, Missle>();
            var activeInterceptors = new List<InterceptorBattery>();


            var arrowQueue = new ConcurrentQueue<Threat>();
            var ironDomeQueue = new ConcurrentQueue<Threat>();
            PhysicsEngine physics = new PhysicsEngine(activeThreats, activeMissles);
            BattleManager bmc = new BattleManager(arrowQueue, ironDomeQueue);

            Radar radar = new Radar(activeThreats,activeInterceptors);


            TcpBroadcaster tcpServer = new TcpBroadcaster();
            tcpServer.StartServer(5000);

            Action<string, string> onMissileFiredHandler = (batteryName, threatName) =>
            {
                Console.WriteLine($"[🚀 FIRED] {batteryName} launched a missile at {threatName}!");
                tcpServer.SendEvent(new { TYPE = "FIRE_MISSLE", BATTERY_NAME = batteryName, THREAT_ID = threatName });
            };

            tcpServer.OnCommandReceived += (jsonLine) =>
            {
                using JsonDocument doc = JsonDocument.Parse(jsonLine);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("COMMAND", out JsonElement cmd))
                {
                    string commandAction = cmd.GetString();
                    if (commandAction == "ADD_BATTERY")
                    {
                        string type = root.GetProperty("BAT_TYPE").GetString();
                        string name = root.GetProperty("NAME").GetString();
                        int x = root.GetProperty("X").GetInt32();
                        InterceptorBattery newBattery = null;
                        if (type == "IronDome")
                        {
                            newBattery = new IronDome(name, x, 0, 6, activeThreats, activeMissles);
                            activeInterceptors.Add(newBattery);

                        }
                        else if (type == "Arrow")
                        {
                            newBattery = new Arrow(name, x, 0, 5, activeThreats, activeMissles);
                        }
                        if (newBattery != null)
                        {
                            newBattery.OnMissileFired += onMissileFiredHandler;
                            newBattery.StartDefending();
                            lock (activeInterceptors)
                            {
                                activeInterceptors.Add(newBattery);
                            }
                        }

                    }
                    else if (commandAction == "START")
                    {
                        physics.StartSimulation();
                        radar.StartScanning();
                    }

                }
            };


                physics.OnPhysicsUpdate += (id, x, y) =>
                {
                    tcpServer.SendEvent(new { TYPE = "PHYSICS", THREAT_ID = id, X = x, Y = y });
                    Console.WriteLine($" {id}: X: {x}\n Y: {y}\n ");

                };
                physics.OnIntercepted += (threat, missle) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    tcpServer.SendEvent(new { Type = "DESTROYED", THREAT_ID = threat, MISSLE_ID = missle });
                    Console.WriteLine($" {threat} intercept by: {missle}\n");
                    Console.ResetColor();

                };
                physics.OnMissileMissed += (missle, threat) =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    tcpServer.SendEvent(new { Type = "MISSED", THREAT_ID = threat, MISSLE_ID = missle });
                    Console.WriteLine($" {missle} missed {threat} lock is off \n");
                    Console.ResetColor();


                };

                physics.OnGroundImpact += (id) =>
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[🔥 IMPACT] {id} hit the ground! We failed.");
                    tcpServer.SendEvent(new { TYPE = "IMPACT", THREAT_ID = id });
                    Console.ResetColor();
                };

                radar.ThreatDetected += (threat) =>
                {
                    bmc.RouteNewThreat(threat);
                    Console.WriteLine($"DETECTED: {threat.id} at height: {threat.location.y}");
                    tcpServer.SendEvent(new { TYPE = "DETECTED", THREAT_ID = threat.id, X = threat.location.x, Y = threat.location.y });
                };


                //radar.StartScanning();


                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(15000);
                        try
                        {
                            if (activeInterceptors.Count > 0)
                            {
                                foreach (var x in activeInterceptors)
                                {
                                    if(x is IronDome)
                                    {
                                        x.ReloadBattery(5);
                                    }
                                    else if(x is Arrow)
                                    {
                                        x.ReloadBattery(3);
                                    }
                                }
                            }
                            Console.WriteLine("\n[🚚 LOGISTICS] Refilling batteries: Arrow (+2), Iron Dome (+5)");
                          
                        }
                        catch (Exception ex)
                        {
                            // אם משהו קורס, נראה את זה פה באדום בוהק!
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"\n[🔥 CRITICAL ERROR IN LOGISTICS] {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                            Console.ResetColor();
                        }
                    }
                });
                Console.ReadLine();
            }

        }
    }

