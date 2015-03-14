using System.Collections.Generic;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

namespace Karma
{
    /// <summary>
    ///     Karma Class, contains main loading requirements.
    /// </summary>
    internal class Karma
    {
        public Karma(Obj_AI_Hero hero)
        {
            // Save Player Instance
            Instances.Player = hero;

            // Add Spells
            Instances.Spells = new Dictionary<SpellSlot, Spell>
            {
                { SpellSlot.Q, new Spell(SpellSlot.Q, 1050f) },
                { SpellSlot.W, new Spell(SpellSlot.W, 675f) },
                { SpellSlot.E, new Spell(SpellSlot.E, 800f) },
                { SpellSlot.R, new Spell(SpellSlot.R, 950f) }
            };

            Instances.Spells[SpellSlot.Q].SetSkillshot(.25f, 65f, 1700f, true, SkillshotType.SkillshotLine);
            Instances.Spells[SpellSlot.W].SetTargetted(.25f, float.MaxValue);
            Instances.Spells[SpellSlot.R].SetSkillshot(.25f, 80f, 1700f, true, SkillshotType.SkillshotLine); // R+Q only

            // Add Items
            Instances.Items = new Dictionary<ItemId, ItemData.Item>
            {
                { ItemId.Zhonyas_Hourglass, ItemData.Zhonyas_Hourglass },
                { ItemId.Talisman_of_Ascension, ItemData.Talisman_of_Ascension },
                { ItemId.Mikaels_Crucible, ItemData.Mikaels_Crucible },
                { ItemId.Locket_of_the_Iron_Solari, ItemData.Locket_of_the_Iron_Solari },
                { ItemId.Sightstone, ItemData.Sightstone },
                { ItemId.Ruby_Sightstone, ItemData.Ruby_Sightstone },
                { ItemId.Vision_Ward, ItemData.Vision_Ward },
                { ItemId.Stealth_Ward, ItemData.Stealth_Ward },
                { ItemId.Health_Potion, ItemData.Health_Potion },
                { ItemId.Mana_Potion, ItemData.Mana_Potion },
                { ItemId.Warding_Totem_Trinket, ItemData.Warding_Totem_Trinket },
                { ItemId.Sweeping_Lens_Trinket, ItemData.Sweeping_Lens_Trinket },
                { ItemId.Oracles_Lens_Trinket, ItemData.Oracles_Lens_Trinket },
                { ItemId.Frost_Queens_Claim, ItemData.Frost_Queens_Claim },
                { ItemId.Randuins_Omen, ItemData.Randuins_Omen }
            };

            #region OnGameUpdate

            // Add event listener onto OnGameUpdate delegate
            Game.OnGameUpdate += args =>
            {
                if (Instances.Player == null || Instances.Orbwalker == null || Instances.Menu == null)
                {
                    return;
                }

                if (Instances.Player.IsDead || Instances.Player.IsStunned)
                {
                    return;
                }

                switch (Instances.Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        Mechanics.ProcessCombo(Instances.Target);
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Mechanics.ProcessHarass(Instances.Target);
                        break;
                    case Orbwalking.OrbwalkingMode.LastHit:
                        Mechanics.ProcessLastHit();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        Mechanics.ProcessLaneClear();
                        break;
                    case Orbwalking.OrbwalkingMode.None:
                        if (Instances.Menu.Item("l33t.karma.flee.active").GetValue<bool>() &&
                            Instances.Menu.Item("l33t.karma.flee.activekey").GetValue<KeyBind>().Active)
                        {
                            Mechanics.ProcessFlee();
                        }
                        if (Instances.Menu.Item("l33t.karma.killsteal.enable").GetValue<bool>() &&
                            (!Instances.Menu.Item("l33t.karma.flee.activekey").GetValue<KeyBind>().Active) ||
                            Instances.Menu.Item("l33t.karma.killsteal.enableflee").GetValue<bool>())
                        {
                            if (Instances.Target.IsValidTarget(Instances.Range))
                            {
                                Mechanics.ProcessKillsteal(Instances.Target);
                            }
                        }
                        break;
                }
            };

            #endregion

            #region OnDraw

            Drawing.OnDraw += args =>
            {
                if (Instances.Menu.Item("l33t.karma.drawing.enable").GetValue<bool>())
                {
                    if (Instances.Menu.Item("l33t.karma.drawing.q").GetValue<Circle>().Active)
                    {
                        if (Instances.Spells[SpellSlot.Q].IsReady() &&
                            Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>() ||
                            !Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(
                                Instances.Player.Position, Instances.Spells[SpellSlot.Q].Range,
                                Instances.Menu.Item("l33t.karma.drawing.q").GetValue<Circle>().Color);
                        }
                    }
                    if (Instances.Menu.Item("l33t.karma.drawing.w").GetValue<Circle>().Active)
                    {
                        if (Instances.Spells[SpellSlot.W].IsReady() &&
                            Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>() ||
                            !Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(
                                Instances.Player.Position, Instances.Spells[SpellSlot.W].Range,
                                Instances.Menu.Item("l33t.karma.drawing.w").GetValue<Circle>().Color);
                        }
                    }
                    if (Instances.Menu.Item("l33t.karma.drawing.e").GetValue<Circle>().Active)
                    {
                        if (Instances.Spells[SpellSlot.E].IsReady() &&
                            Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>() ||
                            !Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(
                                Instances.Player.Position, Instances.Spells[SpellSlot.E].Range,
                                Instances.Menu.Item("l33t.karma.drawing.e").GetValue<Circle>().Color);
                        }
                    }
                    if (Instances.Menu.Item("l33t.karma.drawing.rq").GetValue<Circle>().Active)
                    {
                        if (Instances.Spells[SpellSlot.R].IsReady() && Instances.Spells[SpellSlot.Q].IsReady() &&
                            Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>() ||
                            !Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>())
                        {
                            if (Instances.Target.IsValidTarget())
                            {
                                Render.Circle.DrawCircle(
                                    Instances.Target.Position, 250f,
                                    Instances.Menu.Item("l33t.karma.drawing.rq").GetValue<Circle>().Color);
                            }
                        }
                    }
                    if (Instances.Menu.Item("l33t.karma.drawing.re").GetValue<Circle>().Active)
                    {
                        if (Instances.Spells[SpellSlot.R].IsReady() && Instances.Spells[SpellSlot.E].IsReady() &&
                            Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>() ||
                            !Instances.Menu.Item("l33t.karma.drawing.enablespells").GetValue<bool>())
                        {
                            Render.Circle.DrawCircle(
                                Instances.Player.Position, Instances.Spells[SpellSlot.E].Range - 200f,
                                Instances.Menu.Item("l33t.karma.drawing.re").GetValue<Circle>().Color);
                        }
                    }
                }
            };

            #endregion

            Game.PrintChat(
                "<font color=\"#800080\"><b>L33T</b></font> | <font color=\"#1762a1\">Karma</font> the Enlightened One, loaded.");
        }

