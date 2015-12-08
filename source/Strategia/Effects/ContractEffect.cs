﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Strategies;
using ContractConfigurator;

namespace Strategia
{
    public class ContractEffect : StrategyEffect, IObjectiveEffect, ICanDeactivateEffect
    {
        /// <summary>
        /// Separate MonoBehaviour for checking, as the strategy system only gets update calls in flight.
        /// </summary>
        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        public class ContractChecker : MonoBehaviour
        {
            public static ContractChecker Instance;
            public List<ContractEffect> effects = new List<ContractEffect>();

            void Start()
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }

            public void Register(ContractEffect effect)
            {
                effects.AddUnique(effect);
            }

            public void Unregister(ContractEffect effect)
            {
                effects.Remove(effect);
            }

            public void Update()
            {
                List<ContractEffect> toDeactivate = new List<ContractEffect>();
                foreach (ContractEffect effect in effects)
                {
                    // Assign the contract
                    if (effect.contract == null)
                    {
                        effect.contract = ContractSystem.Instance.GetCurrentActiveContracts<ConfiguredContract>().
                            Where(c => c.contractType != null && c.contractType.name == effect.contractType).FirstOrDefault();
                    }

                    // Check if the contract has failed
                    if (effect.Parent.IsActive && effect.contract != null && effect.contract.ContractState == Contract.State.Failed)
                    {
                        toDeactivate.Add(effect);

                        MessageSystem.Instance.AddMessage(new MessageSystem.Message("Failed to complete strategy '" + effect.Parent.Title + "'",
                            effect.failureMessage, MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.FAIL));
                    }

                    // Check if the contract has succeeded
                    if (effect.Parent.IsActive && effect.contract != null && effect.contract.ContractState == Contract.State.Completed)
                    {
                        toDeactivate.Add(effect);

                        MessageSystem.Instance.AddMessage(new MessageSystem.Message("Completed strategy '" + effect.Parent.Title + "'",
                            effect.completedMessage, MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE));
                    }
                }

                // Late deactivate, otherwise we mess up the iterator
                foreach (ContractEffect effect in toDeactivate)
                {
                    (effect.Parent as StrategiaStrategy).ForceDeactivate();
                }
            }
        }

        public CelestialBody targetBody;
        public double rewardFunds { get; private set; }
        public float rewardScience { get; private set; }
        public float rewardReputation { get; private set; }
        public double failureFunds { get; private set; }
        public float failureScience { get; private set; }
        public float failureReputation { get; private set; }
        public double advanceFunds { get; private set; }
        public float advanceScience { get; private set; }
        public float advanceReputation { get; private set; }
        public string synopsis;
        public string completedMessage;
        public string failureMessage;
        public Duration duration;

        public string contractType;

        public Contract contract;

        public ContractEffect(Strategy parent)
            : base(parent)
        {
        }

        public IEnumerable<string> ObjectiveText()
        {
            yield return synopsis;

            if (duration != null)
            {
                yield return "Must complete objective within " + KSPUtil.PrintDateDelta((int)duration.Value, false) + ".";
            }
        }

        protected string MinimumDurationText()
        {
            return "Cannot deactivate, objective must be completed before " +
                KSPUtil.PrintDateNew((int)(duration.Value + Parent.DateActivated), false);
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            contractType = ConfigNodeUtil.ParseValue<string>(node, "contractType");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
            rewardFunds = ConfigNodeUtil.ParseValue<double>(node, "rewardFunds", 0.0);
            rewardScience = ConfigNodeUtil.ParseValue<float>(node, "rewardScience", 0.0f);
            rewardReputation = ConfigNodeUtil.ParseValue<float>(node, "rewardReputation", 0.0f);
            failureFunds = ConfigNodeUtil.ParseValue<double>(node, "failureFunds", 0.0);
            failureScience = ConfigNodeUtil.ParseValue<float>(node, "failureScience", 0.0f);
            failureReputation = ConfigNodeUtil.ParseValue<float>(node, "failureReputation", 0.0f);
            advanceFunds = ConfigNodeUtil.ParseValue<double>(node, "advanceFunds", 0.0);
            advanceScience = ConfigNodeUtil.ParseValue<float>(node, "advanceScience", 0.0f);
            advanceReputation = ConfigNodeUtil.ParseValue<float>(node, "advanceReputation", 0.0f);
            synopsis = ConfigNodeUtil.ParseValue<string>(node, "synopsis");
            completedMessage = ConfigNodeUtil.ParseValue<string>(node, "completedMessage");
            failureMessage = ConfigNodeUtil.ParseValue<string>(node, "failureMessage");
            duration = ConfigNodeUtil.ParseValue<Duration>(node, "duration", null);
        }

        protected override void OnRegister()
        {
            ContractChecker.Instance.Register(this);

            // Force contracts to generate immediately in case we need the associated contract
            ContractPreLoader.Instance.ResetGenerationFailure();
        }

        protected override void OnUnregister()
        {
            ContractChecker.Instance.Unregister(this);
        }

        public bool CanDeactivate(ref string reason)
        {
            if (duration != null && Parent.DateActivated + duration.Value > Planetarium.fetch.time)
            {
                reason = MinimumDurationText();
                return false;
            }

            return true;
        }
    }
}
