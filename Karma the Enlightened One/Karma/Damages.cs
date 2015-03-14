using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Karma
{
    /// <summary>
    ///     Damages Class, contains damage data / calculations.
    /// </summary>
    internal class Damages
    {
        /// <summary>
        ///     Get Damage
        /// </summary>
        /// <param name="target">Target Instance</param>
        /// <param name="spellSlot">Spell Slot</param>
        /// <param name="mantra">Mantra</param>
        /// <param name="explosionOnly">Explosion Only (Q)</param>
        /// <returns></returns>
        public static double GetDamage(Obj_AI_Base target, SpellSlot spellSlot, bool mantra, bool explosionOnly = false)
        {
            switch (spellSlot)
            {
                case SpellSlot.Q:
                    return GetQDamage(target, mantra, explosionOnly);
                case SpellSlot.W:
                    return GetWDamage(target, mantra);
            }
            return 0d;
        }

        /// <summary>
        ///     Calculate Q Damage
        /// </summary>
        /// <param name="target">Target Instance</param>
        /// <param name="mantra">Mantra Active</param>
        /// <param name="explodeOnly">Explosion Damage Only</param>
        /// <returns>Damage in double units</returns>
        private static double GetQDamage(Obj_AI_Base target, bool mantra, bool explodeOnly)
        {
            var magicDamage = new[] { 80, 125, 170, 215, 260 }[Instances.Spells[SpellSlot.Q].Level - 1];
            var explosionDamage = new[] { 50, 150, 250, 350 }[Instances.Spells[SpellSlot.R].Level - 1];
            var bonusDamage = new[] { 25, 75, 125, 175 }[Instances.Spells[SpellSlot.R].Level - 1];
            var damage = magicDamage;

            if (explodeOnly)
            {
                return CalcMagicDamage(
                    Instances.Player, target, explosionDamage + (Instances.Player.TotalMagicalDamage() * .6));
            }
            if (mantra)
            {
                damage += bonusDamage + explosionDamage;
            }

            return CalcMagicDamage(Instances.Player, target, damage);
        }

        /// <summary>
        ///     Calculate W Damage
        /// </summary>
        /// <param name="target">Target Instance</param>
        /// <param name="mantra">Mantra Active</param>
        /// <returns>Damage in double units</returns>
        private static double GetWDamage(Obj_AI_Base target, bool mantra)
        {
            var magicDamage = new[] { 60, 110, 160, 210, 260 }[Instances.Spells[SpellSlot.W].Level - 1];
            var bonusDamage = new[] { 75, 150, 225, 300 }[Instances.Spells[SpellSlot.R].Level - 1];

            var damage = magicDamage;
            if (mantra)
            {
                damage += bonusDamage;
            }

            return CalcMagicDamage(Instances.Player, target, damage);
        }

        /// <summary>
        ///     Calculate Magic Damage
        /// </summary>
        /// <param name="source">Source Instance</param>
        /// <param name="target">Target Instance</param>
        /// <param name="amount">Raw Magic Damage</param>
        /// <returns>Real Magic Damage in double units</returns>
        private static double CalcMagicDamage(Obj_AI_Base source, Obj_AI_Base target, double amount)
        {
            var magicResist = target.SpellBlock;

            //Penetration cant reduce magic resist below 0
            double k;
            if (magicResist < 0)
            {
                k = 2 - 100 / (100 - magicResist);
            }
            else if ((target.SpellBlock * source.PercentMagicPenetrationMod) - source.FlatMagicPenetrationMod < 0)
            {
                k = 1;
            }
            else
            {
                k = 100 /
                    (100 + (target.SpellBlock * source.PercentMagicPenetrationMod) - source.FlatMagicPenetrationMod);
            }

            //Take into account the percent passives
            k = PassivePercentMod(source, target, k);

            k = k * (1 - target.PercentMagicReduction) * (1 + target.PercentMagicDamageMod);

            return k * amount;
        }

        /// <summary>
        ///     Add Passive Percent Mod
        /// </summary>
        /// <param name="source">Source Instance</param>
        /// <param name="target">Target Instance</param>
        /// <param name="k">Processed Magic Damage</param>
        /// <returns>Magic Damage after percent mod in double units</returns>
        private static double PassivePercentMod(Obj_AI_Base source, Obj_AI_Base target, double k)
        {
            var siegeMinionList = new List<string> { "Red_Minion_MechCannon", "Blue_Minion_MechCannon" };
            var normalMinionList = new List<string>
            {
                "Red_Minion_Wizard",
                "Blue_Minion_Wizard",
                "Red_Minion_Basic",
                "Blue_Minion_Basic"
            };

            //Minions and towers passives:
            if (source is Obj_AI_Turret)
            {
                //Siege minions receive 70% damage from turrets
                if (siegeMinionList.Contains(target.BaseSkinName))
                {
                    k = 0.7d * k;
                }

                //Normal minions take 114% more damage from towers.
                else if (normalMinionList.Contains(target.BaseSkinName))
                {
                    k = (1 / 0.875) * k;
                }

                // Turrets deal 105% damage to champions for the first attack.
                else if (target is Obj_AI_Hero)
                {
                    k = 1.05 * k;
                }
            }

            //Masteries:

            //Offensive masteries:
            var hero = source as Obj_AI_Hero;
            if (hero != null)
            {
                var sourceAsHero = hero;

                //Double edge sword:
                //  Melee champions: You deal 2% increase damage from all sources, but take 1% increase damage from all sources.
                //  Ranged champions: You deal and take 1.5% increased damage from all sources. 
                if (sourceAsHero.Masteries.Any(m => m.Page == MasteryPage.Offense && m.Id == 65 && m.Points == 1))
                {
                    if (sourceAsHero.CombatType == GameObjectCombatType.Melee)
                    {
                        k = k * 1.02d;
                    }
                    else
                    {
                        k = k * 1.015d;
                    }
                }

                //Havoc:
                //  Increases damage by 3%. 
                if (sourceAsHero.Masteries.Any(m => m.Page == MasteryPage.Offense && m.Id == 146 && m.Points == 1))
                {
                    k = k * 1.03d;
                }

                //Executioner
                //  Increases damage dealt to champions below 20 / 35 / 50% by 5%. 
                if (target is Obj_AI_Hero)
                {
                    var mastery =
                        (sourceAsHero).Masteries.FirstOrDefault(m => m.Page == MasteryPage.Offense && m.Id == 100);
                    if (mastery != null && mastery.Points >= 1 &&
                        target.Health / target.MaxHealth <= 0.05d + 0.15d * mastery.Points)
                    {
                        k = k * 1.05;
                    }
                }
            }


            if (!(target is Obj_AI_Hero))
            {
                return k;
            }

            var targetAsHero = (Obj_AI_Hero) target;

            //Defensive masteries:

            //Double edge sword:
            //     Melee champions: You deal 2% increase damage from all sources, but take 1% increase damage from all sources.
            //     Ranged champions: You deal and take 1.5% increased damage from all sources. 
            if (targetAsHero.Masteries.Any(m => m.Page == MasteryPage.Offense && m.Id == 65 && m.Points == 1))
            {
                if (target.CombatType == GameObjectCombatType.Melee)
                {
                    k = k * 1.01d;
                }
                else
                {
                    k = k * 1.015d;
                }
            }

            return k;
        }
    }
}