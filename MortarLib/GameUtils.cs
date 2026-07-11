using HarmonyLib;
using System;
using UnityEngine;
using BepInEx;
using System.Collections.Generic;

namespace MortarLib
{
    public static class GameUtils
    {
        public static class Buffs
        {
            public const string WeaponSpeedUp = "weaponSpeedUp";
            public const string WeaponSpeedDown = "weaponSpeedDown";
            public const string WeaponRecoil = "weaponRecoil";
            public const string HangingGrounded = "hangingGrounded";
            public const string Jump = "addJump";
            public const string Stamina = "addStamina";
            public const string StaminaRegen = "addStaminaRegen";
            public const string ClimbSpeed = "addClimb";
            public const string Reach = "addReach";
            public const string Armor = "damageResist";
            public const string KnockbackArmor = "knockbackResist";
            public const string Vulnerability = "damageMult";
            public const string slowMass = "massResist";
            public const string SlowTime = "slowTime";
        }
        /// <summary>
        /// Retrieves the amount of a specific buff applied to the player.
        /// </summary>
        public static float GetPlayerBuffAmount(string buffId)
        {
            var player = ENT_Player.GetPlayer();
            if (player != null && player.curBuffs != null)
            {
                var buff = player.curBuffs.GetBuff(buffId);
                return buff;
            }
            return 0f;
        }
        /// <summary>
        /// Applies damage and hit force to a denizen or prop.
        /// </summary>
        private static void ApplyDamageAndForce(Collider collider, Vector3 hitPoint, float damage, float knockback, List<string> damageTags)
        {
            ENT_Player player = ENT_Player.GetPlayer();
            ObjectTagger tagger = collider.GetComponent<ObjectTagger>();
            if (tagger == null) return;

            Hitbox hitbox = tagger.GetComponent<Hitbox>();
            GameEntity gameEntity = hitbox != null ? hitbox.GetGameEntity() : null;
            if (gameEntity != null)
            {
                tagger = gameEntity.GetTagger();
            }

            if (tagger.HasTag("Damageable"))
            {
                Damageable damageable = collider.GetComponent<Damageable>();
                if (damageable != null)
                {
                    Damageable.DamageInfo damageInfo = Damageable.DamageInfo.CreateDamageInfo(damage, "Melee", damageTags, player);
                    damageInfo.position = hitPoint;
                    damageInfo.sourceEntity = player;
                    damageInfo.sourceObject = player.gameObject;
                    damageable.Damage(damageInfo);
                }
            }

            if (knockback > 0f)
            {
                if ((tagger.HasTag("Prop") || tagger.HasTag("Denizen")) && gameEntity != null)
                {
                    gameEntity.AddForceAtPosition(Camera.main.transform.forward * (knockback * 20f), hitPoint, "");
                }
                else if (gameEntity != null)
                {
                    gameEntity.AddForce(Camera.main.transform.forward * (knockback * 10f), "");
                }
            }
        }

        private static Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        /// <summary>
        /// Get a GameObject by searching for its prefab name in the White Knuckle database. Use AssetStudio or another tool to browse available assets.
        /// </summary>
        public static GameObject GetCachedPrefab(string prefabName)
        {
            if (prefabCache.TryGetValue(prefabName, out GameObject cachedObj) && cachedObj != null)
            {
                return UnityEngine.Object.Instantiate(cachedObj);
            }

            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == prefabName)
                {
                    prefabCache[prefabName] = go;
                    return UnityEngine.Object.Instantiate(go);
                }
            }

