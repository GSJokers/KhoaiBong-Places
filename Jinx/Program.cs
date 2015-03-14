using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Jinx
{
    class Program
    {
        public const string ChampionName = "Jinx";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        //ManaMenager
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static bool attackNow = true;
        public static double WCastTime = 0;
        public static double QCastTime = 0;
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

            Q = new Spell(SpellSlot.Q, float.MaxValue);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);
            R1 = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 70f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.7f, 200f, 1500f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(R1);
            Config = new Menu(ChampionName, ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();
            #region E
                Config.SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
                Config.SubMenu("E Config").AddItem(new MenuItem("comboE", "Auto E in Combo BETA").SetValue(true));
                Config.SubMenu("E Config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
                Config.SubMenu("E Config").AddItem(new MenuItem("opsE", "OnProcessSpellCastE").SetValue(true));
                Config.SubMenu("E Config").AddItem(new MenuItem("telE", "Auto E teleport").SetValue(true));
            #endregion
            #region R
                Config.SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
                Config.SubMenu("R Config").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
                Config.SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            #endregion
            Config.SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("orb", "Orbwalker target").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("wTarget", "W Target").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("semi", "Semi-manual R target").SetValue(false));

            Config.AddItem(new MenuItem("pots", "Use pots").SetValue(true));
            Config.AddItem(new MenuItem("Hit", "Hit Chance W").SetValue(new Slider(2, 3, 0)));
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#ff00d8\">J</font>inx full automatic AI ver 2.7 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                Farm = true;
            else
                Farm = false;

            if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && Config.Item("autoE").GetValue<bool>())
            {
                if (Config.Item("telE").GetValue<bool>())
                {
                    foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < E.Range && E.IsReady() && Obj.Team != Player.Team && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                    {
                        E.Cast(Object.Position, true);
                    }
                }
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range) && E.IsReady()))
                {
                    if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.HasBuff("Recall"))
                        E.Cast(enemy, true);
                    else if (enemy.HasBuffOfType(BuffType.Slow) && enemy.Path.Count() < 2)
                        E.CastIfHitchanceEquals(enemy, HitChance.VeryHigh, true);
                    else if (enemy.Path.Count() < 2 && enemy.CountEnemiesInRange(300) > 2)
                        E.CastIfHitchanceEquals(enemy, HitChance.VeryHigh, true);
                    else
                        E.CastIfHitchanceEquals(enemy, HitChance.Immobile, true);
                }
                
                var ta = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && E.IsReady() && ta.IsValidTarget(E.Range) && Config.Item("comboE").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + EMANA + WMANA && ta.Path.Count() == 1)
                {
                    if (ObjectManager.Player.Position.Distance(ta.ServerPosition) > ObjectManager.Player.Position.Distance(ta.Position))
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) < ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(E.Range))
                        {
                            E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                            debug("E run");
                        }
                    }
                    else
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) > ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(E.Range))
                        {
                            E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                            debug("E escape");
                        }
                    }
                }
            }

            if (Q.IsReady())
            {
                ManaMenager();
                if (Farm)
                    if (ObjectManager.Player.Mana > RMANA + WMANA + EMANA + 10 && !FishBoneActive)
                        farmQ();
                var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var distance = GetRealDistance(t);
                    var powPowRange = GetRealPowPowRange(t);
                    if (!FishBoneActive && !Orbwalking.InAutoAttackRange(t))
                    {
                        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && (ObjectManager.Player.Mana > RMANA + WMANA + 20 || ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health))
                            Q.Cast();
                        else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Orbwalker.GetTarget() == null && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + 20 && distance < bonusRange() + t.BoundingRadius + ObjectManager.Player.BoundingRadius)
                            Q.Cast();
                        else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && haras() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + WMANA + 20 && distance < bonusRange())
                            Q.Cast();
                    }
                }
                else if (!FishBoneActive && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && ObjectManager.Player.Mana > RMANA + WMANA + 20)
                    Q.Cast();
                else if (FishBoneActive && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && ObjectManager.Player.Mana < RMANA + WMANA + 20)
                    Q.Cast();
                else if (FishBoneActive && Farm)
                    Q.Cast();
            }

            if (W.IsReady() && (Game.Time - QCastTime > 0.6))
            {
                ManaMenager();
                bool cast = false;
                bool wait = false;

                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    
                    if (target.IsValidTarget(W.Range) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(W.Delay + (Player.Distance(target.ServerPosition) / W.Speed) * 1000));
                        var Wdmg = W.GetDamage(target);
                        if (Wdmg > predictedHealth)
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
                                if (length < (W.Width + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            if (!Orbwalking.InAutoAttackRange(target) && cast && target.IsValidTarget(W.Range) && ObjectManager.Player.CountEnemiesInRange(400) == 0 && target.Path.Count() < 2)
                            {
                                W.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                                debug("W ks");
                            }
                        }
                    }
                }

                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && W.IsReady() && !wait)
                {

                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + 10 && ObjectManager.Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                    {
                        castW(t);
                    }
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA + 40) && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.CountEnemiesInRange(bonusRange()) == 0 && haras())
                    {
                        castW(t);
                    }
                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + WMANA && ObjectManager.Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
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

            
            if ( R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    R1.Cast(t, true, true);
                }
            }

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>())
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget() && (Game.Time - WCastTime > 1) && !target.IsZombie && !target.IsDead &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        var Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                        {
                            cast = true;
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
                                if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            
                            if (cast && target.IsValidTarget(R.Range) && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius && target.CountAlliesInRange(600) == 0 && ObjectManager.Player.CountEnemiesInRange(400) == 0)
                            {

                                if (Config.Item("hitchanceR").GetValue<bool>())
                                {
                                    if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 10)
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
                            else if (cast && target.IsValidTarget(R.Range) && target.CountEnemiesInRange(200) > 2 && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius )
                            {
                                R1.Cast(target, true, true);
                                debug("R aoe 1");
                            }
                            else if (cast && target.HasBuff("Recall"))
                            {
                                R.Cast(target, true, true);
                                debug("R recall");
                            }
                        }
                    }
                }
            }
            PotionMenager();
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {

        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        { 
            var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
            if (Q.IsReady() && FishBoneActive && t.IsValidTarget())
            {
                var distance = GetRealDistance(t);
                var powPowRange = GetRealPowPowRange(t);
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && (distance < powPowRange) && (ObjectManager.Player.Mana < RMANA + WMANA + 20 || ObjectManager.Player.GetAutoAttackDamage(t) * 2 < t.Health))
                    Q.Cast();
                else if (Farm && (distance > bonusRange() || distance < powPowRange || ObjectManager.Player.Mana < RMANA + EMANA + WMANA + WMANA))
                    Q.Cast();
            }
            if (Q.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !FishBoneActive && ObjectManager.Player.Mana < RMANA + EMANA + WMANA + 30)
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
                foreach (var minion in allMinionsQ)
                {
                    if (Orbwalking.InAutoAttackRange(minion) && minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion))
                    {
                        foreach (var minion2 in allMinionsQ)
                        {
                            if (minion2.Health < ObjectManager.Player.GetAutoAttackDamage(minion2) && minion.ServerPosition.Distance(minion2.Position) < 150 && minion2.Position != minion.Position)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(ObjectManager.Player.ServerPosition, true);
                    debug("E agc");
                }
                return;
            }
            return;
        }

        public static void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) && minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion))
                {
                    Q.Cast();
                    return;
                }
            }
        }

        public static bool haras()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange(), MinionTypes.All);
            var haras = true;
            foreach (var minion in allMinionsQ)
            {
                if (minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) * 1.5 && bonusRange() > GetRealDistance(minion))
                    haras = false;
            }
            if (haras)
                return true;
            else
                return false;
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
            {
                W.CastIfHitchanceEquals(target, HitChance.VeryHigh, true); 
            } 
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            double ShouldUse = ShouldUseE(args.SData.Name);

            if (Config.Item("opsE").GetValue<bool>() && unit.Team != ObjectManager.Player.Team && ShouldUse >= 0f && unit.IsValidTarget(E.Range))
            {
                E.Cast(unit.ServerPosition, true);
                debug("E ope");
            }
            if (unit.IsMe && args.SData.Name == "JinxW")
            {
                WCastTime = Game.Time;
            }

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
             {
                 if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                 {

                     var dmg = unit.GetSpellDamage(target, args.SData.Name);
                     double HpLeft = target.Health - dmg;
                     if ( HpLeft < 0 && target.IsValidTarget())
                     {
                         QCastTime = Game.Time;
                     }
                     if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(W.Range) && W.IsReady())
                     {
                         var wDmg = W.GetDamage(target);
                         if ( wDmg > HpLeft && HpLeft > 0)
                         {
                             W.Cast(target, true);
                             WCastTime = Game.Time;
                             debug("W ks OPS");
                         }
                     }
                     var rDmg = R.GetDamage(target);
                     if (rDmg > HpLeft && HpLeft > 0 && !target.IsZombie && !target.IsDead && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius && target.IsValidTarget(R.Range) && R.IsReady() && ObjectManager.Player.CountEnemiesInRange(400) == 0)
                     {              
                         var cast = true;  
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
                             if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                 cast = false;
                         }
                         if ( cast  )
                         {
                             if (Config.Item("hitchanceR").GetValue<bool>())
                             {
                                 if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(target.ServerPosition) - ObjectManager.Player.Distance(target.Position)) > 10)
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
                     }
                     
                 }
            }
        }

        public static double ShouldUseE(string SpellName)
        {
            if (SpellName == "ThreshQ")
                return 0;
            if (SpellName == "KatarinaR")
                return 0;
            if (SpellName == "AlZaharNetherGrasp")
                return 0;
            if (SpellName == "GalioIdolOfDurand")
                return 0;
            if (SpellName == "LuxMaliceCannon")
                return 0;
            if (SpellName == "MissFortuneBulletTime")
                return 0;
            if (SpellName == "RocketGrabMissile")
                return 0;
            if (SpellName == "CaitlynPiltoverPeacemaker")
                return 0;
            if (SpellName == "EzrealTrueshotBarrage")
                return 0;
            if (SpellName == "InfiniteDuress")
                return 0;
            if (SpellName == "VelkozR")
                return 0;
            return -1;
        }

        public static float bonusRange()
        {
            return 620f + ObjectManager.Player.BoundingRadius + 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static bool FishBoneActive
        {
            get { return Math.Abs(ObjectManager.Player.AttackRange - 525f) > float.Epsilon; }
        }

        private static float GetRealPowPowRange(GameObject target)
        {
            return 610f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private static float GetRealDistance(Obj_AI_Base target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.ServerPosition) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = 10;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = WMANA - ObjectManager.Player.Level * 2;
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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA + 20)
                        ManaPotion.Cast();
                }
            }
        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && W.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Cyan);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Cyan);
            }
            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Pink);
            }
            
            if (Config.Item("noti").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                if (t.IsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(t);
                    if (rDamage > predictedHealth)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        Render.Circle.DrawCircle(t.ServerPosition, 200, System.Drawing.Color.Red);
                    } 
                    if (Config.Item("semi").GetValue<bool>())
                    {
                            Render.Circle.DrawCircle(t.Position, 100, System.Drawing.Color.Red);
                    }
                }
                var tw = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {
                    if (Config.Item("wTarget").GetValue<bool>())
                        Render.Circle.DrawCircle(tw.ServerPosition, 100, System.Drawing.Color.Cyan);
                    var wDmg = W.GetDamage(tw);
                    if (wDmg > tw.Health)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.ServerPosition, W.Range, System.Drawing.Color.Red);
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "W can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}