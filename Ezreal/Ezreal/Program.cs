using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using System.Threading;
namespace Ezreal
{
    class Program
    {
        public const string ChampionName = "Ezreal";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        public static bool attackNow = true;
        //ManaMenager
        public static float QMANA;
        public static int pathe;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static bool Esmart = false;
        public static double WCastTime = 0;
        public static double OverKill = 0;
        public static double OverFarm = 0;
        public static double lag  = 0;
        //AutoPotion
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);
        public static int Muramana = 3042;
        public static int Tear = 3070;
        public static int Manamune = 3004;
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
            Q = new Spell(SpellSlot.Q, 1180);
            Q2 = new Spell(SpellSlot.Q, 1600);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 3000f);
            R1 = new Spell(SpellSlot.R, 3000f);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.1f, 160f, 2000f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(1.1f, 200f, 2000f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(Q2);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(R1);
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


            Config.SubMenu("Iteams").AddItem(new MenuItem("mura", "Auto Muramana").SetValue(true));
            Config.SubMenu("Iteams").AddItem(new MenuItem("stack", "Stack Tear if full mana").SetValue(false));
            Config.SubMenu("Iteams").AddItem(new MenuItem("pots", "Use pots").SetValue(true));

            Config.SubMenu("E config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.SubMenu("E config").AddItem(new MenuItem("smartE", "SmartCast E key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));

            Config.SubMenu("R option").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("Rcc", "R cc").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("Raoe", "R aoe").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            Config.SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("orb", "Orbwalker target").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("qTarget", "Q Target").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("semi", "Semi-manual R target").SetValue(false));

            Config.AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.AddItem(new MenuItem("haras", "Haras over farm").SetValue(true));
            Config.AddItem(new MenuItem("wPush", "W ally (push tower)").SetValue(true));
            Config.AddItem(new MenuItem("noob", "Noob KS bronze mode").SetValue(false));
            Config.AddItem(new MenuItem("Hit", "Hit Chance Skillshot").SetValue(new Slider(2, 3, 0)));
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#008aff\">E</font>zreal full automatic AI ver 2.4 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }
        #region Farm
        public static void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            var t = TargetSelector.GetTarget(Q.Range + 300, TargetSelector.DamageType.Physical);
            var mobs = MinionManager.GetMinions(Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            foreach (var minion in allMinionsQ)
            {
                float predictedHealth = HealthPrediction.GetHealthPrediction(minion, (int)(Q.Delay + (Player.Distance(minion.ServerPosition) / Q.Speed) * 1000));
                if (predictedHealth < 5)
                    return;
                if (!Orbwalking.InAutoAttackRange(minion) && predictedHealth < Q.GetDamage(minion))
                    Q.Cast(minion);

                else if (Game.Time - OverFarm > 0.4
                    && Orbwalker.ActiveMode.ToString() == "LaneClear"
                        && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA * 3
                        && predictedHealth > ObjectManager.Player.GetAutoAttackDamage(minion)
                        && predictedHealth < Q.GetDamage(minion)
                        && (!t.IsValidTarget() || ObjectManager.Player.UnderTurret(false))
                    && Orbwalker.GetTarget() != minion)
                    Q.Cast(minion);
                else if (
                    Orbwalker.ActiveMode.ToString() == "LaneClear"
                    && ObjectManager.Player.UnderTurret(false)
                    && predictedHealth < Q.GetDamage(minion)
                    && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA
                    && predictedHealth > ObjectManager.Player.GetAutoAttackDamage(minion)
                    && Orbwalker.GetTarget() != minion)
                    Q.Cast(minion);
            }
            if (mobs.Count > 0 && Q.IsReady() && Orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                var mob = mobs[0];
                Q.Cast(mob, true);
            }
        }

        public static bool haras()
        {
            if (Config.Item("haras").GetValue<bool>())
                return true;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, ObjectManager.Player.AttackRange, MinionTypes.All);
            var haras = true;
            foreach (var minion in allMinions)
            {
                float predictedHealth = HealthPrediction.GetHealthPrediction(minion, (int)(Q.Delay));
                if (predictedHealth < ObjectManager.Player.GetAutoAttackDamage(minion) * 1.4 && Orbwalking.InAutoAttackRange(minion))
                    haras = false;
            }
            if (haras)
                return true;
            else
                return false;
        }
        #endregion
        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                Farm = true;
            else
                Farm = false;

            if (Orbwalker.GetTarget() == null)
                attackNow = true;
            if (E.IsReady())
            {
                if (Config.Item("smartE").GetValue<KeyBind>().Active)
                    Esmart = true;
                if (Esmart && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
            else
                Esmart = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && E.IsReady() && Config.Item("autoE").GetValue<bool>())
            {
                ManaMenager();
                var t2 = TargetSelector.GetTarget(950, TargetSelector.DamageType.Physical);
                var t = TargetSelector.GetTarget(1400, TargetSelector.DamageType.Physical);

                if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA
                    && ObjectManager.Player.CountEnemiesInRange(260) > 0
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3
                    && t.Position.Distance(Game.CursorPos) > t.Position.Distance(ObjectManager.Player.Position))
                {

                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                }
                else if (ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4
                    && !ObjectManager.Player.UnderTurret(true)
                    && (Game.Time - OverKill > 0.4)

                     && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3)
                {
                    if (t.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                     && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                     && Q.IsReady()
                     && Q.GetDamage(t) + E.GetDamage(t) > t.Health
                     && !Orbwalking.InAutoAttackRange(t)
                     && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                         )
                    {
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                        debug("E kill Q");
                    }
                    else if (t2.IsValidTarget()
                     && t2.Position.Distance(Game.CursorPos) + 300 < t2.Position.Distance(ObjectManager.Player.Position)
                     && ObjectManager.Player.Mana > EMANA + RMANA
                     && ObjectManager.Player.GetAutoAttackDamage(t2) + E.GetDamage(t2) > t2.Health
                     && !Orbwalking.InAutoAttackRange(t2))
                    {
                        var position = ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range);
                        if (W.IsReady())
                            W.Cast(position, true);
                        E.Cast(position, true);
                        debug("E kill aa");
                        OverKill = Game.Time;
                    }
                    else if (t.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                     && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                     && W.IsReady()
                     && W.GetDamage(t) + E.GetDamage(t) > t.Health
                     && !Orbwalking.InAutoAttackRange(t)
                     && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                         )
                    {
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                        debug("E kill W");
                    }
                }
            }

            if (Q.IsReady())
            {
                //Q.Cast(ObjectManager.Player);
                ManaMenager();
                if (Config.Item("mura").GetValue<bool>())
                {
                    int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Items.HasItem(Mur) && Items.CanUseItem(Mur) && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                    {
                        if (!ObjectManager.Player.HasBuff("Muramana"))
                            Items.UseItem(Mur);
                    }
                    else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                        Items.UseItem(Mur);
                }
                bool cast = false;
                bool wait = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(Q.Range + 100) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(Q.Delay + (Player.Distance(target.ServerPosition) / Q.Speed) * 1000));
                        var Qdmg = Q.GetDamage(target);
                        if (Qdmg > predictedHealth)
                        {
                            cast = true;
                            wait = true;
                            PredictionOutput output = R.GetPrediction(target);
                            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                            direction.Normalize();
                            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == target.SkinName || !cast)
                                    continue;
                                PredictionOutput prediction = R.GetPrediction(enemy);
                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - Player.ServerPosition;
                                Vector3 w = predictedPosition - Player.ServerPosition;
                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;
                                Vector3 pb = Player.ServerPosition + ((float)b * v);
                                float length = Vector3.Distance(predictedPosition, pb);
                                if (length < (Q.Width + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            if (cast && target.IsValidTarget(Q.Range + 100))
                            {
                                Q.Cast(target, true);
                                debug("Q ks");
                            }
                        }
                    }
                }
                var t = TargetSelector.GetTarget(Q.Range - 50, TargetSelector.DamageType.Physical);
                if (ObjectManager.Player.CountEnemiesInRange(900) == 0)
                    t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                else
                    t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget() && Q.IsReady() && !wait && attackNow)
                {
                    var qDmg = Q.GetDamage(t);
                    var wDmg = W.GetDamage(t);
                    if (qDmg * 3 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                        debug("Q noob mode");
                    else if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                        Q.Cast(t, true);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                        castQ(t);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true) && haras())
                    {
                        castQ(t);
                    }

                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                Q.Cast(enemy, true);
                            }
                        }
                    }
                }
                if (lag > 20)
                {
                    lag = 0;
                    if (Farm && attackNow && Config.Item("farmQ").GetValue<bool>() && Q.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA * 3)
                        farmQ();
                    else if (!Farm && !ObjectManager.Player.HasBuff("Recall") && Orbwalker.ActiveMode.ToString() != "Combo" && !t.IsValidTarget() && Config.Item("stack").GetValue<bool>() && (Items.HasItem(Tear) || Items.HasItem(Manamune)) && ObjectManager.Player.Mana == ObjectManager.Player.MaxMana && Q.IsReady())
                    {
                        Q.Cast(ObjectManager.Player);
                    }
                }
                lag++;
            }
            if (W.IsReady() && attackNow)
            {
                //W.Cast(ObjectManager.Player);
                ManaMenager();
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var wDmg = W.GetDamage(t);
                    if (wDmg > t.Health)
                        castW(t);
                    else if (wDmg + qDmg > t.Health && Q.IsReady())
                        castW(t);
                    else if (qDmg * 2 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                        debug("W noob mode");
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        castW(t);
                    else if (Farm && !ObjectManager.Player.UnderTurret(true) && (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8 || W.Level > Q.Level) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                        castW(t);
                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                W.Cast(enemy, true);
                            }
                        }
                    }
                }
            }
            PotionMenager();
            var tr = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("useR").GetValue<KeyBind>().Active && tr.IsValidTarget())
            {
                R.CastIfWillHit(tr, 2, true);
                R1.Cast(tr, true, true);
            }
            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && ObjectManager.Player.CountEnemiesInRange(800) == 0 && (Game.Time - OverKill > 0.6))
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(R.Range) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) &&
                        !target.HasBuffOfType(BuffType.SpellImmunity) &&
                        !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        double Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                            Rdmg = getRdmg(target);
                        var qDmg = Q.GetDamage(target);
                        var wDmg = W.GetDamage(target);
                        if (target.IsValidTarget(R.Range) && Rdmg > predictedHealth && target.CountAlliesInRange(400) == 0 )
                        {
                            if (Config.Item("hitchanceR").GetValue<bool>())
                            {
                                debug("R before" + (ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)));
                                if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 20)
                                {
                                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                                    debug("R normal High");
                                }
                            }
                            else
                            {
                                R.Cast(target, true);
                                debug("R normal");
                            }
                        }
                        else if (Rdmg > predictedHealth && target.HasBuff("Recall"))
                        {
                            R.Cast(target, true, true);
                            debug("R recall");
                        }
                        else if ((target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                         target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                         target.HasBuffOfType(BuffType.Taunt)) && Config.Item("Rcc").GetValue<bool>() &&
                            target.IsValidTarget(Q.Range + E.Range) && Rdmg * 1.8 > predictedHealth)
                        {
                                R.Cast(target, true);
                        }
                        else if (target.IsValidTarget(R.Range) && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 3, true);
                        }
                        else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg + wDmg > predictedHealth && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 2, true);
                        }
                    }
                }
            }
        }
        private static void castQ(Obj_AI_Hero target)
        {
            if (Config.Item("Hit").GetValue<Slider>().Value == 0)
                Q.Cast(target, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 1)
                Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                Q.CastIfHitchanceEquals(target, HitChance.High, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 3 && target.Path.Count() < 2 && Math.Abs(ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 10)
                Q.CastIfHitchanceEquals(target, HitChance.High, true);
        }

        private static void castW(Obj_AI_Hero target)
        {
            if (Config.Item("Hit").GetValue<Slider>().Value == 0)
                W.Cast(target, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 1)
                W.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                W.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 3 && target.Path.Count() < 2 && Math.Abs(ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 10)
                W.CastIfHitchanceEquals(target, HitChance.High, true);

        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        private static double getRdmg(Obj_AI_Hero target)
        {
            var rDmg = R.GetDamage(target);
            var dmg = 0;
            PredictionOutput output = R.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                PredictionOutput prediction = R.GetPrediction(enemy);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
            foreach (var minion in allMinionsR)
            {
                PredictionOutput prediction = R.GetPrediction(minion);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug").GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);
            if (dmg > 7)
                return rDmg * 0.7;
            else
                return rDmg - (rDmg * 0.1 * dmg);
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;
            if (Config.Item("mura").GetValue<bool>())
            {
                int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                if (args.Target.IsEnemy && args.Target.IsValid<Obj_AI_Hero>() && Items.HasItem(Mur) && Items.CanUseItem(Mur) && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                        Items.UseItem(Mur);
                }
                else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                    Items.UseItem(Mur);
            }
            if (W.IsReady() && Config.Item("wPush").GetValue<bool>() && args.Target.IsValid<Obj_AI_Turret>() && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA + WMANA + RMANA)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (!ally.IsMe && ally.IsAlly && ally.Distance(ObjectManager.Player.Position) < 600)
                        W.Cast(ally);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                    debug("E AGC");
                }
            }
            return;
        }


        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                {

                    var dmg = unit.GetSpellDamage(target, args.SData.Name);
                    double HpLeft = target.Health - dmg;
                    if (HpLeft < 0 && target.IsValidTarget() && target.IsValidTarget(R.Range))
                    {
                        OverKill = Game.Time;
                        debug("OverKill detection " + target.ChampionName);
                    }
                    if (target.IsValidTarget(Q.Range) && Q.IsReady())
                    {
                        var qDmg = Q.GetDamage(target);
                        if (qDmg > HpLeft && HpLeft > 0)
                        {
                            Q.Cast(target, true);
                            debug("Q ks OPS");
                        }
                    }
                    if ( target.IsValidTarget(W.Range) && W.IsReady())
                    {
                        var wDmg = W.GetDamage(target);
                        if (wDmg > HpLeft && HpLeft > 0)
                        {
                            W.Cast(target, true);
                            debug("W ks OPS");
                        }
                    }
                    if ( Config.Item("autoR").GetValue<bool>() && target.IsValidTarget(R.Range) && R.IsReady() && ObjectManager.Player.CountEnemiesInRange(700) == 0)
                    {
                        double rDmg = getRdmg(target);
                        if (rDmg > HpLeft && HpLeft > 0 && target.CountAlliesInRange(500) == 0)
                        {
                            if (Config.Item("hitchanceR").GetValue<bool>())
                            {
                                if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 10)
                                {
                                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                                    debug("R OPS High");
                                }
                            }
                            else
                            {
                                R.Cast(target, true);
                                debug("R OPS");
                            }
                        }
                    }
                }
            }
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost; ;

            if (Farm)
                RMANA = RMANA + ObjectManager.Player.CountEnemiesInRange(2500) * 20;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA + RMANA)
                        ManaPotion.Cast();
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {

            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            foreach (var obj in ObjectManager.Get<Obj_AI_Hero>().Where(obj => obj.Team != ObjectManager.Player.Team))
            {
                float distance = 0;
                if (obj.IsVisible)
                {
                    Vector3[] path = obj.GetPath(ObjectManager.Player.Position);
                    for (int i = 0; i < path.Length - 1; i++)
                    {
                        distance += path[i].Distance(path[i + 1]);
                    }
                    double time = distance / t.MoveSpeed;
                }
            }

            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && Q.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && W.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && E.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow);
            }
            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Pink);
            }
            if (Config.Item("semi").GetValue<bool>() && R.IsReady())
            {
                if (t.IsValidTarget())
                    Render.Circle.DrawCircle(t.Position, 100, System.Drawing.Color.Red);
            }


            if (Config.Item("noti").GetValue<bool>())
            {
                
                if (t.IsValidTarget() && R.IsReady())
                {
                    float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                    double rDamage = R.GetDamage(t);
                    if (rDamage > predictedHealth)
                        rDamage = getRdmg(t);
                    if (rDamage > predictedHealth)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        Render.Circle.DrawCircle(t.ServerPosition, 200, System.Drawing.Color.Red);
                    }
                    
                }
                var tw = TargetSelector.GetTarget(Q.Range - 50, TargetSelector.DamageType.Physical);
                if (ObjectManager.Player.CountEnemiesInRange(900) == 0)
                    tw = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                else
                    tw = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {
                    if (Config.Item("debug").GetValue<bool>())
                        Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0.5f, System.Drawing.Color.Red,  " path: " + tw.Path.Count() + " pos: " + (ObjectManager.Player.Distance(tw.ServerPosition) - ObjectManager.Player.Distance(tw.Position))  );
        
                    if (Config.Item("qTarget").GetValue<bool>())
                        Render.Circle.DrawCircle(tw.ServerPosition, 100, System.Drawing.Color.Cyan);
                    if (Q.GetDamage(tw) > tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                    else if (Q.GetDamage(tw) + W.GetDamage(tw) > tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                    else if (Q.GetDamage(tw) + Q.GetDamage(tw) + E.GetDamage(tw) > tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W + E kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}