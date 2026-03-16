using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace HaganaSimulator
{
    public class Location
    {
        public double x { get; set; }
        public double y { get; set; }
        public Location(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public static double distance(Location p1, Location p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }
    }
}
