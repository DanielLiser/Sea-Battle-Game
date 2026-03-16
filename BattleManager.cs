using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HaganaSimulator
{
    internal class BattleManager
    {

        private ConcurrentQueue<Threat> arrowQueue;
        private ConcurrentQueue<Threat> ironDomeQueue;

        public event Action<string, Threat> OnCriticalMiss;

        public BattleManager(ConcurrentQueue<Threat> t1, ConcurrentQueue<Threat> t2) 
        { 
            this.arrowQueue = t1;
            this.ironDomeQueue = t2;
        }
        public void RouteNewThreat(Threat threat) 
        {
            if (threat.location.y >= 3500)
            {
                arrowQueue.Enqueue(threat);
            }
            else
            {
                ironDomeQueue.Enqueue(threat);
            }
        }
        public void RouteMissedThreat(string batteryName, Threat threat) 
        {
            threat.isTargeted = false;
            double secondsToImpact = threat.location.y / threat.speed;

            if (secondsToImpact <= 2.5)
            {
                OnCriticalMiss?.Invoke(batteryName, threat);
            }
            else
            {
                ironDomeQueue.Enqueue(threat);
            }
        }
    }
}
