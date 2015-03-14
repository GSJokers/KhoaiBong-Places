#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace LightningRyze
{
	internal class Program
	{
		private static Menu Config;
		private static Orbwalking.Orbwalker Orbwalker;
		private static Obj_AI_Hero Target;
		private static Obj_AI_Hero Player;
		private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		private static string LastCast;
		private static float LastFlashTime;
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
			
			if (Player.ChampionName != "Ryze") return;
			
			Q = new Spell(SpellSlot.Q, 625);
			W = new Spell(SpellSlot.W, 600);
			E = new Spell(SpellSlot.E, 600);
			R = new Spell(SpellSlot.R);
			
			IgniteSlot = Player.GetSpellSlot("SummonerDot");
			
			Config = new Menu("Lightning Ryze", "Lightning Ryze", true);
			
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			TargetSelector.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
			Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
			Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("TypeCombo", "").SetValue(new StringList(new[] {"Mixed mode","Burst combo","Long combo"},0)));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseAA", "Auto Attack").SetValue(true));
			
			Config.AddSubMenu(new Menu("Harass", "Harass"));
			Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HW", "Use W").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("AutoPoke", "Auto Harass Q").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
			Config.SubMenu("Harass").AddItem(new MenuItem("ManaH", "Auto Harass if % MP >").SetValue(new Slider(30, 1, 100)));
			
			Config.AddSubMenu(new Menu("Farm", "Farm"));
			Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FR", "Use R").SetValue(true));
			
			Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KQ", "Use Q").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KW", "Use W").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KE", "Use E").SetValue(true));
			
			Config.AddSubMenu(new Menu("Extra", "Extra"));
			Config.SubMenu("Extra").AddItem(new MenuItem("tearStack", "Q+W double tear effect").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Toggle)));
			Config.SubMenu("Extra").AddItem(new MenuItem("UseSeraphs", "Use Seraphs Embrace").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("HP", "SE when % HP <=").SetValue(new Slider(20, 100, 0)));
			Config.SubMenu("Extra").AddItem(new MenuItem("YasuoWall", "Don't use skillshots on Yasuo's Wall").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("WInterrupt", "Interrupt spells W").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("WGap", "W on GapCloser").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("UsePacket", "Packet Cast").SetValue(true));
			
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WERange", "W+E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.AddToMainMenu();
			
			Game.PrintChat("Lightning Ryze loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
			Drawing.OnDraw += Drawing_OnDraw;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
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
			
			Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
			
			if (GetActive("ComboActive"))
			{
				if (!GetBool("UseAA")) Orbwalker.SetAttack(false);
				else Orbwalker.SetAttack(true);
				if (GetSelected("TypeCombo") == 0) ComboMixed();
				else if (GetSelected("TypeCombo") == 1) ComboBurst();
				else if (GetSelected("TypeCombo") == 2) ComboLong();
			}
			else if (GetActive("HarassActive")) Harass();
			
//			if (GetActive("tearStack")) TearExploit();
			if (GetActive("LaneClearActive") || GetActive("FreezeActive")) Farm();
			
			if (GetActive("AutoPoke")) AutoPoke(Target);
		}
		
		private static bool IsOnTheLine(Vector3 point, Vector3 start, Vector3 end)
		{
			var obj = Geometry.ProjectOn(point.To2D(),start.To2D(),end.To2D());
			if (obj.IsOnSegment) return true;
			return false;
		}
		
		private static void OnCreateObject(GameObject obj, EventArgs args)
		{
			if (YasuoWall == null && obj != null && obj.IsValid && Player.Distance(obj.Position) < 1500 &&
			    System.Text.RegularExpressions.Regex.IsMatch(obj.Name, "_w_windwall.\\.troy",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				YasuoWall = obj;
		}
		
		private static void OnDeleteObject(GameObject obj, EventArgs args)
		{
			if (YasuoWall != null && obj != null && obj.IsValid && Player.Distance(obj.Position) < 1500 &&
			    System.Text.RegularExpressions.Regex.IsMatch(obj.Name, "_w_windwall.\\.troy",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				YasuoWall = null;
		}
		
		private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsMe)
			{
				if (args.SData.Name.ToLower() == "overload") LastCast = "Q";
				else if (args.SData.Name.ToLower() == "runeprison") LastCast = "W";
				else if (args.SData.Name.ToLower() == "spellflux") LastCast = "E";
				else if (args.SData.Name.ToLower() == "desperatepower") LastCast = "R";
				else if (args.SData.Name.ToLower() == "summonerflash") LastFlashTime = Game.Time;
				if (GetActive("tearStack"))
				{
					var spellSlot = Player.GetSpellSlot(args.SData.Name);
					var target = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(args.Target.NetworkId);
					var distance = Player.ServerPosition.Distance(target.ServerPosition);
					var delay = 1000 * (distance / args.SData.MissileSpeed);
					delay -= Game.Ping / 2;
					if (spellSlot == SpellSlot.Q && W.IsReady() && target.IsMinion && target.Health < Q.GetDamage(target))
						Utility.DelayAction.Add((int)delay, () => W.CastOnUnit(target, true));
				}
			}
			if (GetBool("UseSeraphs") && sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret))
			{
				if ( (args.SData.Name != null && IsOnTheLine(Player.Position,args.Start,args.End)) || (args.Target == Player && GetDistance(sender) <= 700))
				{
					if (Player.Health/Player.MaxHealth*100 <= GetSlider("HP") && Items.HasItem(3040) && Items.CanUseItem(3040)) Items.UseItem(3040);
				}
			}
			if (sender.IsValid && sender.IsEnemy && args.SData.Name == "YasuoWMovingWall")
			{
				WallCastT = Environment.TickCount;
				YasuoWallCastedPos = sender.ServerPosition.To2D();
			}
		}
		
		private static void Drawing_OnDraw(EventArgs args)
		{
			if (GetCircle("QRange").Active && !Player.IsDead) Render.Circle.DrawCircle(Player.Position, Q.Range, GetCircle("QRange").Color);
			if (GetCircle("WERange").Active && !Player.IsDead) Render.Circle.DrawCircle(Player.Position, W.Range, GetCircle("WERange").Color);
		}
		
		private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
		{
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
				args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || GetDistance(args.Target) >= 600);
		}
		
		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
		{
			if (Player.HasBuff("Recall") || Player.IsWindingUp) return;
			if (GetBool("WGap") && W.IsReady() && GetDistance(gapcloser.Sender) <= W.Range && gapcloser.Sender.IsTargetable)
				W.CastOnUnit(gapcloser.Sender,GetBool("UsePacket"));
		}
		
		private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
		{
			if (GetBool("WInterrupt") && W.IsReady() && GetDistance(unit) <= W.Range && unit.IsTargetable) W.CastOnUnit(unit,GetBool("UsePacket"));
		}
		
		private static bool detectCollision(Obj_AI_Hero target)
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
		
		private static float GetDistance(AttackableUnit target)
		{
			return Vector3.Distance(Player.Position, target.Position);
		}
		
		private static bool IsInvul(Obj_AI_Hero target)
		{
			if (target.HasBuff("JudicatorIntervention") || target.HasBuff("Undying Rage"))
				return true;
			return false;
		}
		
		private static bool CanIgnite()
		{
			return (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready);
		}
		
		private static double GetComboDamage(Obj_AI_Base target)
		{
			double dmg = 0;
			if (Q.IsReady() && GetDistance(target) <= Q.Range)
				dmg += Player.GetSpellDamage(target, SpellSlot.Q)*2;
			if (W.IsReady() && GetDistance(target) <= W.Range)
				dmg += Player.GetSpellDamage(target, SpellSlot.W);
			if (E.IsReady() && GetDistance(target) <= E.Range)
				dmg += Player.GetSpellDamage(target, SpellSlot.E);
			if (CanIgnite() && GetDistance(target) <= 600)
				dmg += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
			return dmg;
		}
		
		private static void ComboMixed()
		{
			if (Target == null || !detectCollision(Target)) return;
			if (IsInvul(Target))
			{
				W.CastOnUnit(Target,GetBool("UsePacket"));
				return;
			}
			if (GetBool("UseIgnite") && CanIgnite() && GetDistance(Target) <= 600 && GetComboDamage(Target) >= (double)Target.Health)
				Player.Spellbook.CastSpell(IgniteSlot, Target);
			if (Game.Time - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
			else
			{
				if (Q.IsKillable(Target) && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
				else if (E.IsKillable(Target) && E.IsReady()) E.CastOnUnit(Target,GetBool("UsePacket"));
				else if (W.IsKillable(Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else if (GetDistance(Target) >= 575 && !IsBothFacing(Player,Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else
				{
					if (Q.IsReady() && W.IsReady() && E.IsReady() && GetComboDamage(Target) >= (double)Target.Health)
					{
						if (Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
						else if (R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
						else if (W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
						else if (E.IsReady()) E.CastOnUnit(Target,GetBool("UsePacket"));
					}
					else if (Math.Abs(Player.PercentCooldownMod) >= 0.2)
					{
						if (Utility.CountEnemiesInRange(Target,300) > 1)
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
								if (R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
								if (!R.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
								if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
							}
							else Q.CastOnUnit(Target,GetBool("UsePacket"));
						}
						else
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
								if (W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
								if (!W.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
								if (!W.IsReady() && !E.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
							}
							else
								if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
						}
					}
					else
					{
						if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
						else if (R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
						else if (E.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
						else if (W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
					}
				}
			}
		}
		
		private static void ComboBurst()
		{
			if (Target == null || !detectCollision(Target)) return;
			if (IsInvul(Target))
			{
				W.CastOnUnit(Target,GetBool("UsePacket"));
				return;
			}
			if (GetBool("UseIgnite") && CanIgnite() && GetDistance(Target) <= 600 && GetComboDamage(Target) >= (double)Target.Health)
				Player.Spellbook.CastSpell(IgniteSlot, Target);
			if (Game.Time - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
			else
			{
				if (Q.IsKillable(Target) && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
				else if (E.IsKillable(Target) && E.IsReady()) E.CastOnUnit(Target,GetBool("UsePacket"));
				else if (W.IsKillable(Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else if (GetDistance(Target) >= 575 && !IsBothFacing(Player,Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else
				{
					if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
					else if (R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
					else if (E.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
					else if (W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
				}
			}
		}
		
		private static void ComboLong()
		{
			if (Target == null || !detectCollision(Target)) return;
			if (IsInvul(Target))
			{
				W.CastOnUnit(Target,GetBool("UsePacket"));
				return;
			}
			if (GetBool("UseIgnite") && CanIgnite() && GetDistance(Target) <= 600 && GetComboDamage(Target) >= (double)Target.Health)
				Player.Spellbook.CastSpell(IgniteSlot, Target);
			if (Game.Time - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
			else
			{
				if (Q.IsKillable(Target) && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
				else if (E.IsKillable(Target) && E.IsReady()) E.CastOnUnit(Target,GetBool("UsePacket"));
				else if (W.IsKillable(Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else if (GetDistance(Target) >= 575 && !IsBothFacing(Player,Target) && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				else
				{
					if (Utility.CountEnemiesInRange(Target,300) > 1)
					{
						if (LastCast == "Q")
						{
							if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
							if (R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
							if (!R.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
							if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
						}
						else Q.CastOnUnit(Target,GetBool("UsePacket"));
					}
					else
					{
						if (LastCast == "Q")
						{
							if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
							if (W.IsReady()) W.CastOnUnit(Target ,GetBool("UsePacket"));
							if (!W.IsReady()) E.CastOnUnit(Target ,GetBool("UsePacket"));
							if (!W.IsReady() && !E.IsReady() && R.IsReady() && GetBool("UseR")) R.Cast(GetBool("UsePacket"));
						}
						else
							if (Q.IsReady()) Q.CastOnUnit(Target ,GetBool("UsePacket"));
					}
				}
			}
		}
		
		private static void Harass()
		{
			if (Target == null || !detectCollision(Target)) return;
			if (GetDistance(Target) <= 625 )
			{
				if (GetBool("HQ") && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
				if (GetBool("HW") && W.IsReady()) W.CastOnUnit(Target,GetBool("UsePacket"));
				if (GetBool("HQ") && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
				if (GetBool("HE") && E.IsReady()) E.CastOnUnit(Target,GetBool("UsePacket"));
				if (GetBool("HQ") && Q.IsReady()) Q.CastOnUnit(Target,GetBool("UsePacket"));
			}
		}
		
		private static void Farm()
		{
			var allMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All ,MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
			if (allMinions.Count < 1) return;
			if (GetActive("FreezeActive"))
			{
				foreach (var minion in allMinions)
				{
					if (GetBool("FQ") && Q.IsReady() && Player.GetSpellDamage(minion, SpellSlot.Q) > minion.Health)
						Q.CastOnUnit(minion,GetBool("UsePacket"));
					else if (GetBool("FW") && W.IsReady() && Player.GetSpellDamage(minion, SpellSlot.W) > minion.Health)
						W.CastOnUnit(minion,GetBool("UsePacket"));
					else if (GetBool("FE") && E.IsReady() && Player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
						E.CastOnUnit(minion,GetBool("UsePacket"));
				}
			}
			else if (GetActive("LaneClearActive"))
			{
				foreach (var minion in allMinions)
				{
					if ((allMinions.Count >= 5 || allMinions.Count >= 2 && Player.Health/Player.MaxHealth*100 <= 5) && GetBool("FR") && R.IsReady()) R.Cast(GetBool("UsePacket"));
					if (GetBool("FQ") && Q.IsReady()) Q.CastOnUnit(minion,GetBool("UsePacket"));
					if (GetBool("FW") && W.IsReady()) W.CastOnUnit(minion,GetBool("UsePacket"));
					if (GetBool("FQ") && Q.IsReady()) Q.CastOnUnit(minion,GetBool("UsePacket"));
					if (GetBool("FE") && E.IsReady()) E.CastOnUnit(minion,GetBool("UsePacket"));
					if (GetBool("FQ") && Q.IsReady()) Q.CastOnUnit(minion,GetBool("UsePacket"));
				}
			}
		}
		
		private static void AutoPoke(Obj_AI_Hero enemy)
		{
			if (enemy == null) return;
			if (Q.IsReady() && GetDistance(enemy) <= Q.Range && enemy.IsTargetable && (Player.Mana/Player.MaxMana)*100 > GetSlider("ManaH") && detectCollision(Target))
				Q.CastOnUnit(enemy, GetBool("UsePacket"));
		}
		
		private static void KillSteal()
		{
			if (!GetBool("KQ") && !GetBool("KW") && !GetBool("KE")) return;
			foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => GetDistance(enemy) <= Q.Range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead && enemy.IsTargetable))
			{
				if (enemy == null || !detectCollision(enemy)) return;
				if (IsInvul(enemy))
				{
					W.CastOnUnit(enemy,GetBool("UsePacket"));
					break;
				}
				if (GetBool("KQ") && Q.IsReady() && Player.GetSpellDamage(enemy, SpellSlot.Q) > enemy.Health) Q.CastOnUnit(enemy,GetBool("UsePacket"));
				else if (GetBool("KW") && W.IsReady() && Player.GetSpellDamage(enemy, SpellSlot.W) > enemy.Health) W.CastOnUnit(enemy,GetBool("UsePacket"));
				else if (GetBool("KE") && E.IsReady() && Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health) E.CastOnUnit(enemy,GetBool("UsePacket"));
				else if (GetBool("KQ") && GetBool("KW") && Q.IsReady() && W.IsReady() &&
				         Player.GetSpellDamage(enemy, SpellSlot.Q)+Player.GetSpellDamage(enemy, SpellSlot.W) > enemy.Health)
				{
					W.CastOnUnit(enemy,GetBool("UsePacket"));
					Q.CastOnUnit(enemy,GetBool("UsePacket"));
				}
				else if (GetBool("KQ") && GetBool("KE") && Q.IsReady() && W.IsReady() &&
				         Player.GetSpellDamage(enemy, SpellSlot.Q)+Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health)
				{
					E.CastOnUnit(enemy,GetBool("UsePacket"));
					Q.CastOnUnit(enemy,GetBool("UsePacket"));
				}
				else if (GetBool("KW") && GetBool("KE") && Q.IsReady() && W.IsReady() &&
				         Player.GetSpellDamage(enemy, SpellSlot.W)+Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health)
				{
					E.CastOnUnit(enemy,GetBool("UsePacket"));
					W.CastOnUnit(enemy,GetBool("UsePacket"));
				}
				else if (GetBool("KQ") && GetBool("KW") && GetBool("KE") && Q.IsReady() && W.IsReady() && E.IsReady() &&
				         Player.GetSpellDamage(enemy, SpellSlot.Q)+Player.GetSpellDamage(enemy, SpellSlot.W)+Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health)
				{
					E.CastOnUnit(enemy,GetBool("UsePacket"));
					W.CastOnUnit(enemy,GetBool("UsePacket"));
					Q.CastOnUnit(enemy,GetBool("UsePacket"));
				}
			}
		}
		private static bool IsFacing(Obj_AI_Base source, Obj_AI_Base target)
		{
			const float angle = 90;
			return source.Direction.To2D().AngleBetween((target.Position - source.Position).To2D()) < angle;
		}
		private static bool IsBothFacing(Obj_AI_Base source, Obj_AI_Base target)
		{
			return IsFacing(source,target) && IsFacing(target,source);
		}
	}
}