using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    internal class Radar
    {
        private ConcurrentDictionary<string, Threat> activeThreats;
        private int threatCounter = 0;
        private List<InterceptorBattery> activeInterceptors;



        public event Action<Threat> ThreatDetected;

        public Radar(ConcurrentDictionary<string, Threat> activeList, 
            List<InterceptorBattery> activeInterceptors)
        {
            this.activeThreats = activeList;
            this.activeInterceptors = activeInterceptors;
        }

        public void StartScanning()
        {
            Task.Run(async() =>
            {
                while (true)
                {
                    await Task.Delay(Random.Shared.Next(1000, 3000));
                    threatCounter++;
                    String id = $"Threat{threatCounter:D3}";
                    int randomX = Random.Shared.Next(-5000, 5000); 
                    int startY = Random.Shared.Next(2000, 5500);     
                    int speed = Random.Shared.Next(500, 900);
                    int targetX = 0;
                    lock (activeInterceptors)
                    {
                        if (activeInterceptors.Count > 0)
                        {
                            int RandomIndex = Random.Shared.Next(activeInterceptors.Count);
                             int batteryX = (int)activeInterceptors[RandomIndex].getX();
                            targetX= batteryX + Random.Shared.Next(-200, 200);


                        }
                        else
                        {
                             targetX = 0;
                        }
                    }
                    Threat newThreat = new Threat(id, randomX, startY, speed, targetX, 0);
                    activeThreats.TryAdd(id, newThreat);
                    ThreatDetected?.Invoke(newThreat);


                }
            });
        }
    }
}