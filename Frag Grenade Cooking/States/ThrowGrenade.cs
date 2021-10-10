using UnityEngine;
using RoR2;
using UnityEngine.Networking;
using RoR2.Projectile;

namespace EntityStates.Frag_Grenade_Cooking
{
    public class ThrowGrenade : GenericProjectileBaseState
    {
        public static GameObject _projectilePrefab;
        public static float _damageCoefficient = 12f;
        public static float _force = 1000f;
        public static float _minSpread = 0f;
        public static float _maxSpread = 0f;
        public static float _baseDuration = 0.5f;
        public static float _recoilAmplitude = 0f;
        public static string _attackSoundString = "Play_commando_M2_grenade_throw";
        public static float _projectilePitchBonus = -10f;
        public static float _baseDelayBeforeFiringProjectile = 0f;
        public static float _bloom = 0f;

        public float fuseTime;

        private void LoadStatics()
        {
            projectilePrefab = _projectilePrefab;
            damageCoefficient = _damageCoefficient;
            force = _force;
            minSpread = _minSpread;
            maxSpread = _maxSpread;
            baseDuration = _baseDuration;
            recoilAmplitude = _recoilAmplitude;
            attackSoundString = _attackSoundString;
            projectilePitchBonus = _projectilePitchBonus;
            baseDelayBeforeFiringProjectile = _baseDelayBeforeFiringProjectile;
            bloom = _bloom;
        }

        public override void OnEnter()
        {
            LoadStatics();
            base.OnEnter();
        }

        public override void FireProjectile()
        {
            //Fuse time is stored in the Force field. Component fetches the real force later.
            if (base.isAuthority)
            {
                Ray aimRay = base.GetAimRay();
                aimRay = this.ModifyProjectileAimRay(aimRay);
                aimRay.direction = Util.ApplySpread(aimRay.direction, this.minSpread, this.maxSpread, 1f, 1f, 0f, this.projectilePitchBonus);
                ProjectileManager.instance.FireProjectile(this.projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageStat * this.damageCoefficient, fuseTime, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
            }
        }

        public override void PlayAnimation(float duration)
        {
            if (base.GetModelAnimator())
            {
                base.PlayAnimation("Gesture, Additive", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
                base.PlayAnimation("Gesture, Override", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
