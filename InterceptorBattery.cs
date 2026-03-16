using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace HaganaSimulator
{
    public abstract class InterceptorBattery
    {
        protected String name;
        protected Location location;
        protected SemaphoreSlim missileInventory;
        private ConcurrentDictionary<string, Threat> activeThreats;
        private ConcurrentDictionary<string, Missle> activeMissles;

        protected int missles;

        public event Action<string, string> OnMissileFired;
        public event Action<string, string> OnTargetDestroyed;

        public double getX() {return this.location.x; }

        public InterceptorBattery(string name, double x, double y, int missles,
            ConcurrentDictionary<String,Threat>activeThreats,
            ConcurrentDictionary<String,Missle>activeMissles)
        {
            this.name = name;
            this.location = new Location(x, y);
            this.missileInventory = new SemaphoreSlim(missles, missles);
            this.activeThreats = activeThreats;
            this.activeMissles = activeMissles;
            this.missles = missles;
        }

        public bool calculateInterception(Threat threat,out double interceptX, out double interceptY, out double timeToIntercept)
        {
            interceptX = 0;
            interceptY = 0;
            timeToIntercept = 0;

            // 1. חישוב המרחקים ההתחלתיים בין הסוללה (this) לאיום (threat)
            double deltaX = threat.location.x - this.location.x;
            double deltaY = threat.location.y - this.location.y;

            // שימוש בפונקציה האבסטרקטית כדי לקבל את מהירות המיירט הספציפי
            double interceptorSpeed = this.GetMissileSpeed();

            // 2. בניית המקדמים של המשוואה הריבועית
            // A*t^2 + B*t + C = 0
            double A = Math.Pow(threat.vx, 2) + Math.Pow(threat.vy, 2) - Math.Pow(interceptorSpeed, 2);
            double B = 2 * ((deltaX * threat.vx) + (deltaY * threat.vy));
            double C = Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2);

            // 3. חישוב הדיסקרימיננטה (הדלתא מתחת לשורש)
            double discriminant = Math.Pow(B, 2) - (4 * A * C);

            // אם הדלתא קטנה מאפס, אין פתרון ממשי. המיירט לעולם לא ישיג את המטרה.
            if (discriminant < 0)
            {
                return false;
            }

            // 4. מציאת פתרונות הזמן (t1, t2)
            double sqrtDiscriminant = Math.Sqrt(discriminant);
            double t1 = (-B + sqrtDiscriminant) / (2 * A);
            double t2 = (-B - sqrtDiscriminant) / (2 * A);

            // אנחנו מחפשים את הזמן החיובי הקטן ביותר (הפגיעה המוקדמת ביותר האפשרית)
            double actualTime = -1;

            if (t1 > 0 && t2 > 0)
            {
                actualTime = Math.Min(t1, t2);
            }
            else if (t1 > 0)
            {
                actualTime = t1;
            }
            else if (t2 > 0)
            {
                actualTime = t2;
            }

            // אם שני הזמנים שליליים, משמעות הדבר היא שהיירוט התאפשר רק בעבר
            if (actualTime <= 0)
            {
                return false;
            }

            // 5. חישוב נקודת המפגש המדויקת באוויר בזמן actualTime
            double futureX = threat.location.x + (threat.vx * actualTime);
            double futureY = threat.location.y + (threat.vy * actualTime);

            // בדיקת שפיות: האם היירוט קורה מתחת לאדמה?
            if (futureY <= 0)
            {
                return false; // הטיל יפגע בקרקע לפני שנספיק ליירט אותו
            }

            // 6. הצלחה! מילוי ה"קופסאות" של ה-out בנתונים שחישבנו
            interceptX = futureX;
            interceptY = futureY;
            timeToIntercept = actualTime;

            return true;
        }
        public void StartDefending()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var threat in activeThreats.Values)
                    {
                        if (threat == null || !threat.isAlive || threat.isTargeted)
                            continue;
                        if (calculateInterception(threat, out double targetX, out double targetY, out double time))
                        {
                            if (!CanIntercept(targetY)) { continue; }
                            lock (threat.ThreatLock)
                            {
                                if (threat.isTargeted)
                                {
                                    continue;
                                }
                                threat.isTargeted = true;
                            }
                            missileInventory.Wait();
                            FireMissle(threat,targetX,targetY,time);
                            break;
                        }
                    }
                    await Task.Delay(50);
                }
            });

        }

        private void FireMissle(Threat threat,double targetX,double targetY,double time)
        {
            OnMissileFired?.Invoke(name, threat.id);
            string interceptorId = this.name + "-Missle-" + Guid.NewGuid().ToString().Substring(0, 4);
            Missle missle=new Missle(interceptorId, threat.id,this.location.x,this.location.y,
                targetX,targetY,time);
            activeMissles.TryAdd(threat.id,missle);

            

        }
        public void ReloadBattery(int amount)
        {
            int currentAmmo = missileInventory.CurrentCount;
            int space = this.missles - currentAmmo;
            if (amount + currentAmmo > this.missles)
            {
                if (space > 0)
                {
                    missileInventory.Release(space);
                }
                return;
            }
            if (amount > 0)
            {
                missileInventory.Release(amount);
            }

        }
        protected abstract void HandleMiss(Threat threat);
        protected abstract double GetHitProbability();
        protected abstract double GetMissileSpeed();

        protected abstract bool CanIntercept(double targetY);
    }



}