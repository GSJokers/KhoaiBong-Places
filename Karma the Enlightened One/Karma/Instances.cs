using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

namespace Karma
{
    /// <summary>
    ///     Instances Class, contains global instances
    /// </summary>
    internal class Instances
    {
        /// <summary>
        ///     Saved Target Instance
        /// </summary>
        private static Obj_AI_Hero _target;

        /// <summary>
        ///     Saved Player Instance
        /// </summary>
        public static Obj_AI_Hero Player { get; set; }

        /// <summary>
        ///     Target
        /// </summary>
        public static Obj_AI_Hero Target
        {
            get
            {
                // If old target is invalid, get a new one.
                if (!_target.IsValidTarget(Range))
                {
                    // Return target & save -> (Saved Target)
                    return _target = TargetSelector.GetTarget(Range, TargetSelector.DamageType.Magical);
                }
                // Return (Saved Target)
                return _target;
            }
        }

        /// <summary>
        ///     Target Search Range
        /// </summary>
        public static float Range
        {
            get { return 1200f; /* Vision Range */ }
        }

        /// <summary>
        ///     Menu Instance
        /// </summary>
        public static Menu Menu { get; set; }

        /// <summary>
        ///     Orbwalker Instance
        /// </summary>
        public static Orbwalking.Orbwalker Orbwalker { get; set; }

        /// <summary>
        ///     Spells Instance
        /// </summary>
        public static Dictionary<SpellSlot, Spell> Spells { get; set; }

        /// <summary>
        ///     Items Instance
        /// </summary>
        public static Dictionary<ItemId, ItemData.Item> Items { get; set; }
    }
}