﻿using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using nManager.FiniteStateMachine;
using nManager.Helpful;
using nManager.Wow.Class;
using nManager.Wow.Enums;
using nManager.Wow.Helpers;
using nManager.Wow.ObjectManager;
using Timer = nManager.Helpful.Timer;

namespace nManager.Wow.Bot.States
{
    public class FlightMasterDiscovery : State
    {
        public override string DisplayName
        {
            get { return "FlightMasterDiscovery"; }
        }

        public override int Priority { get; set; }

        public override List<State> NextStates
        {
            get { return new List<State>(); }
        }

        public override List<State> BeforeStates
        {
            get { return new List<State>(); }
        }

        private readonly Timer _timerCheck = new Timer(10000);
        private WoWUnit _flightMaster = new WoWUnit(0);
        private bool _enRoute = false;

        public override bool NeedToRun
        {
            get
            {
                if (Usefuls.BadBottingConditions || Usefuls.ShouldFight)
                    return false;

                if (!_timerCheck.IsReady && !_enRoute)
                    return false;
                _timerCheck.Reset();
                WoWUnit flightMaster = ObjectManager.ObjectManager.GetNearestWoWUnit(ObjectManager.ObjectManager.GetWoWUnitFlightMasterUndiscovered());
                if (!flightMaster.IsValid || flightMaster.UnitFlightMasteStatus != UnitFlightMasterStatus.FlightUndiscovered)
                {
                    _enRoute = false;
                    return false;
                }
                _flightMaster = flightMaster;
                bool success;
                PathFinder.FindPath(_flightMaster.Position, out success);
                return success; // only return true if we have a valid path to target (in case we are currently in a zone where we need to use a portal etc.
            }
        }

        public override void Run()
        {
            if (!_enRoute)
            {
                MovementManager.StopMove();
                Logging.Write("Nearby Flight Master " + _flightMaster.Name + " (" + _flightMaster.Entry + ") is not yet discovered.");
            }
            _enRoute = true;
            Npc target = new Npc
            {
                Entry = _flightMaster.Entry,
                Position = _flightMaster.Position,
                Name = _flightMaster.Name,
                ContinentIdInt = Usefuls.ContinentId,
                Faction = ObjectManager.ObjectManager.Me.PlayerFaction.ToLower() == "horde" ? Npc.FactionType.Horde : Npc.FactionType.Alliance,
            };
            uint baseAddress = MovementManager.FindTarget(ref target, 5.0f);
            if (MovementManager.InMovement)
                return;
            if (_flightMaster.GetDistance > 5)
                return;
            if (baseAddress > 0)
            {
                MovementManager.StopMove(); // avoid a red wow error
                Thread.Sleep(150);
                Interact.InteractWith(baseAddress);
            }
        }
    }
}