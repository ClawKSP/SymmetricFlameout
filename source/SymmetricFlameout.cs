/*
 * This module written by Claw. For more details please visit
 * http://forum.kerbalspaceprogram.com/threads/97285-0-25-Stock-Bug-Fix-Modules
 * 
 * This mod is covered under the CC-BY-NC-SA license. See the license.txt for more details.
 * (https://creativecommons.org/licenses/by-nc-sa/4.0/)
 * 
 * Written for KSP v0.90.0
 *
 * SymmetricFlameout v0.01.00
 * 
 * This plugin allows symmetricly placed engines to talk with each other, so that they roll back
 * and flame out symmetrically. It also highlights the parts as they are nearing flameout status.
 * 
 * Change Log:
 * 
 * v0.01.00 - Initial release
 * 
 */

using UnityEngine;
using KSP;

namespace ClawKSP
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SFFix : MonoBehaviour
    {
        public void Start()
        {

            GameEvents.onVesselGoOffRails.Add(OffRails);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselGoOffRails.Remove(OffRails);
        }

        public void OffRails (Vessel VesselToFix)
        {
            for (int IndexParts = 0; IndexParts < VesselToFix.parts.Count; IndexParts++)
            {
                Part CurrentPart = VesselToFix.parts[IndexParts];

                for (int IndexModules = 0; IndexModules < CurrentPart.Modules.Count; IndexModules++)
                {
                    if ("ModuleEngines" == CurrentPart.Modules[IndexModules].moduleName)
                    {
                        bool UsesIntakeAir = false;

                        ModuleEngines ME = (ModuleEngines) CurrentPart.Modules[IndexModules];

                        for (int IndexPropellants = 0; IndexPropellants < ME.propellants.Count; IndexPropellants++)
                        {
                            if ("IntakeAir" == ME.propellants[IndexPropellants].name)
                            {
                                UsesIntakeAir = true;
                                break;
                            }
                        }
                        if (false == UsesIntakeAir)
                        {
                            continue;
                        }

                        for (int IndexCheck = 0; IndexCheck < CurrentPart.Modules.Count; IndexCheck++)
                        {
                            //Debug.LogWarning("Module: " + CurrentPart.Modules[IndexCheck].moduleName);
                            if ("MSFFix" == CurrentPart.Modules[IndexCheck].moduleName)
                            {
                                return;
                            }
                        }
                        //Debug.LogWarning("OffRails(): Adding MSFFix " + CurrentPart.Modules[IndexModules].name);
                        MSFFix TempModule = (MSFFix) CurrentPart.AddModule("MSFFix");
                        //TempModule.moduleName = "MSFFix";
                    }
                }
            }
        }
    }

    public class MSFFix : PartModule
    {
        private enum MSFFixState
        {
            RUNNING,
            STARVING,
            FLAMEOUT
        }

        public ModuleEngines ME = null;
        private MSFFix LeadMSFFix = null;
        public int IndexIntakeAir = 0;

        public bool ThrottleSet = false;
        public float GroupThrottle = 0f;
        private MSFFixState GroupState = MSFFixState.RUNNING;
        private MSFFixState LocalState = MSFFixState.RUNNING;
        public double TotalRequirement = 0d;
        public double TotalAmount = 0d;
        private float PropellantRatio = 15f;
        private float OriginalRatio = 15f;

        public void Start()
        {
            Debug.Log("MSFFix.Start()");

            for (int IndexModules = 0; IndexModules < part.Modules.Count; IndexModules++)
            {
                if ("ModuleEngines" == part.Modules[IndexModules].moduleName)
                {
                    ME = (ModuleEngines) part.Modules[IndexModules];
                    ME.manuallyOverridden = true;

                    for (int IndexPropellants = 0; IndexPropellants < ME.propellants.Count; IndexPropellants++)
                    {
                        if ("IntakeAir" == ME.propellants[IndexPropellants].name)
                        {
                            IndexIntakeAir = IndexPropellants;
                            OriginalRatio = ME.propellants[IndexPropellants].ratio;
                            PropellantRatio = OriginalRatio;
                            break;
                        }
                    }

                    break;
                }
            }

            if (null == ME)
            {
                part.RemoveModule(this);
                return;
            }
        }

        public void OnDestroy()
        {
            Debug.Log("MSFix.OnDestroy()");
            if (null != ME)
            {
                ME.manuallyOverridden = false;
            }
        }

        public void FixedUpdate()
        {

            if (null != ME)
            {

                float mainThrottle = vessel.ctrlState.mainThrottle;

                if (false == ThrottleSet || this == LeadMSFFix)
                {
                    LeadMSFFix = this;

                    double percentFill = 1d;
                    //Debug.LogWarning("Amount: " + TotalAmount + " || Requirement: " + TotalRequirement);

                    if (0d != TotalRequirement)
                    {
                        percentFill = TotalAmount / TotalRequirement;
                    }
                    //Debug.LogWarning("PercentFill: " + percentFill);


                    if (1d <= percentFill)
                    {
                        //Debug.LogWarning("Increment: " + GroupThrottle);
                        GroupThrottle += 0.01f;
                    }
                    else if (0.97d >= percentFill)
                    {
                        //Debug.LogWarning("Chop: " + GroupThrottle);
                        GroupThrottle = 0.01f;
                    }
                    else
                    {
                        //Debug.LogWarning("Modulate: " + GroupThrottle);
                        GroupThrottle = GroupThrottle * (float)percentFill * (float)percentFill * (float)percentFill * (float)percentFill;
                    }

                    if (GroupThrottle > vessel.ctrlState.mainThrottle)
                    {
                        //Debug.LogWarning("Max");
                        GroupThrottle = vessel.ctrlState.mainThrottle;
                    }
                    else if (GroupThrottle < 0.666f * mainThrottle)
                    {
                        //Debug.LogWarning("Min");
                        GroupThrottle = 0.666f * mainThrottle;
                    }

                    if (GroupThrottle <= (0.667f * mainThrottle)
                        && percentFill < 0.9999d)
                    {
                        PropellantRatio = OriginalRatio * (float)(part.symmetryCounterparts.Count + 1) / (ME.ignitionThreshold);

                        if (MSFFixState.FLAMEOUT != GroupState)
                        {
                            // PropellantRatio = 15f * (float)(part.symmetryCounterparts.Count + 1) / (ME.ignitionThreshold * 0.667f);
                            GroupState = MSFFixState.FLAMEOUT;
                            //UpdateState();
                        }
                    }
                    else if (GroupThrottle < (0.98 * mainThrottle)
                        && MSFFixState.FLAMEOUT != GroupState
                        && percentFill < 0.9999d)
                    {
                        if (MSFFixState.STARVING != GroupState)
                        {
                            PropellantRatio = OriginalRatio;
                            GroupState = MSFFixState.STARVING;
                            //UpdateState();
                        }
                    }
                    else
                    {
                        if (MSFFixState.RUNNING != GroupState)
                        {
                            PropellantRatio = OriginalRatio;
                            GroupState = MSFFixState.RUNNING;
                            //UpdateState();
                        }
                    }

                    if (MSFFixState.FLAMEOUT == LocalState)
                    {
                        //PropellantRatio = 15f * (float)(part.symmetryCounterparts.Count + 1) / (ME.ignitionThreshold * (0.01f / 0.667f));
                        if ((ME.ignitionThreshold / (part.symmetryCounterparts.Count + 1)) <= percentFill)
                        {
                            PropellantRatio = OriginalRatio;
                            GroupState = MSFFixState.RUNNING;
                        }
                    }

                    TotalRequirement = 0f;
                    TotalAmount = 0f;

                    ME.propellants[IndexIntakeAir].ratio = PropellantRatio;

                    //Debug.LogWarning("Main: " + mainThrottle + "Throttle: " + GroupThrottle + " || percentFill: " + percentFill);
                    //Debug.LogWarning("Ratio: " + PropellantRatio);
                }

                if (LocalState != GroupState)
                {
                    LocalState = GroupState;

                    switch (GroupState)
                    {
                        case MSFFixState.RUNNING:
                            ME.propellants[IndexIntakeAir].ratio = PropellantRatio;
                            part.SetHighlightColor();
                            part.SetHighlight(false, true);
                            break;
                        case MSFFixState.STARVING:
                            part.SetHighlightColor(Color.yellow);
                            part.SetHighlight(true, false);
                            break;
                        case MSFFixState.FLAMEOUT:
                            ME.propellants[IndexIntakeAir].ratio = PropellantRatio;
                            part.SetHighlightColor(Color.red);
                            part.SetHighlight(true, false);
                            break;
                    }
                }

                vessel.ctrlState.mainThrottle = GroupThrottle;
                ME.manuallyOverridden = false;
                ME.FixedUpdate();
                ME.manuallyOverridden = true;
                vessel.ctrlState.mainThrottle = mainThrottle;

                for (int IndexPropellants = 0; IndexPropellants < ME.propellants.Count; IndexPropellants++)
                {
                    if ("IntakeAir" == ME.propellants[IndexPropellants].name)
                    {
                        TotalRequirement += ME.propellants[IndexPropellants].currentRequirement;
                        TotalAmount += ME.propellants[IndexPropellants].currentAmount;
                        CounterpartUpdate();
                        break;
                    }
                }
            }
        }

        private void CounterpartUpdate()
        {
            if (null == part.symmetryCounterparts) { return; }

            for (int IndexParts = 0; IndexParts < part.symmetryCounterparts.Count; IndexParts++)
            {                
                Part CurrentPart = part.symmetryCounterparts[IndexParts];

                for (int IndexModules = 0; IndexModules < CurrentPart.Modules.Count; IndexModules++)
                {
                    if ("MSFFix" == CurrentPart.Modules[IndexModules].moduleName)
                    {
                        MSFFix ModuleThrottle = (MSFFix)CurrentPart.Modules[IndexModules];
                        ModuleThrottle.GroupThrottle = GroupThrottle;
                        ModuleThrottle.ThrottleSet = true;
                        ModuleThrottle.TotalRequirement = TotalRequirement;
                        ModuleThrottle.TotalAmount = TotalAmount;
                        ModuleThrottle.GroupState = GroupState;
                        ModuleThrottle.PropellantRatio = PropellantRatio;
                    }
                }
            }
        }

        public void Update()
        {
            ThrottleSet = false;
        }
    }
}
