﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Strategies;
using Strategies.Effects;

namespace Strategia
{
    public class MinimumFacilityLevelRequirement : StrategyEffect, IRequirementEffect
    {
        SpaceCenterFacility facility;
        static Dictionary<SpaceCenterFacility, string> facilityNames = new Dictionary<SpaceCenterFacility, string>();
        int level;

        static MinimumFacilityLevelRequirement()
        {
            facilityNames[SpaceCenterFacility.Administration] = "Administration Facility";
            facilityNames[SpaceCenterFacility.AstronautComplex] = "Astronaut Complex";
            facilityNames[SpaceCenterFacility.LaunchPad] = "Launch Pad";
            facilityNames[SpaceCenterFacility.MissionControl] = "Mission Control";
            facilityNames[SpaceCenterFacility.ResearchAndDevelopment] = "Research and Development";
            facilityNames[SpaceCenterFacility.Runway] = "Runway";
            facilityNames[SpaceCenterFacility.SpaceplaneHangar] = "Spaceplane Hangar";
            facilityNames[SpaceCenterFacility.TrackingStation] = "Tracking Station";
            facilityNames[SpaceCenterFacility.VehicleAssemblyBuilding] = "Vehicle Assembly Building";
        }

        public string Reason
        {
            get
            {
                return "Requires " + facilityNames[facility] + " Level " + level + " or greater.";
            }
        }

        public MinimumFacilityLevelRequirement(Strategy parent)
            : base(parent)
        {
        }

        protected override void OnLoadFromConfig(ConfigNode node)
        {
            facility = ConfigNodeUtil.ParseValue<SpaceCenterFacility>(node, "facility", SpaceCenterFacility.Administration);
            level = ConfigNodeUtil.ParseValue<int>(node, "level");
        }

        public string RequirementText()
        {
            return facilityNames[facility] + " Level " + level;
        }

        public bool RequirementMet()
        {
            int currentLevel = (int)ScenarioUpgradeableFacilities.GetFacilityLevel(facility) * ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility) + 1;
            return currentLevel >= level;
        }
    }
}