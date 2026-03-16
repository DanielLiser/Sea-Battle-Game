using HaganaSimulator;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AirDefenseSimulator
{
    public class PhysicsEngine
    {
        private ConcurrentDictionary<string, Threat> activeThreats;
        private ConcurrentDictionary<string, Missle> activeMissles;


        public event Action<string> OnGroundImpact;
        public event Action<string, double,double> OnPhysicsUpdate;
        public event Action<string, string> OnIntercepted; 
        public event Action<string,string> OnMissileMissed; 

        public PhysicsEngine(ConcurrentDictionary<string, Threat> threats,
            ConcurrentDictionary<string, Missle> activeMissles)
        {
            this.activeThreats = threats;
            this.activeMissles = activeMissles;
        }

        public void StartSimulation()
        {
            Task.Run(async () => 
            {
                double dt = 0.1;
                while (true) 
                { 
                    foreach(var  k in activeThreats)
                    {
                        Threat threat = k.Value;
                        if (!threat.isAlive||threat.location.y<=0)
                        {
                            activeThreats.TryRemove(threat.id, out _);
                            continue;
                        }
                        threat.location.x += threat.vx * dt;
                        threat.location.y += threat.vy * dt;
                        OnPhysicsUpdate?.Invoke(threat.id, threat.location.x, threat.location.y);
                        if (threat.location.y <= 0)
                        {
                            threat.location.y = 0;  
                            threat.isAlive = false; 
                            activeThreats.TryRemove(threat.id, out _);
                            OnGroundImpact?.Invoke(threat.id);
                        }

                    }
                    foreach (var k in activeMissles)
                    {
                        Missle missle = k.Value;
                        String missleKey=k.Key;
                        missle.location.x += missle.vx * dt;
                        missle.location.y += missle.vy * dt;
                        OnPhysicsUpdate?.Invoke(missle.id, missle.location.x, missle.location.y);

                        if(activeThreats.TryGetValue(missle.threadId, out Threat targetThreat))
                        {
                            double distance=Location.distance(missle.location, targetThreat.location);

                            if (distance <= 100)
                            {
                                targetThreat.isAlive = false;
                                activeMissles.TryRemove(missleKey, out _);
                                activeThreats.TryRemove(targetThreat.id, out _);
                                OnIntercepted?.Invoke(missle.threadId, missle.id);

                            }
                            else if (missle.location.y >= targetThreat.location.y)
                            {
                                activeMissles.TryRemove(missleKey, out _);
                                lock (targetThreat.ThreatLock)
                                {
                                    targetThreat.isTargeted = false;
                                }
                                OnMissileMissed?.Invoke(missle.id,targetThreat.id);
                            }
                            
                        }
                        else
                        {
                            activeMissles.TryRemove(missleKey, out _);
                        }

            


                    }
                    await Task.Delay(100);
                }
            
            });
            
        }
    }
}