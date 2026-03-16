using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace HaganaSimulator
{
    public class Threat
    {
        public String id { get; set; }
        public Location location { get; set; }
        public Location Targerlocation { get; set; }

        public int speed { get; set; }
        public bool isTargeted { get; set; } = false;
        public bool isAlive { get; set; } = true;
        public double vx { get; set; }
        public double vy { get; set; }

        public readonly object ThreatLock = new object();
        public Threat(String id, int startX, int startY, int speed,int targetX,int targetY)
        {
            this.id = id;
            location = new Location(startX, startY);
            Targerlocation = new Location(targetX, targetY);
            this.speed = speed;
            double time = Location.distance(this.location, this.Targerlocation)/speed;
            this.vx=(this.Targerlocation.x-this.location.x)/time;
            this.vy=(this.Targerlocation.y-this.location.y)/time;
        }
    }
}
