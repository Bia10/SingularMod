﻿#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author$
// $Date$
// $HeadURL$
// $LastChangedBy$
// $LastChangedDate$
// $LastChangedRevision$
// $Revision$

#endregion

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Styx;
using Styx.Helpers;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals.World;
using Action = Styx.TreeSharp.Action;
using Singular.Settings;
using Singular.Lists;

namespace Singular.Helpers
{
    public delegate WoWUnit UnitSelectionDelegate(object context);

    public delegate bool SimpleBooleanDelegate(object context);

    internal static class Spell
    {
        public static bool IsChanneling { get { return StyxWoW.Me.ChanneledCastingSpellId != 0 && StyxWoW.Me.IsChanneling; } }

        public static WoWDynamicObject GetGroundEffectBySpellId(int spellId)
        {
            return ObjectManager.GetObjectsOfType<WoWDynamicObject>().FirstOrDefault(o => o.SpellId == spellId);
        }

        public static bool IsStandingInGroundEffect(bool harmful=true)
        {
            foreach(var obj in ObjectManager.GetObjectsOfType<WoWDynamicObject>())
            {
                if (obj.Distance <= obj.Radius)
                {
                    // We're standing in this.
                    if (obj.Caster.IsFriendly && !harmful)
                        return true;
                    if (obj.Caster.IsHostile && harmful)
                        return true;
                }
            }
            return false;
        }

        public static float MeleeDistance(this WoWUnit mob)
        {
            if (mob == null)
                return 0;

            if (mob.IsPlayer)
                return 3.5f;

            return Math.Max(5f, StyxWoW.Me.CombatReach + 1.3333334f + mob.CombatReach);
        }

        public static float MeleeRange
        {
            get
            {
                return StyxWoW.Me.CurrentTarget.MeleeDistance();
            }
        }

        public static float SafeMeleeRange { get { return Math.Max(MeleeRange - 1f, 5f); } }

        /// <summary>
        /// gets the current Cooldown remaining for the spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns>TimeSpan representing cooldown remaining, TimeSpan.MaxValue if spell unknown</returns>
        public static TimeSpan GetSpellCooldown(string spell)
        {
            SpellFindResults sfr;
            if ( SpellManager.FindSpell(spell, out sfr))
                return (sfr.Override ?? sfr.Original).CooldownTimeLeft;

            return TimeSpan.MaxValue;
        }

