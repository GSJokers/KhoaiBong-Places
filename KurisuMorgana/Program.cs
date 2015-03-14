using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace KurisuMorgana
{
    internal class Program
    {
        private static Menu _menu;
        private static Spell _q, _w, _e, _r;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        static void Main(string[] args)
        {
            Console.WriteLine("Morgana injected...");
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Me.ChampionName != "Morgana")
                return;

            // set spells
            _q = new Spell(SpellSlot.Q, 1175f);
            _q.SetSkillshot(0.25f, 72f, 1400f, true, SkillshotType.SkillshotLine);

            _w = new Spell(SpellSlot.W, 900f);
            _w.SetSkillshot(0.25f, 175f, 1200f, false, SkillshotType.SkillshotCircle);

            _e = new Spell(SpellSlot.E, 750f);
            _r = new Spell(SpellSlot.R, 600f);

            _menu = new Menu("KurisuMorgana", "morgana", true);

            var orbmenu = new Menu("Morgana: Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbmenu);
            _menu.AddSubMenu(orbmenu);

            var tsmenu = new Menu("Morgana: Selector", "selector");
            TargetSelector.AddToMenu(tsmenu);
            _menu.AddSubMenu(tsmenu);

            var drmenu = new Menu("Morgana: Drawings", "drawings");
            drmenu.AddItem(new MenuItem("drawq", "Draw Q")).SetValue(true);
            drmenu.AddItem(new MenuItem("draww", "Draw W")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawe", "Draw E")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawr", "Draw R")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawkill", "Draw Killable")).SetValue(true);
            drmenu.AddItem(new MenuItem("drawtarg", "Draw Target Circle")).SetValue(true);
            drmenu.AddItem(new MenuItem("debugdmg", "Debug Combo Damage")).SetValue(false);
            _menu.AddSubMenu(drmenu);

            var spellmenu = new Menu("Morgana: Spells", "spells");

            var menuQ = new Menu("Q Menu", "qmenu");
            menuQ.AddItem(new MenuItem("hitchanceq", "Binding Hitchance ")).SetValue(new Slider(3, 1, 4));
            menuQ.AddItem(new MenuItem("useqcombo", "Use in Combo")).SetValue(true);
            menuQ.AddItem(new MenuItem("useharassq", "Use in Harass")).SetValue(true);
            menuQ.AddItem(new MenuItem("useqanti", "Use on Gapcloser")).SetValue(true);
            menuQ.AddItem(new MenuItem("useqauto", "Use on Immobile")).SetValue(true);
            menuQ.AddItem(new MenuItem("useqdash", "Use on Dashing")).SetValue(true);
            spellmenu.AddSubMenu(menuQ);

            var menuW = new Menu("W Menu", "wmenu");
            menuW.AddItem(new MenuItem("hitchancew", "Tormentsoil Hitchance ")).SetValue(new Slider(2, 1, 4));
            menuW.AddItem(new MenuItem("usewcombo", "Use in Combo")).SetValue(true);
            menuW.AddItem(new MenuItem("useharassw", "Use in Harass")).SetValue(true);       
            menuW.AddItem(new MenuItem("usewauto", "Use on Immobile")).SetValue(true);
            menuW.AddItem(new MenuItem("waitfor", "Wait for Bind or Immobile")).SetValue(true);
            menuW.AddItem(new MenuItem("calcw", "Calculated Ticks")).SetValue(new Slider(3, 1, 5));
            spellmenu.AddSubMenu(menuW);

            var menuE = new Menu("E Menu", "emenu");
            menuE.AddItem(new MenuItem("eco", "Check Minion Collision")).SetValue(false);
            menuE.AddItem(new MenuItem("eco2", "Check Hero Collision")).SetValue(false);

            // create menu per ally
            var allyMenu = new Menu("Use for", "useonwho");
            foreach (var frn in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.Team == Me.Team))
                allyMenu.AddItem(new MenuItem("useon" + frn.ChampionName, frn.ChampionName)).SetValue(true);              

            menuE.AddSubMenu(allyMenu);
   
            foreach (var ene in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.Team != Me.Team))
            {
                // create menu per enemy
                var champMenu = new Menu(ene.ChampionName, "cm" + ene.NetworkId);

                // check if spell is supported in lib
                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == ene.ChampionName))
                {
                    var skillMenu = new Menu(lib.Slot + " - " + lib.SpellMenuName, "sm" + lib.SDataName);
                    skillMenu.AddItem(new MenuItem(lib.SDataName + "on", "Enable")).SetValue(true);
                    skillMenu.AddItem(new MenuItem(lib.SDataName + "wait", "Wait Till Impact (Disabled)")).SetValue(false);
                    skillMenu.AddItem(new MenuItem(lib.SDataName + "pr", "Priority"))
                        .SetValue(new Slider(lib.DangerLevel, 1, 5));
                    champMenu.AddSubMenu(skillMenu);
                }

                menuE.AddSubMenu(champMenu);
            }

            spellmenu.AddSubMenu(menuE);

            var menuR = new Menu("R Menu", "rmenu");
            menuR.AddItem(new MenuItem("usercombo", "Enable")).SetValue(true);
            menuR.AddItem(new MenuItem("rkill", "Use in combo if killable")).SetValue(true);
            menuR.AddItem(new MenuItem("rcount", "Use in combo if enemies >= ")).SetValue(new Slider(3, 1, 5));
            menuR.AddItem(new MenuItem("useautor", "Use automatic if enemies >= ")).SetValue(new Slider(4, 2, 5));
            spellmenu.AddSubMenu(menuR);

            spellmenu.AddItem(new MenuItem("harassmana", "Harass Mana %")).SetValue(new Slider(55, 0, 99));
            _menu.AddSubMenu(spellmenu);

            var menuM = new Menu("Morgana: Misc", "morgmisc");
            foreach (var obj in ObjectManager.Get<Obj_AI_Hero>().Where(obj => obj.Team != Me.Team))
            {
                menuM.AddItem(new MenuItem("dobind" + obj.ChampionName, obj.ChampionName))
                    .SetValue(new StringList(new[] { "Dont Bind ", "Normal Bind ", "Auto Bind" }, 1));
            }

            _menu.AddSubMenu(menuM);

            _menu.AddItem(new MenuItem("combokey", "Combo (active)")).SetValue(new KeyBind(32, KeyBindType.Press));
            _menu.AddItem(new MenuItem("harasskey", "Harass (active)")).SetValue(new KeyBind('C', KeyBindType.Press));
            _menu.AddToMainMenu();

            Game.PrintChat("<font color=\"#FF9900\"><b>KurisuMorgana:</b></font> Loaded");

            // events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            try
            {
                Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception thrown KurisuMorgana: (BlackShield: {0})", e);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Me.IsValidTarget(300, false))
            {
                return;
            }

            CheckDamage(TargetSelector.GetTarget(_q.Range + 10, TargetSelector.DamageType.Magical));


            AutoCast(_menu.Item("useqdash").GetValue<bool>(),
                     _menu.Item("useqauto").GetValue<bool>(),
                     _menu.Item("usewauto").GetValue<bool>());

            if (_menu.Item("combokey").GetValue<KeyBind>().Active)
            {
                Combo(_menu.Item("useqcombo").GetValue<bool>(),
                      _menu.Item("usewcombo").GetValue<bool>(), 
                      _menu.Item("usercombo").GetValue<bool>());
            }

            if (_menu.Item("harasskey").GetValue<KeyBind>().Active)
            {
                Harass(_menu.Item("useqcombo").GetValue<bool>(),
                       _menu.Item("usewcombo").GetValue<bool>());
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Me.IsValidTarget(300, false))
            {
                var ticks = _menu.Item("calcw").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(Me.Position, Me.BoundingRadius - 50, System.Drawing.Color.White, 3);

                if (_menu.Item("drawq").GetValue<bool>())
                    Render.Circle.DrawCircle(Me.Position, _q.Range + 10, System.Drawing.Color.White, 3);
                if (_menu.Item("draww").GetValue<bool>())
                    Render.Circle.DrawCircle(Me.Position, _w.Range, System.Drawing.Color.White, 3);
                if (_menu.Item("drawe").GetValue<bool>())
                    Render.Circle.DrawCircle(Me.Position, 750f, System.Drawing.Color.White, 3);
                if (_menu.Item("drawr").GetValue<bool>())
                    Render.Circle.DrawCircle(Me.Position, _r.Range + 10, System.Drawing.Color.White, 3);

                var target = TargetSelector.GetTarget(_q.Range + 10, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget(_q.Range + 10))
                {
                    if (_menu.Item("drawtarg").GetValue<bool>())
                    {
                        Render.Circle.DrawCircle(target.Position, target.BoundingRadius - 50, System.Drawing.Color.Yellow, 6);                       
                    }

                    if (_menu.Item("drawkill").GetValue<bool>())
                    {
                        var wts = Drawing.WorldToScreen(target.Position);
                        if (_ma*3 + _mi + _mq + _guise >= target.Health)
                            Drawing.DrawText(wts[0] - 20, wts[1] + 20, System.Drawing.Color.LimeGreen, "Q Kill!");
                        else if (_ma*3 + _mi + _mw * ticks + _guise >= target.Health)
                            Drawing.DrawText(wts[0] - 20, wts[1] + 20, System.Drawing.Color.LimeGreen, "W Kill!");
                        else if (_mq + _mw * ticks + _ma * 3 + _mi + _guise >= target.Health)
                            Drawing.DrawText(wts[0] - 20, wts[1] + 20, System.Drawing.Color.LimeGreen, "Q + W Kill!");
                        else if (_mq + _mw * ticks + _ma * 3 + _mi + _mr + _guise >= target.Health)
                            Drawing.DrawText(wts[0] - 20, wts[1] + 20, System.Drawing.Color.LimeGreen, "Q + R + W Kill!");
                        else if (_mq + _mw * ticks + _ma * 3 + _mr + _mi + _guise < target.Health)
                            Drawing.DrawText(wts[0] - 20, wts[1] + 20, System.Drawing.Color.LimeGreen, "Cant Kill");
                    }

                    if (_menu.Item("debugdmg").GetValue<bool>())
                    {
                        var wts = Drawing.WorldToScreen(target.Position);
                        Drawing.DrawText(wts[0] - 75, wts[1] + 40, System.Drawing.Color.Yellow,
                                "Combo Damage: " + (float)(_ma * 3 + _mq + _mw * ticks + _mi + _mr + _guise));
                    }
                }
            }
        }

        private static void Combo(bool useq, bool usew, bool user)
        {
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTargetNoCollision(_q);
                if (qtarget.IsValidTarget(_q.Range + 10))
                {
                    var poutput = _q.GetPrediction(qtarget);
                    if (poutput.Hitchance >= (HitChance) _menu.Item("hitchanceq").GetValue<Slider>().Value + 2)
                    {
                        _q.Cast(poutput.CastPosition);
                    }
                }
            }

            if (usew && _w.IsReady())
            {             
                var wtarget = TargetSelector.GetTarget(_w.Range + 10, TargetSelector.DamageType.Magical);            
                if (wtarget.IsValidTarget(_w.Range))
                {
                    var poutput = _w.GetPrediction(wtarget);
                    if (poutput.Hitchance >= (HitChance)_menu.Item("hitchancew").GetValue<Slider>().Value + 2)
                    {
                        if (!_menu.Item("waitfor").GetValue<bool>() ||
                            _mw*_menu.Item("calcw").GetValue<Slider>().Value >= wtarget.Health)
                        {
                            _w.Cast(poutput.CastPosition);
                        }
                    }                  
                }
            }

            if (user && _r.IsReady())
            {
                var ticks = _menu.Item("calcw").GetValue<Slider>().Value;
                var rtarget = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
                if (rtarget.IsValidTarget(_r.Range))
                {
                    if (_mr + _mq + _mw * ticks + _ma * 3 + _mi + _guise >= rtarget.Health)
                    {
                        if (_menu.Item("rkill").GetValue<bool>())
                        {
                            if (_e.IsReady())
                                _e.CastOnUnit(Me);
                            _r.Cast();
                        }
                    }

                    if (Me.CountEnemiesInRange(_r.Range) >= _menu.Item("rcount").GetValue<Slider>().Value)
                    {
                        if (_e.IsReady())
                            _e.CastOnUnit(Me);

                        _r.Cast();
                    }
                }
            }
        }

        private static void Harass(bool useq, bool usew)
        {
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTargetNoCollision(_q);
                if (qtarget.IsValidTarget(_q.Range - 300))
                {
                    var poutput = _q.GetPrediction(qtarget);
                    if (poutput.Hitchance >= (HitChance)_menu.Item("hitchanceq").GetValue<Slider>().Value + 2)
                    {
                        if ((int)(Me.Mana / Me.MaxMana * 100) >= _menu.Item("harassmana").GetValue<Slider>().Value)
                            _q.Cast(poutput.CastPosition);
                    }
                }
            }

            if (usew && _w.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(_w.Range + 10, TargetSelector.DamageType.Magical);
                if (wtarget.IsValidTarget(_w.Range))
                {
                    var poutput = _w.GetPrediction(wtarget);
                    if (poutput.Hitchance >= (HitChance)_menu.Item("hitchancew").GetValue<Slider>().Value + 2)
                    {
                        if ((int)(Me.Mana / Me.MaxMana * 100) >= _menu.Item("harassmana").GetValue<Slider>().Value)
                            _w.Cast(poutput.CastPosition);
                    }
                }           
            }
        }

        private static void AutoCast(bool dashing, bool immobile, bool soil)
        {
            if (_q.IsReady())
            {
                var itarget =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(h => h.IsEnemy && h.Distance(Me.ServerPosition, true) <= _q.RangeSqr);

                if (itarget.IsValidTarget(_q.Range))
                {
                    if (dashing && _menu.Item("dobind" + itarget.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                        _q.CastIfHitchanceEquals(itarget, HitChance.Dashing);

                    if (immobile && _menu.Item("dobind" + itarget.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                        _q.CastIfHitchanceEquals(itarget, HitChance.Immobile);
                }
            }

            if (_w.IsReady() && soil)
            {
                var itarget =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(h => h.IsEnemy && h.Distance(Me.ServerPosition, true) <= _w.RangeSqr);

                if (itarget.IsValidTarget(_w.Range))
                    _w.CastIfHitchanceEquals(itarget, HitChance.Immobile);          
            }

            if (_r.IsReady())
            {
                if (Me.CountEnemiesInRange(_r.Range) >= _menu.Item("useautor").GetValue<Slider>().Value)
                {
                    if (_e.IsReady())
                        _e.CastOnUnit(Me);
                    _r.Cast();
                }           
            }
        }  

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsValidTarget(_q.Range + 10))
            {
                if (_menu.Item("useqanti").GetValue<bool>())
                {
                    var poutput = _q.GetPrediction(gapcloser.Sender);
                    if (poutput.Hitchance >= HitChance.Low)
                    {
                        _q.Cast(poutput.CastPosition);
                    }
                }
            }
        }

        private static float _mq, _mw, _mr;
        private static float _ma, _mi, _guise;
        private static void CheckDamage(Obj_AI_Base target)
        {
            if (target == null)
            {
                return;
            }

            var qready = Me.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.Ready;
            var wready = Me.Spellbook.CanUseSpell(SpellSlot.W) == SpellState.Ready;
            var rready = Me.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready;
            var iready = Me.Spellbook.CanUseSpell(Me.GetSpellSlot("summonerdot")) == SpellState.Ready;

            _ma = (float) Me.GetAutoAttackDamage(target);
            _mq = (float) (qready ? Me.GetSpellDamage(target, SpellSlot.Q) : 0);
            _mw = (float) (wready ? Me.GetSpellDamage(target, SpellSlot.W) : 0);
            _mr = (float) (rready ? Me.GetSpellDamage(target, SpellSlot.R) : 0);
            _mi = (float) (iready ? Me.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) : 0);

            _guise = (float) (Items.HasItem(3151)
                ? Me.GetItemDamage(target, Damage.DamageItems.LiandrysTorment)
                : 0);
        }

        internal static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Type == Me.Type && _e.IsReady() && sender.IsEnemy)
            {
                var attacker = ObjectManager.Get<Obj_AI_Hero>().First(x => x.NetworkId == sender.NetworkId);
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(_e.Range, false)))
                {
                    var detectRange = ally.ServerPosition + (args.End - ally.ServerPosition).Normalized() * ally.Distance(args.End);
                    if (detectRange.Distance(ally.ServerPosition) <= ally.AttackRange - ally.BoundingRadius)
                    {
                        // no linq cus i say :p
                        foreach (var lib in KurisuLib.CCList)
                        {
                            if (lib.HeroName == attacker.ChampionName && lib.Slot == attacker.GetSpellSlot(args.SData.Name))
                            {
                                var timeTillHit = lib.Delay +
                                    (int)(1000*attacker.Distance(ally.ServerPosition/args.SData.MissileSpeed));

                                if (_menu.Item(lib.SDataName + "on").GetValue<bool>() && _menu.Item("useon" + ally.ChampionName).GetValue<bool>())
                                {
                                    if (_menu.Item(lib.SDataName + "wait").GetValue<bool>())
                                    {
                                        Utility.DelayAction.Add(timeTillHit, () => _e.CastOnUnit(ally));
                                    }

                                    else
                                    {
                                        _e.CastOnUnit(ally);
                                    }
                                }
                            }
                        }
                    }
                }
            }       
        }
    }
}
