#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
using xSLx_Orbwalker;
#endregion

namespace LightningLux
{
	internal class Program
	{
		private static Menu Config;
		private static Obj_AI_Hero Target;
		private static Obj_AI_Hero Player;
		private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		private static GameObject EObject;
		private static HitChance HitC;
		private static int WallCastT;
		private static Vector2 YasuoWallCastedPos;
		private static GameObject YasuoWall;
		
		private static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}
		
		private static void Game_OnGameLoad(EventArgs args)
		{
			Player = ObjectManager.Player;
			
			if (Player.ChampionName != "Lux") return;
			
			Q = new Spell(SpellSlot.Q, 1175);
			W = new Spell(SpellSlot.W, 1075);
			E = new Spell(SpellSlot.E, 1100);
			R = new Spell(SpellSlot.R, 3340);

			Q.SetSkillshot(0.25f, 80f, 1200f, true, SkillshotType.SkillshotLine);
			W.SetSkillshot(0.25f, 150f, 1200f, false, SkillshotType.SkillshotLine);
			E.SetSkillshot(0.15f, 275f, 1300f, false, SkillshotType.SkillshotCircle);
			R.SetSkillshot(1.35f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);
			
			IgniteSlot = Player.GetSpellSlot("SummonerDot");
			
			Config = new Menu("Lightning Lux", "Lightning Lux", true);
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			TargetSelector.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
//			Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
//			var orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
			
			var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
			xSLxOrbwalker.AddToMenu(Menu_Orbwalker);
			Config.AddSubMenu(Menu_Orbwalker);
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R if Killable").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseItems", "Use Items").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
			
			Config.AddSubMenu(new Menu("Harass", "Harass"));
			Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
			
