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
    [BepInPlugin("com.Moffein.Frag_Grenade_Cooking", "Frag Grenade Cooking", "1.1.2")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(LoadoutAPI), nameof(PrefabAPI),  nameof(ProjectileAPI), nameof(NetworkingHelpers), nameof(EffectAPI))]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class Frag_Grenade_Cooking : BaseUnityPlugin
    {
        bool enableFalloff = false;
        bool pauseCooldown = false;
        float damageCoefficient = 14f;
        float radius = 14f;
        int stock = 1;
        float cooldown = 10f;
        float selfDamage = 0.6f;
        float selfForce = 4500f;

        public void Awake()
        {
            damageCoefficient = base.Config.Bind<float>(new ConfigDefinition("General", "Damage"), 14f, new ConfigDescription("How much damage the skill does (Vanilla is 7.0).")).Value;
            enableFalloff = base.Config.Bind<bool>(new ConfigDefinition("General", "Enable Sweetspot Falloff"), true, new ConfigDescription("Enable grenade sweet spot falloff (Vanilla is true).")).Value;
            selfDamage = base.Config.Bind<float>(new ConfigDefinition("General", "Self Damage Percent"), 0.6f, new ConfigDescription("Percent of max HP to lose when overcooking.")).Value;
            selfForce = base.Config.Bind<float>(new ConfigDefinition("General", "Self Force"), 4500f, new ConfigDescription("Forcce applied to yourself when overcooking.")).Value;
            pauseCooldown = base.Config.Bind<bool>(new ConfigDefinition("General", "Cooking Pauses Cooldown"), false, new ConfigDescription("Prevent cooldown from ticking down while cooking.")).Value;
            cooldown = base.Config.Bind<float>(new ConfigDefinition("General", "Cooldown"), 10f, new ConfigDescription("How long it takes for nades to recharge (Vanilla is 5s).")).Value;
            stock = base.Config.Bind<int>(new ConfigDefinition("General", "Stock"), 1, new ConfigDescription("How many charges you get (Vanilla is 2).")).Value;
            radius = base.Config.Bind<float>(new ConfigDefinition("General", "Blast Radius"), 14f, new ConfigDescription("Explosion blast radius (Vanilla is 11.0).")).Value;

            ThrowGrenade._damageCoefficient = damageCoefficient;
            CookGrenade.selfHPDamagePercent = selfDamage;
            CookGrenade.selfBlastRadius = radius;

            SkillLocator sk = Resources.Load<GameObject>("prefabs/characterbodies/CommandoBody").GetComponent<SkillLocator>();
            for (int i = 0; i < sk.special.skillFamily.variants.Length; i++)
            {
                if (sk.special.skillFamily.variants[i].skillDef.skillNameToken == "COMMANDO_SPECIAL_ALT1_NAME")
                {
                    LanguageAPI.Add("MFGC_COMMANDO_SPECIAL_ALT1_DESC", "Throw a grenade that explodes for <style=cIsDamage>"+damageCoefficient.ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style> after 3 seconds. Can be <style=cIsDamage>cooked</style> to explode early.");
                    CookGrenade.overcookExplosionEffectPrefab = BuildGrenadeOvercookExplosionEffect();
                    ThrowGrenade._projectilePrefab = BuildGrenadeProjectile();
                    LoadoutAPI.AddSkill(typeof(CookGrenade));
                    LoadoutAPI.AddSkill(typeof(ThrowGrenade));

                    SkillDef grenadeDef = SkillDef.CreateInstance<SkillDef>();
                    grenadeDef.activationState = new SerializableEntityStateType(typeof(CookGrenade));
                    grenadeDef.activationStateMachineName = "Weapon";
                    grenadeDef.baseMaxStock = stock;
                    grenadeDef.baseRechargeInterval = cooldown;
                    grenadeDef.beginSkillCooldownOnSkillEnd = pauseCooldown;
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
                    grenadeDef.skillDescriptionToken = "MFGC_COMMANDO_SPECIAL_ALT1_DESC";
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
            pie.blastRadius = radius;
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
