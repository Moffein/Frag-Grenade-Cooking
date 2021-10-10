using EntityStates.Frag_Grenade_Cooking;
using RoR2.Projectile;
using UnityEngine;

namespace Frag_Grenade_Cooking.Components
{
    public class GrenadeTimer : MonoBehaviour
    {
        public void Start()
        {
            ProjectileDamage pd = base.GetComponent<ProjectileDamage>();
            ProjectileImpactExplosion pie = base.GetComponent<ProjectileImpactExplosion>();
            if (pd && pie)
            {
                pie.stopwatch = pd.force;
                pd.force = ThrowGrenade._force;
            }
            Destroy(this);
        }
    }
}
