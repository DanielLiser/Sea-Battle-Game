using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    public class Arrow: InterceptorBattery
    {
        public event Action<string, Threat> OnTargetMissed;
        public Arrow(string name, double x, double y, int missles, ConcurrentDictionary<String, Threat> activeThreats,
            ConcurrentDictionary<String, Missle> activeMissles)
            : base(name, x, y, missles, activeThreats,activeMissles)
        {
            
        }

        protected override double GetHitProbability()
        {
            return 0.6;
        }

        protected override double GetMissileSpeed()
        {
            return 1500;
        }

        protected override void HandleMiss(Threat threat)
        {
            OnTargetMissed?.Invoke(name, threat);
        }
        protected override bool CanIntercept(double targetY)
        {
            return  targetY >= 3000;
        }
    }
}