            return null;
        }
        /// <summary>
        /// Forces a player to grab a handhold (does not ensure they will hold on)
        /// </summary>
        public static bool ForceGrabHandhold(this ENT_Player.Hand hand, Transform target, Vector3? customWorldPos = null)
        {
            if (hand == null || target == null) return false;

            CL_Handhold handholdComponent = target.GetComponent<CL_Handhold>()
                                            ?? target.GetComponentInChildren<CL_Handhold>()
                                            ?? target.GetComponentInParent<CL_Handhold>();

            if (handholdComponent == null) return false;

            Vector3 finalGrabPosition = customWorldPos ?? handholdComponent.transform.position;

            if (hand.IsHolding())
            {
                hand.DropHand(true);
            }

            hand.GrabHold(handholdComponent.transform, finalGrabPosition, null);
            return true;
        }

        /// <summary>
        /// Checks if the player currently has a specific buff.
        /// </summary>
        public static bool HasPlayerBuff(string buffId)
        {
            var player = ENT_Player.GetPlayer();
            return player != null && player.curBuffs != null && player.curBuffs.GetBuff(buffId) != 0;
        }

        /// <summary>
        /// Applies a backward recoil force to the player based on the main camera's forward direction.
        /// </summary>
        public static void ApplyRecoil(float forceMultiplier)
        {
            var player = ENT_Player.GetPlayer();
            if (player != null && Camera.main != null)
            {
                Vector3 recoilForce = -Camera.main.transform.forward * forceMultiplier;
                player.AddForce(recoilForce);
            }
        }

        /// <summary>
        /// Restores a consumed hand item to an unused state, resetting its uses and reversing use animations.
        /// </summary>
        public static void RestoreHandItemState(HandItem item, int targetUses = 3)
        {
            item.used = false;

            var setUsedMethod = Traverse.Create(item.item).Method("SetUsed", new object[] { false });
            if (setUsedMethod.MethodExists())
            {
                setUsedMethod.GetValue();
            }

            string usesStr = item.item.GetFirstDataStringByType("uses", false);
            if (!string.IsNullOrEmpty(usesStr) && int.TryParse(usesStr, out int currentUses))
            {
                if (currentUses <= 0)
                {
                    item.item.SetFirstDataStringsofType("uses", "1");
                    var usesField = Traverse.Create(item).Field("uses");
                    if (usesField.FieldExists())
                    {
                        usesField.SetValue(targetUses);
                    }
                }
            }

            if (item.anim != null)
            {
                item.anim.Rebind();
                foreach (AnimatorControllerParameter param in item.anim.parameters)
                {
                    if (param.name == "Empty" && param.type == AnimatorControllerParameterType.Bool)
                    {
                        item.anim.SetBool("Empty", false);
                    }
                    else if (param.name == "Squeeze" && param.type == AnimatorControllerParameterType.Bool)
                    {
                        item.anim.SetBool("Squeeze", false);
                    }
                    else if (param.name == "Uses" && param.type == AnimatorControllerParameterType.Int)
                    {
                        if (int.TryParse(item.item.GetFirstDataStringByType("uses", false), out int usesToSet))
                        {
                            item.anim.SetInteger("Uses", usesToSet);
                        }
                    }
                }
                item.anim.Update(0f);
            }

            var takeover = item.GetComponent<Hand_Takeover>() ?? item.GetComponentInChildren<Hand_Takeover>();
            if (takeover != null)
            {
                takeover.blend = 0f;
                var curBlendField = Traverse.Create(takeover).Field("curBlend");
                if (curBlendField.FieldExists())
                {
                    curBlendField.SetValue(0f);
                }
            }

            if (item is HandItem_BlinkEye blinkEye)
            {
                blinkEye.CancelBlink();
                if (blinkEye.crushEffect != null)
                {
                    blinkEye.crushEffect.Stop();
                }
            }

            if (item is HandItem_Food food)
            {
                food.FinishEating();
            }

            item.Refresh();
        }

        /// <summary>
        /// Plays a global sound effect at a specified position by searching for its AudioClip name.
        /// </summary>
        public static void PlaySoundByName(string clipName, Vector3 position)
        {
            var allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var clip in allClips)
            {
                if (clip.name == clipName)
                {
                    AudioManager.PlaySound(clip, position);
                    break;
                }
            }
        }

        /// <summary>
        /// Buffs or nerfs a handhold by the given amounts.
        /// </summary>
        public static void ModifyHandhold(CL_Handhold handhold, float climb = 0, float jump = 0, float staminaDrain = 0, float staminaJumpDrain = 0, float grav = 0)
        {
            if (handhold == null) return;
            handhold.climbMult += climb;
            handhold.jumpMult += jump;
            handhold.strainRate += staminaDrain;
            handhold.jumpStrainMult += staminaJumpDrain;
            handhold.climbGrav += grav;
        }
    }

    [HarmonyPatch(typeof(HandItem_Shoot), "Update")]
    public class Patch_HandItem_Shoot_Speed
    {
        static void Postfix(HandItem_Shoot __instance)
        {
            if (__instance.anim == null || __instance.hand == null) return;

            float speedMultiplier = 1f;
            speedMultiplier += GameUtils.GetPlayerBuffAmount("weaponSpeedUp");
            speedMultiplier -= GameUtils.GetPlayerBuffAmount("weaponSpeedDown");

            __instance.anim.speed = Mathf.Max(0.04f, speedMultiplier);
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), "Update")]
    public class Patch_HandItem_Melee_Speed
    {
        private static readonly AnimationCurve SlowIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 3f),
        new Keyframe(0.05f, 0f),
        new Keyframe(1f, 0.2f)
        );

        static void Postfix(HandItem_Melee __instance)
        {
            if (__instance.anim == null || __instance.hand == null) return;

            float speedMultiplier = 1f;
            speedMultiplier += GameUtils.GetPlayerBuffAmount("weaponSpeedUp");

            float reloadNegValue = GameUtils.GetPlayerBuffAmount("weaponSpeedDown");
            if (reloadNegValue > 0f)
            {
                AnimatorStateInfo stateInfo = __instance.anim.GetCurrentAnimatorStateInfo(0);
                float progress = stateInfo.normalizedTime % 1f;
                float slowIntensity = SlowIntensityCurve.Evaluate(progress);
                speedMultiplier -= (reloadNegValue * slowIntensity);
            }

            __instance.anim.speed = Mathf.Max(0.04f, speedMultiplier);
        }
    }

    [HarmonyPatch(typeof(HandItem_Shoot), "Shoot")]
    public class Patch_HandItem_Shoot_Recoil
    {
        public static void Postfix()
        {
            float recoilAmount = GameUtils.GetPlayerBuffAmount("weaponRecoil");
            if (recoilAmount > 0f)
            {
                GameUtils.ApplyRecoil(recoilAmount);
            }
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), "Hit")]
    public class Patch_HandItem_Melee_Hit_Recoil
    {
        public static void Postfix()
        {
            float recoilAmount = GameUtils.GetPlayerBuffAmount("weaponRecoil");
            if (recoilAmount > 0f)
            {
                GameUtils.ApplyRecoil(recoilAmount * 0.65f);
            }
        }
    }

    [HarmonyPatch(typeof(HandItem_Melee), "OnChargeAttack")]
    public class Patch_HandItem_Melee_Charge_Recoil
    {
        public static void Postfix()
        {
            float recoilAmount = GameUtils.GetPlayerBuffAmount("weaponRecoil");
            if (recoilAmount > 0f)
            {
                GameUtils.ApplyRecoil(recoilAmount);
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player.Hand), "DamageGripStrength")]
    public class Patch_PlayerHand_IronGrip
    {
        public static bool Prefix()
        {
            return !GameUtils.HasPlayerBuff("ironGrip");
        }
    }

    [HarmonyPatch(typeof(ENT_Player), nameof(ENT_Player.HasInfiniteCharge))]
    public class Patch_Player_InfiniteCharge
    {
        public static void Postfix(ref bool __result)
        {
            if (GameUtils.HasPlayerBuff("infiniteCharge"))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeAmount), "Execute")]
    public class Patch_RechargeAmount_Rate
    {
        public static void Prefix(ItemExecutionModule_RechargeAmount __instance, out float __state)
        {
            __state = __instance.rechargeRate;
            __instance.rechargeRate += GameUtils.GetPlayerBuffAmount("rechargeRateUp");
        }

        public static void Postfix(ItemExecutionModule_RechargeAmount __instance, float __state)
        {
            __instance.rechargeRate = __state;
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeAmount), "Execute")]
    public class Patch_RechargeAmount_Capacity
    {
        public static void Prefix(ItemExecutionModule_RechargeAmount __instance, out float __state)
        {
            __state = __instance.maxCharge;
            __instance.maxCharge += GameUtils.GetPlayerBuffAmount("maxChargeUp");
        }

        public static void Postfix(ItemExecutionModule_RechargeAmount __instance, float __state)
        {
            __instance.maxCharge = __state;
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeAmount), "Execute")]
    public class Patch_RechargeAmount_Pocket
    {
        public static void Prefix(ItemExecutionModule_RechargeAmount __instance, out bool __state)
        {
            __state = __instance.chargeWhenInHand;
            if (GameUtils.HasPlayerBuff("chargeInPocket"))
            {
                __instance.chargeWhenInHand = false;
            }
        }

        public static void Postfix(ItemExecutionModule_RechargeAmount __instance, bool __state)
        {
            __instance.chargeWhenInHand = __state;
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeUses), "Execute")]
    public class Patch_RechargeUses_Speed
    {
        public static void Prefix(ItemExecutionModule_RechargeUses __instance, ref float ___rechargeTime)
        {
            float speed = GameUtils.GetPlayerBuffAmount("useRechargeSpeedUp");
            if (speed > 0f)
            {
                Item item = Traverse.Create(__instance).Field("item").GetValue<Item>();
                if (item != null)
                {
                    ___rechargeTime -= Time.deltaTime * speed;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeUses), "Execute")]
    public class Patch_RechargeUses_Capacity
    {
        public static void Prefix(ItemExecutionModule_RechargeUses __instance, out int __state)
        {
            __state = __instance.maxUses;
            __instance.maxUses += (int)GameUtils.GetPlayerBuffAmount("maxUsesUp");
        }

        public static void Postfix(ItemExecutionModule_RechargeUses __instance, int __state)
        {
            __instance.maxUses = __state;
        }
    }

    [HarmonyPatch(typeof(ItemExecutionModule_RechargeUses), "Execute")]
    public class Patch_RechargeUses_Double
    {
        public static void Prefix(ItemExecutionModule_RechargeUses __instance, ref float ___rechargeTime, out bool __state)
        {
            __state = false;
            Item item = Traverse.Create(__instance).Field("item").GetValue<Item>();
            if (item == null) return;

            int currentUses = 0;
            int.TryParse(item.GetFirstDataStringByType(__instance.usesDataString, false), out currentUses);

            if (currentUses < __instance.maxUses && ___rechargeTime <= Time.deltaTime)
            {
                if (UnityEngine.Random.value < GameUtils.GetPlayerBuffAmount("doubleRechargeChance"))
                {
                    __state = true;
                }
            }
        }

        public static void Postfix(ItemExecutionModule_RechargeUses __instance, bool __state)
        {
            if (__state)
            {
                Item item = Traverse.Create(__instance).Field("item").GetValue<Item>();
                if (item != null)
                {
                    int currentUses = 0;
                    int.TryParse(item.GetFirstDataStringByType(__instance.usesDataString, false), out currentUses);

                    if (currentUses < __instance.maxUses)
                    {
                        currentUses++;
                        item.SetFirstDataStringsofType(__instance.usesDataString, currentUses.ToString());
                    }
                }
            }
        }
    }

    public static class Patch_PlayerGroundedSpoofer
    {
        private static bool _isUpdatingPhysics = false;

        [HarmonyPatch(typeof(ENT_Player), "Movement")]
        public static class MovementPatch
        {
            [HarmonyPrefix] public static void Prefix() { _isUpdatingPhysics = true; }
            [HarmonyPostfix] public static void Postfix() { _isUpdatingPhysics = false; }
        }

        [HarmonyPatch(typeof(ENT_Player), "IsGrounded", new Type[] { typeof(bool) })]
        public static class IsGroundedPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ENT_Player __instance, ref bool __result)
            {
                if (_isUpdatingPhysics) return true;

                if (GameUtils.HasPlayerBuff("hangingGrounded") && __instance.IsHanging())
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ENT_Player), "IsCrouching")]
        public static class IsCrouchingPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ENT_Player __instance, ref bool __result)
            {
                if (_isUpdatingPhysics) return true;

                if (GameUtils.HasPlayerBuff("hangingGrounded") && __instance.IsHanging())
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ENT_Player), "Jump")]
        public static class JumpPatch
        {
            private static bool _wasSpoofed = false;
            private static bool _realCrouching;
            private static bool _realIsGrounded;
            private static ENT_Player.PlayerState _realPlayerState;

            [HarmonyPrefix]
            public static void Prefix(ENT_Player __instance, ref bool ___crouching, ref bool ___isGrounded, ref ENT_Player.PlayerState ___playerState)
            {
                if (GameUtils.HasPlayerBuff("hangingGrounded") && __instance.IsHanging())
                {
                    foreach (var hand in __instance.hands)
                    {
                        if (hand.interactState == ENT_Player.InteractType.hanging)
                        {
                            __instance.StopInteraction(hand.id, "jump", true);
                        }
                    }

                    _realCrouching = ___crouching;
                    _realIsGrounded = ___isGrounded;
                    _realPlayerState = ___playerState;
                    _wasSpoofed = true;

                    ___crouching = true;
                    ___isGrounded = true;
                    ___playerState = ENT_Player.PlayerState.grounded;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(ref bool ___crouching, ref bool ___isGrounded, ref ENT_Player.PlayerState ___playerState)
            {
                if (_wasSpoofed)
                {
                    ___crouching = _realCrouching;
                    ___isGrounded = _realIsGrounded;
                    ___playerState = _realPlayerState;
                    _wasSpoofed = false;
                }
            }
        }
    }
}