        #region ExecutableLoader

        /// <summary>
        ///     Executable Caller
        /// </summary>
        /// <param name="args">Arguments</param>
        private static void Main(string[] args)
        {
            // .Loader sends empty args
            if (args != null)
            {
                // On Game Load
                CustomEvents.Game.OnGameLoad += eventArgs =>
                {
                    // If champion name does not equals Karma (class name)
                    if (!ObjectManager.Player.ChampionName.Equals((typeof(Karma)).Name))
                    {
                        return;
                    }

                    // Construct Karma
                    var exec = new Karma(ObjectManager.Player);

                    // Construct Menu
                    Instances.Menu = exec.Menu();

                    // Add Menu to Main Menu
                    Instances.Menu.AddToMainMenu();
                };
            }
        }

        #endregion

        /// <summary>
        ///     Menu Constructor
        /// </summary>
        /// <returns>Constructed Menu</returns>
        public Menu Menu()
        {
            var menu = new Menu("Karma", "l33t.karma", true);

            #region Target Selector

            var targetSelector = new Menu("Target Selector", "l33t.karma.ts");
            TargetSelector.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            #endregion

            #region Orbwalker

            var orbwalker = new Menu("Orbwalker", "l33t.karma.orbwalker");
            Instances.Orbwalker = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            #endregion

            #region Combo

            var combo = new Menu("Combo Settings", "l33t.combo");

            combo.AddItem(new MenuItem("l33t.karma.combo.q", "Use Inner Flame (Q)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.rq", "Use Soulflare (R + Q)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.w", "Use Focused Resolve (W)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.rw", "Use Renewal (R + W)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.e", "Use Inspire (E)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.re", "Use Defiance (R + E)")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.spacer0", ""));
            combo.AddItem(new MenuItem("l33t.karma.combo.useaoe", "Use Soulflare (Q) - Area of Effect")).SetValue(true);
            combo.AddItem(new MenuItem("l33t.karma.combo.minhpforrw", "Min % of Health for Renewal (R + W)"))
                .SetValue(new Slider(50));
            combo.AddItem(new MenuItem("l33t.karma.combo.minhpforre", "Min % of Health for Defiance (R + E)"))
                .SetValue(new Slider(50));
            combo.AddItem(new MenuItem("l33t.karma.combo.minalliesforre", "Min of Allies for Defiance (R + E)"))
                .SetValue(new Slider(3, 1, 5));
            combo.AddItem(new MenuItem("l33t.karma.combo.minenemiesforre", "Min of Enemies for Defiance (R + E)"))
                .SetValue(new Slider(3, 1, 5));
            combo.AddItem(new MenuItem("l33t.karma.combo.minmpforq", "Min % of Mana for Inner Flame/Soulflare"))
                .SetValue(new Slider(15));
            combo.AddItem(new MenuItem("l33t.karma.combo.minmpforw", "Min % of Mana for Focused Resolve/Renewal"))
                .SetValue(new Slider(15));
            combo.AddItem(new MenuItem("l33t.karma.combo.minmpfore", "Min % of Mana for Inspire/Defiance"))
                .SetValue(new Slider(15));

            menu.AddSubMenu(combo);

            #endregion

            #region Harass

            var harass = new Menu("Harass Settings", "l33t.karma.harass");

            harass.AddItem(new MenuItem("l33t.karma.harass.q", "Use Inner Flame (Q)")).SetValue(true);
            harass.AddItem(new MenuItem("l33t.karma.harass.rq", "Use Soulflare (R + Q)")).SetValue(true);
            harass.AddItem(new MenuItem("l33t.karma.harass.w", "Use Focused Resolve (W)")).SetValue(true);
            harass.AddItem(new MenuItem("l33t.karma.harass.rw", "Use Renewal (R + W)")).SetValue(true);
            harass.AddItem(new MenuItem("l33t.karma.harass.e", "Use Inspire (E)")).SetValue(true);
            harass.AddItem(new MenuItem("l33t.karma.harass.spacer0", ""));
            harass.AddItem(new MenuItem("l33t.karma.harass.minhprw", "Min % of Health for Renewal"))
                .SetValue(new Slider(55));
            harass.AddItem(new MenuItem("l33t.karma.harass.minmpw", "Min % of Mana for Renewal/Focued Resolve"))
                .SetValue(new Slider(35));
            harass.AddItem(new MenuItem("l33t.karma.harass.minmpq", "Min % of Mana for Inner Flame/Soulflare"))
                .SetValue(new Slider(35));

            menu.AddSubMenu(harass);

            #endregion

            #region Drawing

            var drawing = new Menu("Drawing Settings", "l33t.karma.drawing");

            drawing.AddItem(new MenuItem("l33t.karma.drawing.enable", "Enable Drawing")).SetValue(false);
            drawing.AddItem(new MenuItem("l33t.karma.drawing.enablespells", "Draw Available Spells Only"))
                .SetValue(true);
            drawing.AddItem(new MenuItem("l33t.karma.drawing.spacer0", ""));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.q", "Draw Inner Flame (Q) - Range"))
                .SetValue(new Circle(true, Color.DarkRed));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.w", "Draw Focused Resolve (W) - Range"))
                .SetValue(new Circle(true, Color.DarkBlue));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.e", "Draw Inspire (E) - Range"))
                .SetValue(new Circle(true, Color.DarkGreen));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.spacer1", ""));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.rq", "Draw Soulflare (R + Q) - AoE Range"))
                .SetValue(new Circle(true, Color.Crimson));
            drawing.AddItem(new MenuItem("l33t.karma.drawing.re", "Draw Defiance (R + E) - Range"))
                .SetValue(new Circle(true, Color.Aqua));