			Config.AddSubMenu(new Menu("Farm", "Farm"));
			Config.SubMenu("Farm").AddItem(new MenuItem("FarmActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("JungSteal", "JungSteal!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FMP", "My MP %").SetValue(new Slider(15,100,0)));
			
			Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseQ", "Use Q").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseE", "Use E").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseR", "Use R").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KIgnite", "Use Ignite").SetValue(true));
			
			Config.AddSubMenu(new Menu("AutoShield", "AutoShield"));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("AutoW", "Auto W when Lux is targeted").SetValue(true));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("WAllies", "Auto W for Allies").SetValue(true));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("HP", "W for Allies if HP < %").SetValue(new Slider(60,100,0)));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("Mikael", "Use Mikael").SetValue(false));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("Iron", "Use Iron Solari").SetValue(false));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("ItemHP", "Items for Allies if HP < %").SetValue(new Slider(15,100,0)));
			Config.SubMenu("AutoShield").AddItem(new MenuItem("MP", "My MP %").SetValue(new Slider(30,100,0)));
			
			Config.AddSubMenu(new Menu("ExtraSettings", "ExtraSettings"));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQE", "Only E if Target trapped").SetValue(false));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("AutoE2", "Auto pop E").SetValue(true));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQGap", "Q on GapCloser").SetValue(true));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("TargetInvul", "Don't use skillshots on Invul target").SetValue(false));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("YasuoWall", "Don't use skillshots on Yasuo's Wall").SetValue(false));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("HitChance", "HitChance").SetValue(new StringList(new[] {"Low","Medium","High","Very High"},2)));
			Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(false));
			
			Config.AddSubMenu(new Menu("UltSettings", "UltSettings"));
			Config.SubMenu("UltSettings").AddItem(new MenuItem("RHit", "Auto R if hit").SetValue(new StringList(new[] {"None","2 Target","3 Target","4 Target","5 Target"},0)));
			Config.SubMenu("UltSettings").AddItem(new MenuItem("RTrap", "Auto R if trapped").SetValue(false));
			
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.AddToMainMenu();
			
			Game.PrintChat("Lightning Lux loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			xSLxOrbwalker.BeforeAttack += Orbwalking_BeforeAttack;
//			Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
			Drawing.OnDraw += Drawing_OnDraw;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
			GameObject.OnCreate += OnCreateObject;
			GameObject.OnDelete += OnDeleteObject;
		}
		
		private static bool GetBool(string s)
		{
			return Config.Item(s).GetValue<bool>();
		}
		
		private static bool GetActive(string s)
		{
			return Config.Item(s).GetValue<KeyBind>().Active;
		}
		
		private static LeagueSharp.Common.Circle GetCircle(string s)
		{
			return Config.Item(s).GetValue<Circle>();
		}
		
		private static int GetSlider(string s)
		{
			return Config.Item(s).GetValue<Slider>().Value;
		}
		
		private static int GetSelected(string s)
		{
			return Config.Item(s).GetValue<StringList>().SelectedIndex;
		}
		
		private static void Game_OnGameUpdate(EventArgs args)
		{
			KillSteal();
			
			if (GetBool("WAllies") && W.IsReady()) AutoShield();
			if (GetBool("AutoE2")) CastE2();
			if (GetBool("RTrap") && R.IsReady()) RTrapped();
			
			if (R.IsReady())
			{
				if (GetSelected("RHit") == 1) RHit(2);
				else if (GetSelected("RHit") == 2) RHit(3);
				else if (GetSelected("RHit") == 3) RHit(4);
				else if (GetSelected("RHit") == 4) RHit(5);
			}
			
			Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
			
			if (GetActive("ComboActive")) UseCombo();
			else if (GetActive("HarassActive")) Harass();
			else if (GetActive("FarmActive")) Farm();
			else if (GetActive("JungSteal")) JungSteal();
			
			if (GetSelected("HitChance") == 0) HitC = HitChance.Low;
			else if (GetSelected("HitChance") == 1) HitC = HitChance.Medium;
			else if (GetSelected("HitChance") == 2) HitC = HitChance.High;
			else if (GetSelected("HitChance") == 3) HitC = HitChance.VeryHigh;
			
		}
		
		private static void OnCreateObject(GameObject obj, EventArgs args)
		{
			if (EObject == null && obj.Name.Contains("LuxLightstrike_tar_green"))
			{
				EObject = obj;
			}
			if (YasuoWall == null && obj != null && obj.IsValid && Player.Distance(obj.Position) < 1500 &&
			    System.Text.RegularExpressions.Regex.IsMatch(obj.Name, "_w_windwall.\\.troy",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				YasuoWall = obj;
		}
		
		private static void OnDeleteObject(GameObject obj, EventArgs args)
		{
			if (EObject != null && obj.Name.Contains("LuxLightstrike_tar_green"))
			{
				EObject = null;
			}
			if (YasuoWall != null && obj != null && obj.IsValid && Player.Distance(obj.Position) < 1500 &&
			    System.Text.RegularExpressions.Regex.IsMatch(obj.Name, "_w_windwall.\\.troy",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				YasuoWall = null;
		}
		
		private static Obj_AI_Hero GrabAlly(string s)
		{
			Obj_AI_Hero Ally = null;
			foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(W.Range) && hero.IsAlly ) let heroPercent = hero.Health/hero.MaxHealth*100 let shieldPercent = GetSlider(s) where heroPercent <= shieldPercent select hero)
			{
				Ally = hero;
				break;
			}
			return Ally;
		}
		
		private static void RHit(int x)
		{
			var rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
			R.CastIfWillHit(rtarget,x,GetBool("UsePacket"));
		}
		
		private static void RTrapped()
		{
			foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && !hero.IsDead && hero.HasBuff("LuxLightBindingMis")))
			{
				R.Cast(hero,GetBool("UsePacket"));
				return;
			}
		}
		
		private static bool NotYasuoWall(Obj_AI_Hero target)
		{
			if (YasuoWall==null || !GetBool("YasuoWall"))
				return true;
			else
			{
				var level = YasuoWall.Name.Substring(YasuoWall.Name.Length - 6, 1);
				var wallWidth = (300 + 50 * Convert.ToInt32(level));
				var wallDirection = (YasuoWall.Position.To2D() - YasuoWallCastedPos).Normalized().Perpendicular();
				var wallStart = YasuoWall.Position.To2D() + wallWidth / 2 * wallDirection;
				var wallEnd = wallStart - wallWidth * wallDirection;
				var intersection = Geometry.Intersection(wallStart, wallEnd, Player.Position.To2D(), target.Position.To2D());
				var intersections = new List<Vector2>();
				if (intersection.Point.IsValid() && Environment.TickCount + Game.Ping - WallCastT < 4000)
					return false;
				else
					return true;
			}
		}
		
		private static bool IsOnTheLine(Vector3 point, Vector3 start, Vector3 end)
		{
			var obj = Geometry.ProjectOn(point.To2D(),start.To2D(),end.To2D());
			if (obj.IsOnSegment) return true;
			return false;
		}
		
		private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsEnemy && sender.Type == GameObjectType.obj_AI_Minion)
			{
				if (GetBool("FW") && W.IsReady() && GetActive("FarmActive") && args.Target.Name == Player.Name && Player.Mana/Player.MaxMana*100 >= GetSlider("MP") )
				{
					if (GrabAlly("HP") == null) W.Cast(sender,GetBool("UsePacket"));
					else W.CastIfHitchanceEquals(GrabAlly("HP"), HitC ,GetBool("UsePacket"));
				}
				
			}
			if (GetBool("AutoW") && W.IsReady() && sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret ))
			{
				if ( (args.SData.Name != null && IsOnTheLine(Player.Position,args.Start,args.End)) || (args.Target == Player && Player.Distance(sender) <= 450) || args.Target == Player && Utility.UnderTurret(Player,true))
				{
					if (GrabAlly("HP") == null) W.Cast(sender,GetBool("UsePacket"));
					else W.CastIfHitchanceEquals(GrabAlly("HP"), HitC ,GetBool("UsePacket"));
				}
			}
			if (sender.IsValid && sender.IsEnemy && args.SData.Name == "YasuoWMovingWall")
			{
				WallCastT = Environment.TickCount;
				YasuoWallCastedPos = sender.ServerPosition.To2D();
			}
			if (!GetBool("Mikeal") && !GetBool("Iron")) return;
			Items.Item Mikael = new Items.Item(3222,750);;
			Items.Item Iron = new Items.Item(3190,600);;
			if (!Mikael.IsReady() || !Iron.IsReady()) return;
			if (sender.IsValid && sender.IsEnemy)
			{
				var allies = GrabAlly("ItemHP");
				if (allies != null && args.Target == allies)
				{
					if (Utility.CountEnemiesInRange(allies,1200) > 0 && !Utility.InFountain(allies))
					{
						if (Mikael.IsReady() && Mikael.IsInRange(allies)) Mikael.Cast(allies);
						if (Iron.IsReady() && Iron.IsInRange(allies)) Iron.Cast();
					}
					else if (sender.Type == GameObjectType.obj_AI_Turret && Utility.UnderTurret(allies,true))
					{
						if (Mikael.IsReady() && Mikael.IsInRange(allies)) Mikael.Cast(allies);
						if (Iron.IsReady() && Iron.IsInRange(allies)) Iron.Cast();
					}
				}
				else if (allies == null && args.Target == Player && !Utility.InFountain(Player))
				{
					if (Player.HealthPercentage() <= GetSlider("ItemHP") && Utility.CountEnemiesInRange(Player,1200) > 0)
					{
						if (Mikael.IsReady()) Mikael.Cast(Player);
						if (Iron.IsReady()) Iron.Cast();
					}
				}
			}
		}
		
		private static void Drawing_OnDraw(EventArgs args)
		{
			if (GetCircle("QRange").Active && !Player.IsDead)
			{
				Render.Circle.DrawCircle(Player.Position, Q.Range, GetCircle("QRange").Color);
			}
			if (GetCircle("WRange").Active && !Player.IsDead)
			{
				Render.Circle.DrawCircle(Player.Position, W.Range, GetCircle("WRange").Color);
			}
			if (GetCircle("ERange").Active && !Player.IsDead)
			{
				Render.Circle.DrawCircle(Player.Position, E.Range, GetCircle("ERange").Color);
			}
			if (GetActive("JungSteal") && !Player.IsDead)
			{
				Render.Circle.DrawCircle(Game.CursorPos, 900, Color.White);
			}
		}
		
		private static void Orbwalking_BeforeAttack(xSLxOrbwalker.BeforeAttackEventArgs args)
		{
			if (GetActive("ComboActive"))
				args.Process = (!Q.IsReady() || !W.IsReady() || !E.IsReady() || Player.Distance(args.Target) >= 550);
		}
		
		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
		{
			if (Player.HasBuff("Recall") || Player.IsWindingUp) return;
			if (GetBool("UseQGap") && Q.IsReady() && GetDistanceSqr(Player,gapcloser.Sender) <= Q.Range * Q.Range)
				Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High,GetBool("UsePacket"));
		}
		
		private static void AutoShield()
		{
			if (Player.ManaPercentage() >= GetSlider("MP") && GrabAlly("HP") != null)
				W.CastIfHitchanceEquals(GrabAlly("HP"), HitC ,GetBool("UsePacket"));
		}
				
		private static bool IgniteKillable(Obj_AI_Base target)
		{
			return Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite) >= target.Health;
		}
		
		private static float GetDistanceSqr(Obj_AI_Hero source, Obj_AI_Base target)
		{
			return Vector2.DistanceSquared(source.Position.To2D(),target.ServerPosition.To2D());
		}
		
		private static double ComboDmg(Obj_AI_Hero target)
		{
			double damage = 0;
			if (Q.IsReady() && Q.IsInRange(target.Position,Q.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.Q);
			if (E.IsReady() && E.IsInRange(target.Position,E.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.E);
			if (R.IsReady() && R.IsInRange(target.Position,R.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.R);
			if ((Items.HasItem(3128) && Items.CanUseItem(3128)) || (Items.HasItem(3188) && Items.CanUseItem(3188)) && GetDistanceSqr(Player,target) <= 750 * 750)
				damage += damage * 1.2;
			if (CanIgnite() && Player.Distance(target) <= 600) damage += Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite);
			return damage;
		}
		
		private static bool CanIgnite()
		{
			return (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready);
		}
		
		private static bool IsInvul(Obj_AI_Hero enemy)
		{
			if ((enemy.HasBuff("JudicatorIntervention") || enemy.HasBuff("Undying Rage")) && GetBool("TargetInvul"))
				return true;
			return false;
		}
		
		public static bool IsFacing(Obj_AI_Base source, Obj_AI_Base target)
		{
			if (source == null || target == null)
			{
				return false;
			}

			const float angle = 90;
			return source.Direction.To2D().AngleBetween((target.Position - source.Position).To2D()) < angle;
		}
		
		private static void UseCombo()
		{
			if (Target == null) return;
			
			bool AllSkills = false;
			
			if (ComboDmg(Target) >= Target.Health && Target.Distance(Player) <= 950 ) AllSkills = true;
			
			if (GetBool("UseItems") && AllSkills && GetDistanceSqr(Player,Target) <= 750 * 750 && NotYasuoWall(Target))
			{
				if (Items.CanUseItem(3128)) Items.UseItem(3128,Target);
				if (Items.CanUseItem(3188)) Items.UseItem(3188,Target);
			}
			if (GetBool("UseQ") && Q.IsReady() && GetDistanceSqr(Player,Target) <= Q.Range * Q.Range && NotYasuoWall(Target))
			{
				Q.CastIfHitchanceEquals(Target, HitC,GetBool("UsePacket"));
				if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
					Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
			}
			if (GetBool("UseW")  && W.IsReady() && IsFacing(Player,Target) && Player.Distance(Target) <= 550)
			{
				W.Cast(Target,GetBool("UsePacket"));
			}
			if (GetBool("UseE")  && E.IsReady() && GetDistanceSqr(Player,Target) <= E.Range * E.Range && NotYasuoWall(Target) && !IsInvul(Target))
			{
				if (GetBool("UseQE") )
				{
					if (Target.HasBuff("LuxLightBindingMis"))
					{
						E.Cast(Target ,GetBool("UsePacket") );
						CastE2();
					}
				}
				else
				{
					E.CastIfHitchanceEquals(Target, HitC ,GetBool("UsePacket") );
					CastE2();
				}
				if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
					Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
			}
			if (GetBool("UseR") && R.IsReady() && (R.IsKillable(Target) || AllSkills) && NotYasuoWall(Target) && !IsInvul(Target))
			{
				if (Target.Health <= Damage.GetAutoAttackDamage(Player,Target,true) && Player.Distance(Target) < 550)
					Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
				else
				{
					if (Target.HasBuffOfType(BuffType.Slow) || Target.HasBuffOfType(BuffType.Stun) ||
					    Target.HasBuffOfType(BuffType.Snare) || Target.HasBuffOfType(BuffType.Taunt))
						R.Cast(Target,GetBool("UsePacket"));
					else
						R.CastIfHitchanceEquals(Target,HitChance.VeryHigh ,GetBool("UsePacket"));
				}
			}
			if (GetBool("UseIgnite") && (IgniteKillable(Target) || AllSkills) && CanIgnite())
			{
				if (Player.Distance(Target) <= 600)
					if (Target.Health <= Damage.GetAutoAttackDamage(Player,Target,true) && Player.Distance(Target) < 550)
						Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
				else Player.Spellbook.CastSpell(IgniteSlot, Target);
			}
		}
		
		private static void Harass()
		{
			if (Target == null) return;

			if (GetBool("HQ") && Q.IsReady() && GetDistanceSqr(Player,Target) <= Q.Range * Q.Range && NotYasuoWall(Target))
			{
				Q.CastIfHitchanceEquals(Target, HitC,GetBool("UsePacket"));
				if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
					Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
			}
			if (GetBool("HE") && E.IsReady() && GetDistanceSqr(Player,Target) <= E.Range * E.Range && NotYasuoWall(Target) && !IsInvul(Target))
			{
				if (GetBool("UseQE"))
				{
					if (Target.HasBuff("LuxLightBindingMis"))
					{
						E.CastIfHitchanceEquals(Target, HitC ,GetBool("UsePacket"));
						CastE2();
					}
				}
				else
				{
					E.CastIfHitchanceEquals(Target, HitC ,GetBool("UsePacket"));
					CastE2();
				}
				if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
					Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
			}
		}
		
		private static double CalculateDmg(Obj_AI_Base target)
		{
			double damage = 0;
			if (Q.IsReady() && Q.IsInRange(target.Position,Q.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.Q);
			if (E.IsReady() && E.IsInRange(target.Position,E.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.E);
			if (R.IsReady() && R.IsInRange(target.Position,R.Range))
				damage += Player.GetSpellDamage(target,SpellSlot.R);
			return damage;
		}
		
		private static void JungSteal()
		{
			var Minions = MinionManager.GetMinions(Game.CursorPos, 1000, MinionTypes.All, MinionTeam.Neutral);
			foreach (var minion in Minions.Where(minion => minion.IsVisible && !minion.IsDead ))
			{
				if ((minion.SkinName == "SRU_Blue" || minion.SkinName == "SRU_Red" || minion.SkinName == "SRU_Baron" || minion.SkinName == "SRU_Dragon") &&
				    CalculateDmg(minion) > minion.Health)
				{
					if (Q.IsReady() && GetDistanceSqr(Player,minion) <= Q.Range * Q.Range) Q.Cast(minion,GetBool("UsePacket"));
					if (E.IsReady() && GetDistanceSqr(Player,minion) <= E.Range * E.Range)
					{
						E.Cast(minion,GetBool("UsePacket"));
						while (EObject != null)
						{
							E.Cast(GetBool("UsePacket"));
							break;
						}
					}
					if (R.IsReady() && minion.IsValidTarget(R.Range)) R.Cast(minion,GetBool("UsePacket"));
				}
			}
		}
		
		private static void KillSteal()
		{
			if (GetBool("KUseQ") || GetBool("KUseE") || GetBool("KUseR") || GetBool("KIgnite"))
			{
				foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && hero.IsEnemy && !hero.IsDead))
				{
					if (IsInvul(hero) && GetDistanceSqr(Player,hero) <= Q.Range * Q.Range) Q.CastIfHitchanceEquals(hero, HitChance.High ,GetBool("UsePacket"));
					
					if (NotYasuoWall(hero) && !IsInvul(hero))
					{
						if (GetBool("KUseQ") && Q.IsReady() && GetDistanceSqr(Player,hero) <= Q.Range * Q.Range && Q.IsKillable(hero))
							Q.CastIfHitchanceEquals(hero, HitChance.High,GetBool("UsePacket"));
						else if (GetBool("KUseE") && E.IsReady() && GetDistanceSqr(Player,hero) <= E.Range * E.Range && E.IsKillable(hero) )
						{
							E.CastIfHitchanceEquals(hero, HitChance.High ,GetBool("UsePacket"));
							CastE2();
						}
						else if (GetBool("KUseR") && R.IsReady() && hero.IsValidTarget(R.Range) && R.IsKillable(hero))
						{
							if (hero.Health <= Damage.GetAutoAttackDamage(Player,hero,true) && Player.Distance(hero) < 550)
								Player.IssueOrder(GameObjectOrder.AttackUnit, hero);
							else
							{
								if (Target.HasBuffOfType(BuffType.Slow) || Target.HasBuffOfType(BuffType.Stun) ||
								    Target.HasBuffOfType(BuffType.Snare) || Target.HasBuffOfType(BuffType.Taunt))
									R.Cast(hero,GetBool("UsePacket"));
								else
									R.CastIfHitchanceEquals(hero,HitChance.VeryHigh ,GetBool("UsePacket"));
							}
						}
						else if (GetBool("KIgnite") && IgniteKillable(hero) && CanIgnite())
						{
							if (Player.Distance(hero) <= 600)
							{
								if (hero.Health <= Damage.GetAutoAttackDamage(Player,hero,true) && Player.Distance(hero) < 550)
									Player.IssueOrder(GameObjectOrder.AttackUnit, hero);
								else Player.Spellbook.CastSpell(IgniteSlot, hero);
							}
						}
					}
				}
			}
		}
		
		private static void CastE2()
		{
			if (EObject == null) return;
			foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && enemy.IsEnemy &&
			                                                                     Vector3.Distance(EObject.Position, enemy.Position) <= E.Width+15))
			{
				if (!IsInvul(enemy))
				{
					E.Cast(GetBool("UsePacket"));
					return;
				}
			}
			if (Vector3.Distance(Player.Position, EObject.Position) > 800)	E.Cast(GetBool("UsePacket"));
		}
		
		private static void Farm()
		{
			var Minions = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly);
			if (Minions.Count == 0 ) return;
			if (Player.Mana/Player.MaxMana*100 >= GetSlider("MP"))
			{
				if (GetBool("FQ") && Q.IsReady())
				{
					var castPostion = MinionManager.GetBestLineFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), Q.Width, Q.Range);
					Q.Cast(castPostion.Position, GetBool("UsePacket"));
				}
				if (GetBool("FE") && E.IsReady())
				{
					var castPostion = MinionManager.GetBestCircularFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), E.Width, E.Range);
					E.Cast(castPostion.Position, GetBool("UsePacket"));
					E.Cast(GetBool("UsePacket"));
				}
			}
		}
	}
}