using System.Collections.Generic;
using UnityEngine;

namespace MortarLib
{
    public class PerkBuilder
    {
        private readonly Perk _perk;
        /// <summary>
        /// A builder class for use in creating perks.
        /// </summary>
        public PerkBuilder(string id, string title)
        {
            _perk = ScriptableObject.CreateInstance<Perk>();
            _perk.hideFlags = HideFlags.HideAndDontSave;
            _perk.id = id;
            _perk.name = $"perk_{id.Replace("_perk", "")}";
            _perk.title = title;
            _perk.description = string.Empty;
            _perk.cost = 0;
            _perk.canStack = true;
            _perk.stackMax = 100;
            _perk.useBuff = false;
            _perk.buffMultiplier = 1.0f;
            _perk.multiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            _perk.modules = new List<PerkModule>();
            _perk.buff = new BuffContainer { buffs = new List<BuffContainer.Buff>() };
        }
        /// <summary>
        /// Add a description.
        /// </summary>
        public PerkBuilder WithDescription(string description)
        {
            _perk.description = description;
            return this;
        }

        /// <summary>
        /// Make stacking possible for your perk.
        /// </summary>
        public PerkBuilder WithStacking(bool canStack, int maxStack = 100)
        {
            _perk.canStack = canStack;
            _perk.stackMax = canStack ? maxStack : 1;
            return this;
        }

        /// <summary>
        /// Add a buff that goes with your perk. For example, Ligament Restructure has addJump 0.3.
        /// </summary>
        public PerkBuilder WithBuff(string buffId, string statId, float amount, float maxAmount = -1f)
        {
            _perk.useBuff = true;
            _perk.buff.id = buffId;
            _perk.buff.loseOverTime = false;
            _perk.buff.buffTime = 1.0f;
            _perk.buff.multiplier = 1.0f;

            _perk.buff.buffs.Add(new BuffContainer.Buff
            {
                id = statId,
                amount = amount,
                maxAmount = maxAmount == -1f ? amount : maxAmount
            });

            return this;
        }

        /// <summary>
        /// Add a module to the perk. This can be done multiple times.
        /// </summary>
        public PerkBuilder AddModule(PerkModule module)
        {
            _perk.modules.Add(module);
            return this;
        }

        /// <summary>
        /// Return the perk.
        /// </summary>
        public Perk Build() => _perk;
    }
}