            menu.AddSubMenu(drawing);

            #endregion

            #region Farming

            var farming = new Menu("Farming Settings", "l33t.karma.farming");

            farming.AddItem(new MenuItem("l33t.karma.farming.lhq", "[Last Hit] Use Inner Flame (Q)")).SetValue(true);
            farming.AddItem(new MenuItem("l33t.karma.farming.lcq", "[Lane Clear] Use Inner Flame (Q)")).SetValue(true);
            farming.AddItem(new MenuItem("l33t.karma.farming.lcrq", "[Lane Clear] Use Soulflare (R + Q)"))
                .SetValue(true);
            farming.AddItem(new MenuItem("l33t.karma.farming.spacer0", ""));
            farming.AddItem(new MenuItem("l33t.karma.farming.lhminmpq", "[Last Hit] Min % of Mana for Inner Flame"))
                .SetValue(new Slider(50));
            farming.AddItem(
                new MenuItem("l33t.karma.farming.lcminmpq", "[Lane Clear] Min % of Mana for Inner Flame/Soulflare"))
                .SetValue(new Slider(50));
            farming.AddItem(
                new MenuItem("l33t.karma.farming.lcminminions", "[Lane Clear] Min number of Minions for Soulflare"))
                .SetValue(new Slider(4, 1, 8));

