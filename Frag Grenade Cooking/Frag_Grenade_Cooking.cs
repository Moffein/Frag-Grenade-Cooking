using BepInEx;
using RoR2;
using R2API;
using R2API.Networking;
using UnityEngine;
using EntityStates.Frag_Grenade_Cooking;
using RoR2.Skills;
using EntityStates;
using RoR2.Projectile;
using Frag_Grenade_Cooking.Components;
using R2API.Utils;
using BepInEx.Configuration;

namespace Frag_Grenade_Cooking
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Moffein.Frag_Grenade_Cooking", "Frag Grenade Cooking", "1.0.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(LoadoutAPI), nameof(PrefabAPI),  nameof(ProjectileAPI), nameof(NetworkingHelpers))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class Frag_Grenade_Cooking : BaseUnityPlugin
    {
        bool enableFalloff = false;
        public void Awake()
        {
            enableFalloff = base.Config.Bind<bool>(new ConfigDefinition("General", "Enable Blast Falloff"), false, new ConfigDescription("Enable grenade sweet spot falloff.")).Value;

            SkillLocator sk = Resources.Load<GameObject>("prefabs/characterbodies/CommandoBody").GetComponent<SkillLocator>();
            for (int i = 0; i < sk.special.skillFamily.variants.Length; i++)
            {
                if (sk.special.skillFamily.variants[i].skillDef.skillNameToken == "COMMANDO_SPECIAL_ALT1_NAME")
                {
                    LanguageAPI.Add("RISKYREBALANCE_COMMANDO_SPECIAL_ALT1_DESC", "Throw a grenade that explodes for <style=cIsDamage>1200% damage</style> after 3 seconds. Can be <style=cIsDamage>cooked</style> to explode early.");
                    CookGrenade.overcookExplosionEffectPrefab = BuildGrenadeOvercookExplosionEffect();
                    ThrowGrenade._projectilePrefab = BuildGrenadeProjectile();
                    LoadoutAPI.AddSkill(typeof(CookGrenade));
                    LoadoutAPI.AddSkill(typeof(ThrowGrenade));

                    SkillDef grenadeDef = SkillDef.CreateInstance<SkillDef>();
                    grenadeDef.activationState = new SerializableEntityStateType(typeof(CookGrenade));
                    grenadeDef.activationStateMachineName = "Weapon";
                    grenadeDef.baseMaxStock = 1;
                    grenadeDef.baseRechargeInterval = 10f;
                    grenadeDef.beginSkillCooldownOnSkillEnd = true;
                    grenadeDef.canceledFromSprinting = false;
                    grenadeDef.dontAllowPastMaxStocks = true;
                    grenadeDef.forceSprintDuringState = false;
                    grenadeDef.fullRestockOnAssign = true;
                    grenadeDef.icon = sk.special.skillFamily.variants[i].skillDef.icon;
                    grenadeDef.interruptPriority = InterruptPriority.PrioritySkill;
                    grenadeDef.isCombatSkill = true;
                    grenadeDef.keywordTokens = new string[] { };
                    grenadeDef.mustKeyPress = false;
                    grenadeDef.cancelSprintingOnActivation = true;
                    grenadeDef.rechargeStock = 1;
                    grenadeDef.requiredStock = 1;
                    grenadeDef.skillName = "Grenade";
                    grenadeDef.skillNameToken = "COMMANDO_SPECIAL_ALT1_NAME";
                    grenadeDef.skillDescriptionToken = "RISKYREBALANCE_COMMANDO_SPECIAL_ALT1_DESC";
                    grenadeDef.stockToConsume = 1;
                    LoadoutAPI.AddSkillDef(grenadeDef);
                    sk.special.skillFamily.variants[i].skillDef = grenadeDef;
                }
            }
        }

        private GameObject BuildGrenadeProjectile()
        {
            GameObject proj = Resources.Load<GameObject>("prefabs/projectiles/CommandoGrenadeProjectile").InstantiateClone("RiskyRebalanceCommandoNade", true);

            ProjectileSimple ps = proj.GetComponent<ProjectileSimple>();
            ps.lifetime = 10f;

            ProjectileImpactExplosion pie = proj.GetComponent<ProjectileImpactExplosion>();
            pie.timerAfterImpact = false;
            pie.lifetime = CookGrenade.totalFuseTime;
            pie.falloffModel = enableFalloff ? BlastAttack.FalloffModel.SweetSpot : BlastAttack.FalloffModel.None;

            ProjectileDamage pd = proj.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.Generic;

            proj.AddComponent<GrenadeTimer>();

            ProjectileAPI.Add(proj);
            return proj;
        }

        private GameObject BuildGrenadeOvercookExplosionEffect()
        {
            GameObject effect = Resources.Load<GameObject>("prefabs/effects/omnieffect/OmniExplosionVFXCommandoGrenade").InstantiateClone("RiskyRebalanceCommandoNadeOvercookEffect", false);
            EffectComponent ec = effect.GetComponent<EffectComponent>();
            ec.soundName = "Play_commando_M2_grenade_explo";
            EffectAPI.AddEffect(new EffectDef(effect));
            return effect;
        }
    }
}
