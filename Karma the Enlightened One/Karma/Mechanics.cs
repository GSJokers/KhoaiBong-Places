using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Karma
{
    /// <summary>
    ///     Mechanics Class, contains champion mechanics
    /// </summary>
    internal class Mechanics
    {
        /// <summary>
        ///     Process Karma Combo
        /// </summary>
        public static void ProcessCombo(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget(Instances.Range))
            {
                // If target is invalid for max range, ignore combo.
                return;
            }

            // player local value
            var player = Instances.Player;

            // Using menu & spells a lot, copy to local value.
            var menu = Instances.Menu;
            var spells = Instances.Spells;

            var healthPercent = player.HealthPercentage();
            var manaPercent = player.ManaPercentage();

            var defianceCondition = player.CountAlliesInRange(spells[SpellSlot.E].Range - 200f) >
                                    menu.Item("l33t.karma.combo.minalliesforre").GetValue<Slider>().Value &&
                                    player.CountEnemiesInRange(Instances.Range) >
                                    menu.Item("l33t.karma.combo.minenemiesforre").GetValue<Slider>().Value;

            #region Combo - Q

            // Check if Q is ready
            if (spells[SpellSlot.Q].IsReady() && player.Distance(target.ServerPosition) < spells[SpellSlot.Q].Range &&
                manaPercent >= menu.Item("l33t.karma.combo.minmpforq").GetValue<Slider>().Value)
            {
                // R+Q Missle Prediction
                var prediction = spells[SpellSlot.R].GetPrediction(target);

                #region Combo - R + Q

                var spellvampRequest = healthPercent <=
                                       menu.Item("l33t.karma.combo.minhpforrw").GetValue<Slider>().Value &&
                                       player.Distance(target.ServerPosition) < spells[SpellSlot.W].Range;
                var @continue = (healthPercent > menu.Item("l33t.karma.combo.minhpforrw").GetValue<Slider>().Value) &&
                                !defianceCondition;
                var surival = player.Health > 500;
                var isOverkill = target.Health < Damages.GetDamage(target, SpellSlot.Q, false);

                // Check if R is ready and we have R+Q option enabled in the menu
                if (menu.Item("l33t.karma.combo.rq").GetValue<bool>() && spells[SpellSlot.R].IsReady())
                {
                    // Make sure Q doesn't hit anyone in the way. (target not included)
                    if (prediction.CollisionObjects.Count > 0)
                    {
                        if (!spellvampRequest && surival && @continue && !isOverkill)
                        {
                            if (Damages.GetDamage(target, SpellSlot.Q, true, true) > target.Health)
                            {
                                // Collision Object with Q missle
                                var collision = prediction.CollisionObjects.FirstOrDefault();

                                // Check collision object & is in AoE range with target.
                                if (collision != null &&
                                    (collision.IsValidTarget() &&
                                     target.Distance(collision.Position) < spells[SpellSlot.R].Width))
                                {
                                    // Casting Commands
                                    spells[SpellSlot.R].Cast();
                                    spells[SpellSlot.Q].Cast(prediction.CastPosition);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!spellvampRequest && surival && @continue && !isOverkill)
                        {
                            // Casting Commands
                            spells[SpellSlot.R].Cast();
                            spells[SpellSlot.Q].Cast(prediction.CastPosition);
                        }
                    }
                }

                #endregion

                #region Combo - Solo Q

                // Switch prediction to Q
                prediction = spells[SpellSlot.Q].GetPrediction(target);

                // Check if we have Q option enabled in the menu.
                if (menu.Item("l33t.karma.combo.q").GetValue<bool>())
                {
                    // Make sure Q doesn't hit anyone in the way. (target not included)
                    if (prediction.CollisionObjects.Count == 0)
                    {
                        if (spellvampRequest && player.Health > 500 * player.CountEnemiesInRange(Instances.Range) ||
                            !spellvampRequest)
                        {
                            // Casting Commands
                            spells[SpellSlot.Q].Cast(prediction.CastPosition);
                        }
                    }
                }

                #endregion
            }

            #endregion

            // Update Health & Mana between spells
            healthPercent = player.HealthPercentage();
            manaPercent = player.ManaPercentage();

            #region Combo - W

            // Check if W is ready
            if (spells[SpellSlot.W].IsReady() && player.Distance(target.ServerPosition) < spells[SpellSlot.W].Range)
            {
                // Position prediction of target
                var prediction = Prediction.GetPrediction(target, 0.25f, 80f, target.MoveSpeed).UnitPosition;

                #region Combo - R + W

                // Check if R is ready and we if our HP percent is lower than stated in the menu
                if (spells[SpellSlot.R].IsReady() &&
                    (healthPercent <= menu.Item("l33t.karma.combo.minhpforrw").GetValue<Slider>().Value ||
                     player.Health / 2 < target.Health))
                {
                    if (prediction.Distance(player.ServerPosition) < spells[SpellSlot.W].Range)
                    {
                        // If target is still in range of the W, cast.
                        spells[SpellSlot.R].Cast();
                        spells[SpellSlot.W].Cast(target);
                        return;
                    }
                }

                #endregion

                #region Combo - W

                if (prediction.Distance(player.ServerPosition) < spells[SpellSlot.W].Range &&
                    manaPercent >= menu.Item("l33t.karma.combo.minmpforw").GetValue<Slider>().Value)
                {
                    // Cast W onto target
                    spells[SpellSlot.W].Cast(target);
                }

                #endregion
            }

            #endregion

            // Update Mana between spells
            manaPercent = player.ManaPercentage();

            #region Combo - E

            // Check if E is ready
            if (spells[SpellSlot.E].IsReady() &&
                manaPercent >= menu.Item("l33t.karma.combo.minmpfore").GetValue<Slider>().Value)
            {
                #region Combo - R + E

                // Check if we meet the Defiance Condition
                if (defianceCondition)
                {
                    // Cast Defiance (R+E)
                    spells[SpellSlot.R].Cast();
                    spells[SpellSlot.E].Cast(player);
                    return;
                }

                #endregion

                #region Combo - E

                if (target.MoveSpeed > player.MoveSpeed &&
                    player.Distance(target.ServerPosition) > spells[SpellSlot.Q].Range ||
                    player.Distance(target.ServerPosition) > spells[SpellSlot.W].Range)
                {
                    spells[SpellSlot.E].Cast(player);
                }

                #endregion
            }

            #endregion
        }

        /// <summary>
        ///     Process Karma Harass
        /// </summary>
        public static void ProcessHarass(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget(Instances.Range))
            {
                // If invalid target, ignore.
                return;
            }

            // copy player to local value
            var player = Instances.Player;

            // Using menu & spells a lot, copy to local value.
            var menu = Instances.Menu;
            var spells = Instances.Spells;

            var healthPercent = player.HealthPercentage();
            var manaPercent = player.ManaPercentage();

            #region Harass - Q

            if (spells[SpellSlot.Q].IsReady() &&
                manaPercent >= menu.Item("l33t.karma.harass.minmpq").GetValue<Slider>().Value)
            {
                var pred = spells[SpellSlot.R].GetPrediction(target);

                #region Harass - R + Q

                if (spells[SpellSlot.R].IsReady() && menu.Item("l33t.karma.harass.rq").GetValue<bool>() &&
                    healthPercent > menu.Item("l33t.karma.harass.minhprw").GetValue<Slider>().Value)
                {
                    if (pred.CollisionObjects.Count == 0)
                    {
                        spells[SpellSlot.R].Cast();
                        spells[SpellSlot.Q].Cast(pred.CastPosition);
                        return;
                    }
                }

                #endregion

                #region Harass - Solo Q

                if (menu.Item("l33t.karma.harass.q").GetValue<bool>())
                {
                    pred = spells[SpellSlot.Q].GetPrediction(target);
                    // Make sure Q doesn't hit anyone in the way. (target not included)
                    if (pred.CollisionObjects.Count == 0)
                    {
                        // Casting
                        spells[SpellSlot.Q].Cast(pred.CastPosition);
                    }
                }

                #endregion
            }

            #endregion

            #region Harass - W

            if (spells[SpellSlot.W].IsReady() &&
                manaPercent >= menu.Item("l33t.karma.harass.minmpw").GetValue<Slider>().Value)
            {
                #region Harass - R + W

                if (spells[SpellSlot.R].IsReady() && menu.Item("l33t.karma.harass.rw").GetValue<bool>() &&
                    healthPercent <= menu.Item("l33t.karma.harass.minhprw").GetValue<Slider>().Value)
                {
                    spells[SpellSlot.R].Cast();
                    spells[SpellSlot.W].Cast(target);
                }

                #endregion

                #region Harass - W

                if (menu.Item("l33t.karma.harass.w").GetValue<bool>())
                {
                    spells[SpellSlot.W].Cast(target);
                }

                #endregion
            }

            #endregion

            #region Harass - E

            if (spells[SpellSlot.E].IsReady() && menu.Item("l33t.karma.harass.e").GetValue<bool>())
            {
                if (
                    Prediction.GetPrediction(player, .5f, 50f, player.MoveSpeed)
                        .UnitPosition.Distance(target.ServerPosition) >
                    player.ServerPosition.Distance(target.ServerPosition))
                {
                    spells[SpellSlot.E].Cast(player);
                }
            }

            #endregion
        }

        /// <summary>
        ///     Process Karma Lane Clear
        /// </summary>
        public static void ProcessLaneClear()
        {
            // Get minions list
            var minions = ObjectManager.Get<Obj_AI_Minion>().FindAll(m => m.IsValidTarget(Instances.Range));

            if (minions.Count == 0)
            {
                // If no minions, ignore.
                return;
            }

            // player local value
            var player = Instances.Player;

            // Using menu & spells a lot, copy to local value.
            var menu = Instances.Menu;
            var spells = Instances.Spells;

            var manaPercent = player.ManaPercentage();

            #region Lane Clear - Q

            if (spells[SpellSlot.Q].IsReady())
            {
                #region Lane Clear - R + Q

                // Check if R is ready, we allowed R+Q in menu, we have more or equal mana percent than what we placed in menu and if we have enough minions in lane.
                if (spells[SpellSlot.R].IsReady() && menu.Item("l33t.karma.farming.lcrq").GetValue<bool>() &&
                    manaPercent >= menu.Item("l33t.karma.farming.lcminmpq").GetValue<Slider>().Value &&
                    minions.Count >= menu.Item("l33t.karma.farming.lcminminions").GetValue<Slider>().Value)
                {
                    // Best minion
                    var minion = minions.FirstOrDefault();

                    // How many targets get hit by last minion because of AoE
                    var targeted = 0;

                    // Search for minion
                    foreach (var m in minions)
                    {
                        // Find all minions that are not our minion and check how many minions would it hit.
                        var lTargeted = minions.FindAll(lm => lm != m).Count(om => om.Distance(m) < 250f);

                        // If more minions it would hit than our last minion, continue with prediction.
                        if (lTargeted > targeted)
                        {
                            // Prediction
                            var pred = spells[SpellSlot.R].GetPrediction(m);

                            // Check if Q doesn't hit anything in travel
                            if (pred.CollisionObjects.Count == 0)
                            {
                                // Update minion if passes checks
                                minion = m;
                                targeted = lTargeted;
                            }
                        }
                    }

                    // If minion is valid for our range
                    if (minion != null && (minion.IsValidTarget(spells[SpellSlot.R].Range)))
                    {
                        // Prediction
                        var pred = spells[SpellSlot.R].GetPrediction(minion);

                        // Casting
                        spells[SpellSlot.R].Cast();
                        spells[SpellSlot.Q].Cast(pred.CastPosition);
                        return;
                    }
                }

                #endregion

                #region Lane Clear - Solo Q

                // Check if we allowed Q in menu and we have enough mana by what we placed inside the menu
                if (menu.Item("l33t.karma.farming.lcq").GetValue<bool>() &&
                    manaPercent >= menu.Item("l33t.karma.farming.lcminmpq").GetValue<Slider>().Value)
                {
                    // Best minion
                    Obj_AI_Minion minion = null;

                    // Search for minion
                    foreach (var m in
                        from m in minions
                        let pred = spells[SpellSlot.Q].GetPrediction(m)
                        where pred.CollisionObjects.Count == 0
                        select m)
                    {
                        if (minion == null)
                        {
                            var healthPred = HealthPrediction.GetHealthPrediction(
                                m, (int) ((player.AttackDelay * 1000) * 3));
                            if (healthPred < Damages.GetDamage(m, SpellSlot.Q, false) ||
                                healthPred > Damages.GetDamage(m, SpellSlot.Q, false) + player.GetAutoAttackDamage(m))
                            {
                                minion = m;
                            }
                        }
                        else if (minion.Health > m.Health)
                        {
                            minion = m;
                        }
                    }

                    // If minion is valid for our range
                    if (minion != null && (minion.IsValidTarget(spells[SpellSlot.Q].Range)) &&
                        minion.Distance(player.ServerPosition) > player.AttackRange)
                    {
                        // Prediction
                        var pred = spells[SpellSlot.Q].GetPrediction(minion);

                        // Cast
                        spells[SpellSlot.Q].Cast(pred.CastPosition);
                    }
                }

                #endregion
            }

            #endregion
        }

        /// <summary>
        ///     Process Karma Last Hit
        /// </summary>
        public static void ProcessLastHit()
        {
            // Minion list
            var minions = ObjectManager.Get<Obj_AI_Minion>().FindAll(m => m.IsValidTarget(Instances.Range));
            minions.RemoveAll(m => m.Distance(Instances.Player.ServerPosition) < Instances.Player.AttackRange);

            if (minions.Count == 0)
            {
                // If no minions, ignore.
                return;
            }

            // player local value
            var player = Instances.Player;

            // Using menu & spells a lot, copy to local value.
            var menu = Instances.Menu;
            var spells = Instances.Spells;

            var manaPercent = player.ManaPercentage();

            #region Last Hit - Q

            // If Q is ready and we enabled it inside the menu
            if (spells[SpellSlot.Q].IsReady() && menu.Item("l33t.karma.farming.lhq").GetValue<bool>() &&
                manaPercent >= menu.Item("l33t.karma.farming.lhminmpq").GetValue<Slider>().Value)
            {
                // Main minion
                Obj_AI_Minion minion = null;

                // Search for best minion
                foreach (var m in
                    from m in minions
                    let pred = spells[SpellSlot.Q].GetPrediction(m)
                    where pred.CollisionObjects.Count == 0
                    select m)
                {
                    if (minion == null &&
                        Damages.GetDamage(m, SpellSlot.Q, false) >
                        HealthPrediction.GetHealthPrediction(m, (int) ((player.AttackDelay * 1000) * 2)) &&
                        HealthPrediction.GetHealthPrediction(m, (int) ((player.AttackDelay * 1000) * 2)) >
                        player.GetAutoAttackDamage(m))
                    {
                        minion = m;
                    }
                    else if ((minion != null) && minion.Health > m.Health)
                    {
                        minion = m;
                    }
                }

                // Check if minion is valid for range.
                if (minion != null && (minion.IsValidTarget(spells[SpellSlot.Q].Range)) &&
                    player.Distance(minion.ServerPosition) > player.AttackRange)
                {
                    // Cast Q onto minion with prediction.
                    var pred = spells[SpellSlot.Q].GetPrediction(minion);
                    spells[SpellSlot.Q].Cast(pred.CastPosition);
                }
            }

            #endregion
        }

        /// <summary>
        ///     Process Karma Killsteal
        /// </summary>
        public static void ProcessKillsteal(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget(Instances.Range))
            {
                // If invalid target, ignore.
                return;
            }

            // Check if Q is ready
            if (Instances.Spells[SpellSlot.Q].IsReady() &&
                target.Distance(Instances.Player.ServerPosition) < Instances.Spells[SpellSlot.Q].Range)
            {
                // Get Q Prediction
                var prediction = Instances.Spells[SpellSlot.Q].GetPrediction(target);

                // Check that we won't hit anything in the way
                if (prediction.CollisionObjects.Count == 0)
                {
                    // Check if R is ready and we set R+Q to use in the menu and if our mana percent is more than the minimum.
                    if (Instances.Spells[SpellSlot.R].IsReady() &&
                        Instances.Menu.Item("l33t.karma.killsteal.rq").GetValue<bool>())
                    {
                        // If the damage will kill the target (R+Q)
                        if (Damages.GetDamage(target, SpellSlot.Q, true) > target.Health)
                        {
                            // Cast R+Q
                            Instances.Spells[SpellSlot.R].Cast();
                            Instances.Spells[SpellSlot.Q].Cast(prediction.CastPosition);
                            return;
                        }
                    }

                    // If the damage will kill the target (Q)
                    if (Damages.GetDamage(target, SpellSlot.Q, false) > target.Health)
                    {
                        // Cast Q
                        Instances.Spells[SpellSlot.Q].Cast(prediction.CastPosition);
                    }
                }
                else
                {
                    // If we hit something, check if R is ready, mana percent is more than minimum and we want AoE(explosion) damage
                    if (Instances.Spells[SpellSlot.R].IsReady() &&
                        Instances.Menu.Item("l33t.karma.killsteal.rq").GetValue<bool>() &&
                        Instances.Menu.Item("l33t.karma.killsteal.useaoe").GetValue<bool>())
                    {
                        // Get our collided object
                        var collision = prediction.CollisionObjects.FirstOrDefault();

                        // Check if the collided object is valid & check if the target is inside the blast radius
                        if (collision != null &&
                            (collision.IsValidTarget() &&
                             collision.ServerPosition.Distance(target.ServerPosition) < 250f))
                        {
                            // Check if explosion would kill the target
                            if (Damages.GetDamage(target, SpellSlot.Q, true, true) > target.Health)
                            {
                                // Cast R+Q
                                Instances.Spells[SpellSlot.R].Cast();
                                Instances.Spells[SpellSlot.Q].Cast(prediction.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Process Karma Flee
        /// </summary>
        public static void ProcessFlee()
        {
            var spells = Instances.Spells;
            var player = Instances.Player;

            // Check if E is ready
            if (Instances.Spells[SpellSlot.E].IsReady())
            {
                #region Flee - R + E

                // Check if R is ready and enough allies in range as stated in menu
                if (Instances.Spells[SpellSlot.R].IsReady() &&
                    Instances.Player.CountAlliesInRange(Instances.Spells[SpellSlot.E].Range) >=
                    Instances.Menu.Item("l33t.karma.flee.minalliesforre").GetValue<Slider>().Value)
                {
                    // Casting
                    Instances.Spells[SpellSlot.R].Cast();
                    Instances.Spells[SpellSlot.E].Cast(Instances.Player);
                    return;
                }

                #endregion

                #region Flee - E

                // Cast
                Instances.Spells[SpellSlot.E].Cast(Instances.Player);

                #endregion
            }

            // Check if W is ready
            if (Instances.Spells[SpellSlot.W].IsReady())
            {
                var target = Instances.Target;
                var healthPercent = player.HealthPercentage();
                if (target.IsValidTarget(Instances.Range))
                {
                    #region Flee - W

                    // Check if W is ready
                    if (spells[SpellSlot.W].IsReady() &&
                        player.Distance(target.ServerPosition) < spells[SpellSlot.W].Range)
                    {
                        // Position prediction of target
                        var prediction = Prediction.GetPrediction(target, 0.25f, 80f, target.MoveSpeed).UnitPosition;

                        #region Flee - R + W

                        // Check if R is ready and we if our HP percent is lower than stated in the menu
                        if (spells[SpellSlot.R].IsReady() && healthPercent <= 50f)
                        {
                            if (prediction.Distance(player.ServerPosition) < spells[SpellSlot.W].Range)
                            {
                                // If target is still in range of the W, cast.
                                spells[SpellSlot.R].Cast();
                                spells[SpellSlot.W].Cast(target);
                                return;
                            }
                        }

                        #endregion

                        #region Flee - W

                        if (prediction.Distance(player.ServerPosition) < spells[SpellSlot.W].Range)
                        {
                            // Cast W onto target
                            spells[SpellSlot.W].Cast(target);
                        }

                        #endregion
                    }

                    #endregion
                }
            }


            MoveTo(Game.CursorPos, Instances.Player.BoundingRadius);
        }

        #region MoveTo

        public static bool Attack = true;
        public static bool Move = true;
        public static int LastMoveCommandT;
        public static Vector3 LastMoveCommandPosition = Vector3.Zero;
        private const int Delay = 80;
        private const float MinDistance = 400;
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static void MoveTo(Vector3 position,
            float holdAreaRadius = 0,
            bool overrideTimer = false,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            if (Environment.TickCount - LastMoveCommandT < Delay && !overrideTimer)
            {
                return;
            }

            LastMoveCommandT = Environment.TickCount;

            if (Instances.Player.ServerPosition.Distance(position, true) < holdAreaRadius * holdAreaRadius)
            {
                if (Instances.Player.Path.Count() > 1)
                {
                    Instances.Player.IssueOrder(GameObjectOrder.HoldPosition, Instances.Player.ServerPosition);
                    LastMoveCommandPosition = Instances.Player.ServerPosition;
                }
                return;
            }

            var point = position;
            if (useFixedDistance)
            {
                point = Instances.Player.ServerPosition +
                        (randomizeMinDistance ? (Random.NextFloat(0.6f, 1) + 0.2f) * MinDistance : MinDistance) *
                        (position.To2D() - Instances.Player.ServerPosition.To2D()).Normalized().To3D();
            }
            else
            {
                if (randomizeMinDistance)
                {
                    point = Instances.Player.ServerPosition +
                            (Random.NextFloat(0.6f, 1) + 0.2f) * MinDistance *
                            (position.To2D() - Instances.Player.ServerPosition.To2D()).Normalized().To3D();
                }
                else if (Instances.Player.ServerPosition.Distance(position) > MinDistance)
                {
                    point = Instances.Player.ServerPosition +
                            MinDistance * (position.To2D() - Instances.Player.ServerPosition.To2D()).Normalized().To3D();
                }
            }

            Instances.Player.IssueOrder(GameObjectOrder.MoveTo, point);
            LastMoveCommandPosition = point;
        }

        #endregion
    }
}