            menu.AddSubMenu(farming);

            #endregion

            #region Killsteal

            var ks = new Menu("Killsteal Settings", "l33t.karma.killsteal");

            ks.AddItem(new MenuItem("l33t.karma.killsteal.enable", "Use Killsteal")).SetValue(true);
            ks.AddItem(new MenuItem("l33t.karma.killsteal.q", "Use Inner Flame (Q)")).SetValue(true);
            ks.AddItem(new MenuItem("l33t.karma.killsteal.rq", "Use Soulflare (R + Q)")).SetValue(true);
            ks.AddItem(new MenuItem("l33t.karma.killsteal.spacer0", ""));
            ks.AddItem(new MenuItem("l33t.karma.killsteal.enableflee", "Use Killsteal while Fleeing")).SetValue(true);
            ks.AddItem(new MenuItem("l33t.karma.killsteal.useaoe", "Use Soulflare (Q) - Area of Effect")).SetValue(true);

            menu.AddSubMenu(ks);

            #endregion

            #region Flee

            var flee = new Menu("Flee Settings", "l33t.karma.flee");

            flee.AddItem(new MenuItem("l33t.karma.flee.active", "Use Flee")).SetValue(true);
            flee.AddItem(new MenuItem("l33t.karma.flee.activekey", "Flee Key"))
                .SetValue(new KeyBind('Z', KeyBindType.Press));
            flee.AddItem(new MenuItem("l33t.karma.flee.spacer0", ""));
            flee.AddItem(new MenuItem("l33t.karma.flee.e", "Use Inspire (E)")).SetValue(true);
            flee.AddItem(new MenuItem("l33t.karma.flee.re", "Use Defiance (R + E)")).SetValue(true);
            flee.AddItem(new MenuItem("l33t.karma.flee.minalliesforre", "Minimum Allies for Defiance (R + E)"))
                .SetValue(new Slider(3, 1, 5));

            menu.AddSubMenu(flee);

            #endregion

            #region Items

            /*var items = new Menu("Item Settings", "l33t.karma.items");

            menu.AddSubMenu(items);*/

            #endregion

            menu.AddItem(new MenuItem("l33t.karma.spacer0", ""));
            menu.AddItem(new MenuItem("l33t.karma.title", "Karma the Enlightened One"));

            return menu;
        }
    }
}