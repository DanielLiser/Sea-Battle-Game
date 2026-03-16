using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    public class Missle
    {
        public String id;
        public String threadId;
        public Location location {  get; set; }
        public double vx {  get; set; }
        public double vy {  get; set; }
        public Missle(String id,String threatId,double startX,double startY,double interceptX,double interceptY,double
             timeToIntercetpt)
        {
            this.id = id;
            this.threadId = threatId;
            this.location = new Location(startX,startY);
            double errorX = Random.Shared.NextDouble() * (1.04 - 0.96) + 0.96;
            double errorY = Random.Shared.NextDouble() * (1.04 - 0.96) + 0.96;
            this.vx=((interceptX-startX)/timeToIntercetpt)*errorX;
            this.vy=((interceptY-startY)/timeToIntercetpt)*errorY;



        }
    }
}