        /// <summary>
        ///  Returns maximum spell range based on hitbox of unit. 
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="unit"></param>
        /// <returns>Maximum spell range</returns>
        public static float ActualMaxRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.MaxRange == 0)
                return 0;
            // 0.3 margin for error
            return unit != null ? spell.MaxRange + unit.CombatReach + 1f : spell.MaxRange;
        }

        /// <summary>
        /// Returns minimum spell range based on hitbox of unit. 
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="unit"></param>
        /// <returns>Minimum spell range</returns>

        public static float ActualMinRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.MinRange == 0)
                return 0;
            // 0.3 margin for error
            return unit != null ? spell.MinRange + unit.CombatReach + 1.6666667f : spell.MinRange;
        }

        public static double TimeToEnergyCap()
        {

            double timetoEnergyCap;
            double playerEnergy;
            double ER_Rate;

            playerEnergy = Lua.GetReturnVal<int>("return UnitMana(\"player\");", 0); // current Energy 
            ER_Rate = EnergyRegen();
            timetoEnergyCap = (100 - playerEnergy) * (1.0 / ER_Rate); // math 

            return timetoEnergyCap;
        }

        public static double EnergyRegen()
        {
            double energyRegen;
            energyRegen = Lua.GetReturnVal<float>("return GetPowerRegen()", 1); // rate of energy regen
            return energyRegen;
        }

        #region Properties

        internal static string LastSpellCast { get; set; }

        #endregion

        #region Wait

        public static bool IsGlobalCooldown(bool faceDuring = false, bool allowLagTollerance = true)
        {
            uint latency = allowLagTollerance ? StyxWoW.WoWClient.Latency : 0;
            TimeSpan gcdTimeLeft = SpellManager.GlobalCooldownLeft;
            return gcdTimeLeft.TotalMilliseconds > latency;
        }

        /// <summary>
        ///   Creates a composite that will return a success, so long as you are currently casting. (Use this to prevent the CC from
        ///   going down to lower branches in the tree, while casting.)
        /// </summary>
        /// <remarks>
        ///   Created 13/5/2011.
        /// </remarks>
        /// <param name = "faceDuring">Whether or not to face during casting</param>
        /// <param name = "allowLagTollerance">Whether or not to allow lag tollerance for spell queueing</param>
        /// <returns></returns>
        public static Composite WaitForCast(bool faceDuring = false, bool allowLagTollerance = true)
        {
            return new Action(ret =>
                {
                    if (!StyxWoW.Me.IsCasting)
                        return RunStatus.Failure;

                    if (StyxWoW.Me.IsWanding())
                        return RunStatus.Failure;

                    if (StyxWoW.Me.ChannelObjectGuid > 0)
                        return RunStatus.Failure;

                    uint latency = StyxWoW.WoWClient.Latency * 2;
                    TimeSpan castTimeLeft = StyxWoW.Me.CurrentCastTimeLeft;
                    if (allowLagTollerance && castTimeLeft != TimeSpan.Zero &&
                        StyxWoW.Me.CurrentCastTimeLeft.TotalMilliseconds < latency)
                        return RunStatus.Failure;

                    if (faceDuring && StyxWoW.Me.ChanneledSpell == null ) // .ChanneledCastingSpellId == 0)
                        Movement.CreateFaceTargetBehavior();

                    // return RunStatus.Running;
                    return RunStatus.Success;
                });
        }

        #endregion

        /*
        #region PreventDoubleCast

        /// <summary>
        /// Creates a composite to avoid double casting spells on current target. Mostly usable for spells like Immolate, Devouring Plague etc.
        /// </summary>
        /// <remarks>
        /// Created 19/12/2011 raphus
        /// </remarks>
        /// <param name="spellNames"> Spell names to check </param>
        /// <returns></returns>
        public static Composite PreventDoubleCast(params string[] spellNames)
        {
            return PreventDoubleCast(ret => StyxWoW.Me.CurrentTarget, spellNames);
        }

        /// <summary>
        /// Creates a composite to avoid double casting spells on specified unit. Mostly usable for spells like Immolate, Devouring Plague etc.
        /// </summary>
        /// <remarks>
        /// Created 19/12/2011 raphus
        /// </remarks>
        /// <param name="unit"> Unit to check </param>
        /// <param name="spellNames"> Spell names to check </param>
        /// <returns></returns>
        public static Composite PreventDoubleCast(UnitSelectionDelegate unit, params string[] spellNames)
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret =>
                        StyxWoW.Me.IsCasting && spellNames.Contains(StyxWoW.Me.CastingSpell.Name) && unit != null &&
                        unit(ret) != null &&
                        unit(ret).Auras.Any(
                            a => a.Value.SpellId == StyxWoW.Me.CastingSpellId && a.Value.CreatorGuid == StyxWoW.Me.Guid),
                        new Action(ret => SpellManager.StopCasting())));
        }

        #endregion
        */

        #region Double Cast Shit

        // PRSettings.Instance.ThrottleTime is used throughout the rotationbases to enable a setable expiryTime for the methods below.

        static readonly Dictionary<string, DoubleCastSpell> DoubleCastEntries = new Dictionary<string, DoubleCastSpell>();

        private static void UpdateDoubleCastEntries(string spellName, double expiryTime)
        {
            if (DoubleCastEntries.ContainsKey(spellName)) DoubleCastEntries[spellName] = new DoubleCastSpell(spellName, expiryTime, DateTime.UtcNow);
            if (!DoubleCastEntries.ContainsKey(spellName)) DoubleCastEntries.Add(spellName, new DoubleCastSpell(spellName, expiryTime, DateTime.UtcNow));
        }

        public static void OutputDoubleCastEntries()
        {
            foreach (var spell in DoubleCastEntries)
            {
                Logger.Write(spell.Key + " time: " + spell.Value.DoubleCastCurrentTime);
            }
        }

        internal static void PulseDoubleCastEntries()
        {
            DoubleCastEntries.RemoveAll(t => DateTime.UtcNow.Subtract(t.DoubleCastCurrentTime).TotalSeconds >= t.DoubleCastExpiryTime);
        }

        public static Composite PreventDoubleCast(string spell, double expiryTime, Selection<bool> reqs = null)
        {
            return PreventDoubleCast(spell, expiryTime, ret => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite PreventDoubleCast(int spell, double expiryTime, Selection<bool> reqs = null)
        {
            return PreventDoubleCast(spell, expiryTime, target => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite PreventDoubleCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return PreventDoubleCast(spell, expiryTime, ret => StyxWoW.Me.CurrentTarget, reqs, false);
        }

        /// <summary>
        /// This Method can enable / disable the Moving Check from CanCast and should fix Pauls issues
        /// </summary>
        /// <param name="spell">Name of the spell</param>
        /// <param name="expiryTime">expiration time</param>
        /// <param name="onUnit">which unit</param>
        /// <param name="reqs">requirements</param>
        /// <param name="ignoreMoving">false is default and does CanCast-check like all the time, set to true if u wanna ignore movin. usefull for talentchecks which enables Casting when moving</param>
        /// <returns></returns>
        public static Composite PreventDoubleCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null, bool ignoreMoving = false)
        {
            return
                new Decorator(
                    ret =>
                        ((reqs != null && reqs(ret)) || (reqs == null))
                        && onUnit != null
                        && onUnit(ret) != null
                        && SpellManager.CanCast(spell, onUnit(ret), true, !ignoreMoving)
                        && !DoubleCastEntries.ContainsKey(spell + onUnit(ret).GetHashCode()),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(a => CooldownTracker.SpellUsed(spell)), // performace increase.
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).GetHashCode(), expiryTime))));
        }

        public static Composite PreventDoubleCast(int spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret =>
                        ((reqs != null && reqs(ret)) || (reqs == null))
                        && onUnit != null
                        && onUnit(ret) != null
                        && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture) + onUnit(ret).GetHashCode()),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(ret => CooldownTracker.SpellUsed(spell)),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).GetHashCode(), expiryTime))));
        }

        public static Composite PreventDoubleCastNoCanCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret =>
                        ((reqs != null && reqs(ret)) || (reqs == null))
                        && onUnit != null
                        && onUnit(ret) != null
                        && !DoubleCastEntries.ContainsKey(spell + onUnit(ret).GetHashCode()),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(ret => CooldownTracker.SpellUsed(spell)),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).GetHashCode(), expiryTime))));
        }

        public static Composite PreventDoubleChannel(string spell, double expiryTime, bool checkCancast, Selection<bool> reqs = null)
        {
            return PreventDoubleChannel(spell, expiryTime, checkCancast, onUnit => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite PreventDoubleChannel(string spell, double expiryTime, bool checkCancast, UnitSelectionDelegate onUnit, Selection<bool> reqs)
        {
            return PreventDoubleChannel(spell, expiryTime, checkCancast, onUnit, reqs, false);
        }

        public static Composite PreventDoubleChannel(string spell, double expiryTime, bool checkCancast, UnitSelectionDelegate onUnit, Selection<bool> reqs, bool ignoreMoving)
        {
            return new Decorator(
                delegate(object a)
                {
                    if (IsChanneling)
                        return false;

                    if (!reqs(a))
                        return false;

                    if (onUnit != null && DoubleCastEntries.ContainsKey(spell + onUnit(a).GetHashCode()))
                        return false;

                    if (onUnit != null && (checkCancast && !SpellManager.CanCast(spell, onUnit(a), true, !ignoreMoving)))
                        return false;

                    return true;
                },
                new Sequence(
                    new Action(a => SpellManager.Cast(spell, onUnit(a))),
                    new Action(ret => CooldownTracker.SpellUsed(spell)),
                    new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).GetHashCode(), expiryTime))));
        }

        /*
        public static Composite PreventDoubleHeal(string spell, double expiryTime, int HP = 100, Selection<bool> reqs = null)
        {
            return PreventDoubleHeal(spell, expiryTime, on => HealingManager.HealTarget, HP, reqs);
        }

        public static Composite PreventDoubleHeal(string spell, double expiryTime, UnitSelectionDelegate onUnit, int HP = 100, Selection<bool> reqs = null)
        {
            return new Decorator(
                ret => (onUnit != null && onUnit(ret) != null && (reqs == null || reqs(ret)) && onUnit(ret).HealthPercent <= HP) && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture) + onUnit(ret).GetHashCode()),
                new Sequence(
                    new Action(a => SpellManager.Cast(spell, onUnit(a))),
                    new Action(ret => CooldownTracker.SpellUsed(spell)),
                    new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).GetHashCode(), expiryTime))));
        }
        */
        public static Composite PreventDoubleCastOnGround(string spell, double expiryTime, LocationRetriever onLocation)
        {
            return PreventDoubleCastOnGround(spell, expiryTime, onLocation, ret => true);
        }

        public static Composite PreventDoubleCastOnGround(string spell, double expiryTime, LocationRetriever onLocation, CanRunDecoratorDelegate requirements, bool waitForSpell = false)
        {
            return new Decorator(
                    ret =>
                    onLocation != null && requirements(ret) && SpellManager.CanCast(spell) &&
                    /*!BossList.IgnoreAoE.Contains(StyxWoW.Me.CurrentTarget.Entry) &&*/
                    (StyxWoW.Me.Location.Distance(onLocation(ret)) <= SpellManager.Spells[spell].MaxRange ||
                     SpellManager.Spells[spell].MaxRange == 0) && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture) + onLocation(ret)),
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spell)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Name == spell, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret))),
                        new Action(ret => CooldownTracker.SpellUsed(spell)),
                        new Action(ret => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onLocation(ret), expiryTime))));
        }

        struct DoubleCastSpell
        {
            public DoubleCastSpell(string spellName, double expiryTime, DateTime currentTime)
                : this()
            {

                DoubleCastSpellName = spellName;
                DoubleCastExpiryTime = expiryTime;
                DoubleCastCurrentTime = currentTime;
            }

            private string DoubleCastSpellName { get; set; }
            public double DoubleCastExpiryTime { get; set; }
            public DateTime DoubleCastCurrentTime { get; set; }
        }

        #endregion
 
        #region Cast - by name

        /// <summary>
        ///   Creates a behavior to cast a spell by name. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name)
        {
            return Cast(name, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, with special requirements. Returns RunStatus.Success if successful,
        ///   RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, SimpleBooleanDelegate requirements)
        {
            return Cast(name, ret => true, ret => StyxWoW.Me.CurrentTarget, requirements);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, UnitSelectionDelegate onUnit)
        {
            return Cast(name, ret => true, onUnit, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            return Cast(name, ret => true, onUnit, requirements);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, with special requirements, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name="checkMovement"></param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        ///
        public static Composite Cast(string name, SimpleBooleanDelegate checkMovement, UnitSelectionDelegate onUnit,
            SimpleBooleanDelegate requirements)
        {
            return new Decorator(ret =>requirements != null && onUnit != null && requirements(ret) && onUnit(ret) != null && name != null && SpellManager.CanCast(name, onUnit(ret), true), 
                new Throttle(
                    new Action(ret =>
                        {
                            Logger.Write(string.Format("Casting {0} on {1}", name, onUnit(ret).SafeName()));
                            SpellManager.Cast(name, onUnit(ret));
                        })
                    )
                );
        }

        #endregion

        #region Cast - by ID

        /// <summary>
        ///   Creates a behavior to cast a spell by ID. Returns RunStatus.Success if successful,
        ///   RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">Identifier for the spell.</param>
        /// <returns>.</returns>
        public static Composite Cast(int spellId)
        {
            return Cast(spellId, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by ID, with special requirements. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">Identifier for the spell.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(int spellId, SimpleBooleanDelegate requirements)
        {
            return Cast(spellId, ret => StyxWoW.Me.CurrentTarget, requirements);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by ID, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">Identifier for the spell.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite Cast(int spellId, UnitSelectionDelegate onUnit)
        {
            return Cast(spellId, onUnit, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by ID, with special requirements, on a specific unit.
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">Identifier for the spell.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(int spellId, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            return
                new Decorator(ret => requirements != null && requirements(ret) && onUnit != null && onUnit(ret) != null && SpellManager.CanCast(spellId, onUnit(ret),true), 
                    new Action(ret =>
                        {
                            Logger.Write(string.Format("Casting {0} on {1}", spellId, onUnit(ret).SafeName()));
                            SpellManager.Cast(spellId);
                        }));
        }

        #endregion

        #region Buff - by name

        public static readonly Dictionary<string, DateTime> DoubleCastPreventionDict =
            new Dictionary<string, DateTime>();

        public static Composite Buff(string name, params string[] buffNames)
        {
            return buffNames.Length > 0 ? Buff(name, ret => true, buffNames) : Buff(name, ret => true, name);
        }

        public static Composite Buff(string name, bool myBuff)
        {
            return Buff(name, myBuff, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name, with special requirements, on current target. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name of the buff</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns></returns>
        public static Composite Buff(string name, SimpleBooleanDelegate requirements)
        {
            return Buff(name, false, ret => StyxWoW.Me.CurrentTarget, requirements, name);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name of the buff</param>
        /// <param name = "onUnit">The on unit</param>
        /// <returns></returns>
        public static Composite Buff(string name, UnitSelectionDelegate onUnit)
        {
            return Buff(name, false, onUnit, ret => true, name);
        }

        public static Composite Buff(string name, bool myBuff, params string[] buffNames)
        {
            return Buff(name, myBuff, ret => true, buffNames);
        }

        public static Composite Buff(string name, UnitSelectionDelegate onUnit, params string[] buffNames)
        {
            return Buff(name, onUnit, ret => true, buffNames);
        }

        public static Composite Buff(string name, SimpleBooleanDelegate requirements, params string[] buffNames)
        {
            return Buff(name, ret => StyxWoW.Me.CurrentTarget, requirements, buffNames);
        }

        public static Composite Buff(string name, bool myBuff, UnitSelectionDelegate onUnit)
        {
            return Buff(name, myBuff, onUnit, ret => true);
        }

        public static Composite Buff(string name, bool myBuff, SimpleBooleanDelegate requirements)
        {
            return Buff(name, myBuff, ret => StyxWoW.Me.CurrentTarget, requirements);
        }

        public static Composite Buff(string name, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            return Buff(name, false, onUnit, requirements);
        }

        public static Composite Buff(string name, bool myBuff, SimpleBooleanDelegate requirements,
            params string[] buffNames)
        {
            return Buff(name, myBuff, ret => StyxWoW.Me.CurrentTarget, requirements, buffNames);
        }

        public static Composite Buff(string name, bool myBuff, UnitSelectionDelegate onUnit, params string[] buffNames)
        {
            return Buff(name, myBuff, onUnit, ret => true, buffNames);
        }

        public static Composite Buff(string name, bool myBuff, UnitSelectionDelegate onUnit,
            SimpleBooleanDelegate requirements)
        {
            return Buff(name, myBuff, onUnit, requirements, name);
        }

        public static Composite Buff(string name, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements,
            params string[] buffNames)
        {
            return Buff(name, false, onUnit, requirements, buffNames);
        }

        //private static string _lastBuffCast = string.Empty;
        //private static System.Diagnostics.Stopwatch _castTimer = new System.Diagnostics.Stopwatch();
        /// <summary>
        ///   Creates a behavior to cast a buff by name, with special requirements, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name of the buff</param>
        /// <param name = "myBuff">Check for self debuffs or not</param>
        /// <param name = "onUnit">The on unit</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns></returns>
        public static Composite Buff(string name, bool myBuff, UnitSelectionDelegate onUnit,
            SimpleBooleanDelegate requirements, params string[] buffNames)
        {
            //if (name == _lastBuffCast && _castTimer.IsRunning && _castTimer.ElapsedMilliseconds < 250)
            //{
            //    return new Action(ret => RunStatus.Success);
            //}

            //if (name == _lastBuffCast && StyxWoW.Me.IsCasting)
            //{
            //    _castTimer.Reset();
            //    _castTimer.Start();
            //    return new Action(ret => RunStatus.Success);
            //}

            return
                new Decorator(
                    ret =>
                    onUnit(ret) != null && !DoubleCastPreventionDict.ContainsKey(name) &&
                    buffNames.All(b => myBuff ? !onUnit(ret).HasMyAura(b) : !onUnit(ret).HasAura(b)),
                    new Sequence( // new Action(ctx => _lastBuffCast = name),
                        Cast(name, onUnit, requirements),
                        new DecoratorContinue(ret => SpellManager.Spells[name].CastTime > 0,
                            new Sequence(new WaitContinue(1, ret => StyxWoW.Me.IsCasting,
                                new Action(ret => UpdateDoubleCastDict(name)))))));
        }

        private static void UpdateDoubleCastDict(string spellName)
        {
            if (DoubleCastPreventionDict.ContainsKey(spellName))
                DoubleCastPreventionDict[spellName] = DateTime.UtcNow;

            DoubleCastPreventionDict.Add(spellName, DateTime.UtcNow);
        }

        #endregion

        #region BuffSelf - by name

        /// <summary>
        ///   Creates a behavior to cast a buff by name on yourself. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/6/2011.
        /// </remarks>
        /// <param name = "name">The buff name.</param>
        /// <returns>.</returns>
        public static Composite BuffSelf(string name)
        {
            return Buff(name, ret => StyxWoW.Me, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name on yourself with special requirements. Returns RunStatus.Success if
        ///   successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/6/2011.
        /// </remarks>
        /// <param name = "name">The buff name.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite BuffSelf(string name, SimpleBooleanDelegate requirements)
        {
            return Buff(name, ret => StyxWoW.Me, requirements);
        }

        #endregion

        #region Buff - by ID

        /// <summary>
        ///   Creates a behavior to cast a buff by name on current target. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">The ID of the buff</param>
        /// <returns></returns>
        public static Composite Buff(int spellId)
        {
            return Buff(spellId, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name, with special requirements, on current target. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">The ID of the buff</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns></returns>
        public static Composite Buff(int spellId, SimpleBooleanDelegate requirements)
        {
            return Buff(spellId, ret => StyxWoW.Me.CurrentTarget, requirements);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">The ID of the buff</param>
        /// <param name = "onUnit">The on unit</param>
        /// <returns></returns>
        public static Composite Buff(int spellId, UnitSelectionDelegate onUnit)
        {
            return Buff(spellId, onUnit, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by name, with special requirements, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spellId">The ID of the buff</param>
        /// <param name = "onUnit">The on unit</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns></returns>
        public static Composite Buff(int spellId, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            return new Decorator(ret => onUnit(ret) != null && onUnit(ret).Auras.Values.All(a => a.SpellId != spellId),
                Cast(spellId, onUnit, requirements));
        }

        #endregion

        #region BufSelf - by ID

        /// <summary>
        ///   Creates a behavior to cast a buff by ID on yourself. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/6/2011.
        /// </remarks>
        /// <param name = "spellId">The buff ID.</param>
        /// <returns>.</returns>
        public static Composite BuffSelf(int spellId)
        {
            return Buff(spellId, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a buff by ID on yourself with special requirements. Returns RunStatus.Success if
        ///   successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/6/2011.
        /// </remarks>
        /// <param name = "spellId">The buff ID.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite BuffSelf(int spellId, SimpleBooleanDelegate requirements)
        {
            return Buff(spellId, ret => StyxWoW.Me, requirements);
        }

        #endregion

        #region Heal - by name

        /// <summary>
        ///   Creates a behavior to cast a heal spell by name. Heal behaviors will make sure
        ///   we don't double cast. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <returns>.</returns>
        public static Composite Heal(string name)
        {
            return Heal(name, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a heal spell by name, with special requirements. Heal behaviors will make sure
        ///   we don't double cast. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Heal(string name, SimpleBooleanDelegate requirements)
        {
            return Heal(name, ret => true, ret => StyxWoW.Me, requirements);
        }

        /// <summary>
        ///   Creates a behavior to cast a heal spell by name, on a specific unit. Heal behaviors will make sure
        ///   we don't double cast. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite Heal(string name, UnitSelectionDelegate onUnit)
        {
            return Heal(name, ret => true, onUnit, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a heal spell by name, on a specific unit. Heal behaviors will make sure
        ///   we don't double cast. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Heal(string name, UnitSelectionDelegate onUnit, SimpleBooleanDelegate requirements)
        {
            return Heal(name, ret => true, onUnit, requirements);
        }


        public static Composite Heal(string name, SimpleBooleanDelegate checkMovement, UnitSelectionDelegate onUnit,
            SimpleBooleanDelegate requirements, bool allowLagTollerance = false)
        {
            return Heal(name, checkMovement, onUnit, requirements, 
                ret => onUnit(ret).HealthPercent > SingularSettings.Instance.IgnoreHealTargetsAboveHealth, 
                false);
        }

        // used by Spell.Heal() - save fact we are queueing this Heal spell if a spell cast/gcd is in progress already.  this could only occur during 
        // .. the period of latency at the end of a cast where Singular allows you to begin the next one
        private static bool IsSpellBeingQueued = false;

        /// <summary>
        ///   Creates a behavior to cast a heal spell by name, with special requirements, on a specific unit. Heal behaviors will make sure
        ///   we don't double cast. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name="checkMovement"></param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <param name="cancel">The cancel cast in progress delegate</param>
        /// <param name="allowLagTollerance">allow next spell to queue before this one completes</param>
        /// <returns>.</returns>
        public static Composite Heal(string name, SimpleBooleanDelegate checkMovement, UnitSelectionDelegate onUnit,
            SimpleBooleanDelegate requirements, SimpleBooleanDelegate cancel, bool allowLagTollerance = false)
        {
            return new Sequence(
                    
                // save context of currently in a GCD or IsCasting before our Cast
                new Action( ret => IsSpellBeingQueued = (SpellManager.GlobalCooldown || StyxWoW.Me.IsCasting || StyxWoW.Me.ChanneledSpell != null)),

                Cast(name, checkMovement, onUnit, requirements),

                // continue if not queueing spell, or we reahed end of cast/gcd of spell in progress
                new WaitContinue( 
                    new TimeSpan(0, 0, 0, 0, (int) StyxWoW.WoWClient.Latency * 2),
                    ret => !IsSpellBeingQueued || !(SpellManager.GlobalCooldown || StyxWoW.Me.IsCasting || StyxWoW.Me.ChanneledSpell != null),
                    new ActionAlwaysSucceed()
                    ),

                // now for non-instant spell, wait for .IsCasting to be true
                new WaitContinue(1, 
                    ret => {
                        WoWSpell spell;
                        if (SpellManager.Spells.TryGetValue(name, out spell))
                        {
                            if (spell.CastTime == 0)
                                return true;

                            return StyxWoW.Me.IsCasting;
                        }

                        return true;
                        }, 
                    new ActionAlwaysSucceed()
                    ), 
                    
                // finally, wait at this point until heal completes
                new WaitContinue(10, 
                    ret => {
                        // Let channeled heals been cast till end.
                        if (StyxWoW.Me.ChanneledSpell != null) // .ChanneledCastingSpellId != 0)
                        {
                            return false;
                        }

                        // Interrupted or finished casting. Continue (note: following test doesn't deal with latency)
                        if (!StyxWoW.Me.IsCasting)
                        {
                            return true;
                        }

                        // when doing lag tolerance, don't wait for spell to complete before considering finished
                        if (allowLagTollerance)
                        {
                            TimeSpan castTimeLeft = StyxWoW.Me.CurrentCastTimeLeft;
                            if (castTimeLeft != TimeSpan.Zero && castTimeLeft.TotalMilliseconds < (StyxWoW.WoWClient.Latency * 2))
                                return true;
                        }

                        // 500ms left till cast ends. Shall continue for next spell
                        //if (StyxWoW.Me.CurrentCastTimeLeft.TotalMilliseconds < 500)
                        //{
                        //    return true;
                        //}

                        // temporary hack - checking requirements won't work -- either need global value or cancelDelegate
                        // if ( onUnit(ret).HealthPercent > SingularSettings.Instance.IgnoreHealTargetsAboveHealth) // (!requirements(ret))
                        if ( cancel(ret) )
                        {
                            SpellManager.StopCasting();
                            Logger.Write(System.Drawing.Color.Orange, "/cancel {0} on {1} @ {2:F1}%", name, onUnit(ret).SafeName(), onUnit(ret).HealthPercent);
                            return true;
                        }

                        return false;
                    }, 
                    new ActionAlwaysSucceed()
                    )
                );
        }

        #endregion

        #region CastOnGround - placeable spell casting

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on the ground at the specified location. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spell">The spell.</param>
        /// <param name = "onLocation">The on location.</param>
        /// <returns>.</returns>
        public static Composite CastOnGround(string spell, LocationRetriever onLocation)
        {
            return CastOnGround(spell, onLocation, ret => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on the ground at the specified location. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spell">The spell.</param>
        /// <param name = "onLocation">The on location.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite CastOnGround(string spell, LocationRetriever onLocation,
            SimpleBooleanDelegate requirements)
        {
            return CastOnGround(spell, onLocation, requirements, true);
        }


        /// <summary>
        ///   Creates a behavior to cast a spell by name, on the ground at the specified location. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "spell">The spell.</param>
        /// <param name = "onLocation">The on location.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <param name="waitForSpell">Waits for spell to become active on cursor if true. </param>
        /// <returns>.</returns>
        public static Composite CastOnGround(string spell, LocationRetriever onLocation,
            SimpleBooleanDelegate requirements, bool waitForSpell)
        {
            return
                new Decorator(
                    ret =>
                    requirements(ret) && onLocation != null && SpellManager.CanCast(spell) &&
                    (StyxWoW.Me.Location.Distance(onLocation(ret)) <= SpellManager.Spells[spell].MaxRange ||
                     SpellManager.Spells[spell].MaxRange == 0) && GameWorld.IsInLineOfSpellSight(StyxWoW.Me.Location, onLocation(ret)),
                    new Sequence(
                        new Action(ret => Logger.Write("Casting {0} at location {1}", spell, onLocation(ret))),
                        new Action(ret => SpellManager.Cast(spell)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Name == spell, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret)))));
        }

        public static Composite CastOnGround(int spellid, LocationRetriever onLocation)
        {
            return CastOnGround(spellid, onLocation, ret => true);
        }

        public static Composite CastOnGround(int spellid, LocationRetriever onLocation, CanRunDecoratorDelegate requirements, bool waitForSpell = false)
        {
            return
                new Decorator(
                    ret => onLocation != null && requirements(ret),
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spellid)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Id == spellid, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret)))));
        }
        #endregion

        #region Resurrect

        /// <summary>
        ///   Creates a behavior to resurrect dead players around. This behavior will res each player once in every 10 seconds.
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 16/12/2011.
        /// </remarks>
        /// <param name = "spellName">The name of resurrection spell.</param>
        /// <returns>.</returns>
        public static Composite Resurrect(string spellName)
        {
            return new PrioritySelector(ctx => Unit.ResurrectablePlayers.FirstOrDefault(u => !Blacklist.Contains(u)),
                new Decorator(ctx => ctx != null && SingularRoutine.CurrentWoWContext != WoWContext.Battlegrounds,
                    new Sequence(Cast(spellName, ctx => (WoWPlayer) ctx),
                        new Action(ctx => Blacklist.Add((WoWPlayer) ctx, TimeSpan.FromSeconds(30))))));
        }

        #endregion

        #region Cooldowntracker

        public static class CooldownTracker
        {

            public static bool IsOnCooldown(int spell)
            {
                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.Cooldown;
                        }

                        return false;
                    }
                }
                return false;
            }

            public static bool IsOnCooldown(string spell)
            {
                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.Cooldown;
                        }

                        return false;
                    }
                }
                return false;
            }

            public static TimeSpan CooldownTimeLeft(int spell)
            {

                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.CooldownTimeLeft;
                        }

                        return TimeSpan.MaxValue;
                    }
                }
                return TimeSpan.MaxValue;
            }

            public static TimeSpan CooldownTimeLeft(string spell)
            {

                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.CooldownTimeLeft;
                        }

                        return TimeSpan.MaxValue;
                    }
                }
                return TimeSpan.MaxValue;
            }

            public static double CastTime(int spell)
            {

                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.CastTime / 1000.0;
                        }

                        return 99999.9;
                    }
                }
                return 99999.9;
            }

            public static double CastTime(string spell)
            {

                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;

                    long lastUsed;
                    if (cooldowns.TryGetValue(result, out lastUsed))
                    {
                        if (DateTime.Now.Ticks < lastUsed)
                        {
                            return result.CastTime / 1000.0;
                        }

                        return 99999.9;
                    }
                }
                return 99999.9;
            }

            public static void SpellUsed(string spell)
            {
                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;
                    cooldowns[result] = result.CooldownTimeLeft.Ticks + DateTime.Now.Ticks;
                }
            }

            public static void SpellUsed(int spell)
            {
                SpellFindResults results;
                if (SpellManager.FindSpell(spell, out results))
                {
                    WoWSpell result = results.Override ?? results.Original;
                    cooldowns[result] = result.CooldownTimeLeft.Ticks + DateTime.Now.Ticks;
                }
            }

            private static Dictionary<WoWSpell, long> cooldowns = new Dictionary<WoWSpell, long>();
        }

        #endregion

        #region Nested type: Selection

        internal delegate T Selection<out T>(object context);

        #endregion
    }
}