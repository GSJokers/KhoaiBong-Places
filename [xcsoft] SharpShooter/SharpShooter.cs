using System;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;

namespace Sharpshooter
{
    internal static class SharpShooter
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        internal static Orbwalking.Orbwalker Orbwalker;

        internal static Menu Menu;

        internal static void Load()
        {
            try 
	        {
                Console.WriteLine("[xcsoft] SharpShooter: " + Type.GetType("Sharpshooter.Champions." + Player.ChampionName).Name + " support");
	        }
	        catch
	        {
                Console.WriteLine("[xcsoft] SharpShooter: " + Player.ChampionName + " does not support");
                Game.PrintChat("<font color = \"#00D8FF\">[xcsoft] SharpShooter: </font><font color = \"#FF007F\">" + Player.ChampionName + "</font> does not support, Unload.");
		        return;
	        }

            Menu = new Menu("[xcsoft] SharpShooter", "xcsoft_sharpshooter", true);
            Orbwalker = new Orbwalking.Orbwalker(Menu.AddSubMenu(new Menu(Player.ChampionName + ": Orbwalker", "Orbwalker")));
            
            TargetSelector.AddToMenu(Menu.AddSubMenu(new Menu(Player.ChampionName + ": Target Selector", "Target Selector")));
            Menu.AddToMainMenu();

            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Combo", "Combo"));
            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Harass", "Harass"));
            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Laneclear", "Laneclear"));
            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Jungleclear", "Jungleclear"));
            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Misc", "Misc"));
            Menu.AddSubMenu(new Menu(Player.ChampionName + ": Drawings", "Drawings"));

            Type.GetType("Sharpshooter.Champions." + Player.ChampionName).GetMethod("Load").Invoke(null, null);

            Menu.SubMenu("Drawings").AddItem(new MenuItem("brank", " "));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("potxt", "--public options--"));

            Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingTarget", "AA Target").SetValue(true));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("drawMinionLastHit", "Minion Last Hit").SetValue(new Circle(true, Color.GreenYellow)));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("drawMinionNearKill", "Minion Near Kill").SetValue(new Circle(true, Color.Gray)));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("JunglePosition", "JunglePosition").SetValue(true));

            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("<font color = \"#00D8FF\">[xcsoft] SharpShooter:</font> <font color = \"#FF007F\">" + Player.ChampionName + "</font> Loaded");
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            //part of marksman

            var drawMinionLastHit = SharpShooter.Menu.Item("drawMinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = SharpShooter.Menu.Item("drawMinionNearKill").GetValue<Circle>();

            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var xMinions =
                    MinionManager.GetMinions(Player.Position, Player.AttackRange + Player.BoundingRadius + 300, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                foreach (var xMinion in xMinions)
                {
                    if (drawMinionLastHit.Active && Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color, 5);
                    else if (drawMinionNearKill.Active && Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color, 5);
                }
            }

            if (Game.MapId == (GameMapId)11 && Menu.Item("JunglePosition").GetValue<Boolean>())
            {
                const float circleRange = 100f;

                Render.Circle.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f), circleRange, Color.Blue, 5); // blue team :red
                Render.Circle.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f), circleRange, Color.Blue, 5); // blue team :blue
                Render.Circle.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f), circleRange, Color.Blue, 5); // blue team :golems
                Render.Circle.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f), circleRange, Color.Blue, 5); // blue team :wolfs
                Render.Circle.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f), circleRange, Color.Blue, 5); // blue team :wariaths

                Render.Circle.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f), circleRange, Color.Yellow, 5); // Dragon

                Render.Circle.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f), circleRange, Color.Red, 5); // red team :red
                Render.Circle.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f), circleRange, Color.Red, 5); // red team :blue
                Render.Circle.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f), circleRange, Color.Red, 5); // red team :golems
                Render.Circle.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f), circleRange, Color.Red, 5); // red team :wolfs
                Render.Circle.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f), circleRange, Color.Red, 5); // red team :wariaths                    
            }

            if (SharpShooter.Menu.Item("drawingTarget").GetValue<Boolean>())
            {
                var target = Orbwalker.GetTarget();

                if (target != null)
                    Render.Circle.DrawCircle(target.Position, target.BoundingRadius + 15, Color.Red, 6);
            }

        }
    }
}
