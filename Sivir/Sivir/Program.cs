using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Sivir
{
    class Program
    {
        public const string ChampionName = "Sivir";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell Qc;
        public static Spell R;

        public static float QMANA;
        public static float WMANA;
        public static float RMANA;
        //AutoPotion
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);

        //Menu
        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, 1240f);
            Qc = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, float.MaxValue);

            R = new Spell(SpellSlot.R, 25000f);

            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);
            Qc.SetSkillshot(0.25f, 90f, 1350f, true, SkillshotType.SkillshotLine);
            SpellList.Add(Q);
            SpellList.Add(W);

            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Load the orbwalker and add it to the submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();
            
            Config.AddItem(new MenuItem("farmW", "Farm W").SetValue(true));
            Config.AddItem(new MenuItem("Hit", "Hit Chance Q").SetValue(new Slider(2, 3, 0)));
            Config.AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            #region Shield
                Config.SubMenu("E Shield Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
                Config.SubMenu("E Shield Config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
                Config.SubMenu("E Shield Config").AddItem(new MenuItem("Edmg", "E dmg % hp").SetValue(new Slider(0, 100, 0))); 
            #endregion
            Config.AddItem(new MenuItem("pots", "Use pots").SetValue(true));

            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalker_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#9c3232\">S</font>ivir full automatic AI ver 1.6 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");

        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var dmg = sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            double HpLeft = ObjectManager.Player.Health - dmg;
            double HpPercentage = (dmg * 100) / ObjectManager.Player.Health;
            if (sender.IsValid<Obj_AI_Hero>() && HpPercentage >= Config.Item("Edmg").GetValue<Slider>().Value && !sender.IsValid<Obj_AI_Turret>() && sender.IsEnemy && args.Target.IsMe && !args.SData.IsAutoAttack() && Config.Item("autoE").GetValue<bool>() && E.IsReady())
            {
                E.Cast();
                //Game.PrintChat("" + HpPercentage);
            }
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                {

                    dmg = sender.GetSpellDamage(target, args.SData.Name);
                     HpLeft = target.Health - dmg;

                    if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(Q.Range) && Q.IsReady())
                    {
                        var qDmg = Q.GetDamage(target);
                        if (qDmg > HpLeft && HpLeft > 0)
                        {
                            Q.Cast(target, true);
                        }
                    }
                    
                }
            }
        }
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var Target = (Obj_AI_Hero)gapcloser.Sender;
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && Target.IsValidTarget(1000))
                E.Cast();
            return;
        }
        public static void Orbwalker_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            ManaMenager();
            var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
            if (W.IsReady() )
            {
                if (Orbwalker.ActiveMode.ToString() == "Combo" && target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA)
                    W.Cast();
                else if (target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA + QMANA)
                    W.Cast();
                else if (Orbwalker.ActiveMode.ToString() == "LaneClear" && ObjectManager.Player.Mana > RMANA + WMANA + QMANA  && (farmW() || t.IsValidTarget()))
                    W.Cast();
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            PotionMenager();
            if (Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t) * 1.9;
                    if (Orbwalking.InAutoAttackRange(t))
                        qDmg = qDmg + ObjectManager.Player.GetAutoAttackDamage(t) * 3;
                    if (qDmg  > t.Health)
                        Q.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA)
                        castQ(t);
                    else if (((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear")))
                        if (ObjectManager.Player.Mana > RMANA + WMANA + QMANA + QMANA && t.Path.Count() > 1)
                            Qc.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                        else if (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.9 )
                            castQ(t);
                        else if (ObjectManager.Player.Mana > RMANA + WMANA + QMANA + QMANA)
                            Q.CastIfWillHit(t, 2, true);
                    if (ObjectManager.Player.Mana > RMANA + QMANA + WMANA && Q.IsReady())
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall")) 
                                Q.Cast(enemy, true);
                            else
                                Q.CastIfHitchanceEquals(enemy, HitChance.Immobile, true);
                        }
                    }
                }
            }
            if (R.IsReady() && Orbwalker.ActiveMode.ToString() == "Combo" && Config.Item("autoR").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(800, TargetSelector.DamageType.Physical);
                if (ObjectManager.Player.CountEnemiesInRange(800f) > 2)
                    R.Cast();
                else if (t.IsValidTarget() && Orbwalker.GetTarget() == null && Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health && !Q.IsReady() && t.CountEnemiesInRange(800) < 3)
                    R.Cast();
            }
        }

        private static void castQ(Obj_AI_Hero target)
        {
            if (Config.Item("Hit").GetValue<Slider>().Value == 0)
                Q.Cast(target, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 1)
                Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 3 && target.Path.Count() < 2 && Math.Abs(ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 25)
                Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
        }

        public static bool farmW()
        {
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 1300, MinionTypes.All);
            int num=0;
            foreach (var minion in allMinionsW)
            {
                num++;
            }
            if (num > 4 && Config.Item("farmW").GetValue<bool>())
                return true;
            else
                return false;
        }
       
        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost;
            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.3)
            {
                QMANA = 0;
                WMANA = 0;
                RMANA = 0;
            }
        }
        public static void PotionMenager()
        {
            if (Config.Item("pots").GetValue<bool>() && !ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(700) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                        Potion.Cast();
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + QMANA)
                        ManaPotion.Cast();
                }
            }
        }

    }

}