﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Poltergeist.GhostInteractibles.Specific
{
    public enum CostType { DOOR, VALVE, SHIPDOOR, COMPANYBELL, HANGARDOOR, MISC}
    public class BasicInteractible : NaiveInteractible
    {
        public CostType costType = CostType.MISC;
        private InteractTrigger trigger;
        private float interactDuration = 1;
        private float interactTime = 0;
        public bool isHeld = false;

        /**
         * On awake, grab the trigger
         */
        private void Awake()
        {
            trigger = GetComponent<InteractTrigger>();
        }

        /**
         * Get the cost of the interaction
         */
        public override float GetCost()
        {
            switch(costType)
            {
                case CostType.DOOR:
                    return Poltergeist.Config.DoorCost.Value;
                case CostType.VALVE:
                    return Poltergeist.Config.ValveCost.Value;
                case CostType.SHIPDOOR:
                    return Poltergeist.Config.ShipDoorCost.Value;
                case CostType.COMPANYBELL:
                    return Poltergeist.Config.CompanyBellCost.Value;
                case CostType.HANGARDOOR:
                    return Poltergeist.Config.BigDoorCost.Value;
                default: //Should only be misc
                    return Poltergeist.Config.MiscCost.Value;
            }
        }

        private bool FeatureEnabled()
        {
            switch(costType)
            {
                case CostType.DOOR:
                    return Poltergeist.Config.EnableDoor.Value;
                case CostType.VALVE:
                    return Poltergeist.Config.EnableValve.Value;
                case CostType.SHIPDOOR:
                    return Poltergeist.Config.EnableShipDoor.Value;
                case CostType.COMPANYBELL:
                    return Poltergeist.Config.EnableCompanyBell.Value;
                case CostType.HANGARDOOR:
                    return Poltergeist.Config.EnableBigDoor.Value;
                default:
                    return Poltergeist.Config.EnableMisc.Value;
            }
        }

        /**
         * Do the actual interaction
         */
        public override float Interact(Transform playerTransform)
        {
            //Check if this feature is enabled
            if (!FeatureEnabled())
                return 0;

            //Don't let them interact without meeting the cost
            if (SpectatorCamController.instance.Power < GetCost())
                return 0;

            //Check to see if the interactor will let us do it
            if (trigger.interactable && (!trigger.interactCooldown || trigger.currentCooldownValue <= 0))
            {
                //Single-press interactions
                if (!isHeld)
                    trigger.Interact(playerTransform);

                //Long press interactions
                else
                    interactTime = interactDuration;
                return GetCost();
            }

            return 0;
        }

        /**
         * Gives the tip text
         */
        public override string GetTipText()
        {
            //Don't show anything if disabled
            if (!FeatureEnabled())
                return "";

            string retStr = "";

            //Display message for not having enough power
            if (SpectatorCamController.instance.Power < GetCost())
                return "Not Enough Power (" + GetCost().ToString("F0") + ")";

            //Set up the actual text
            if (!trigger.interactable)
                return trigger.disabledHoverTip; //Display no cost if you can't interact
            else
            {
                StringBuilder builder;
                if (!trigger.hoverTip.Equals(""))
                    builder = new StringBuilder(trigger.hoverTip);
                else
                    builder = new StringBuilder("Interact; [LMB]");

                retStr = builder.Replace("[LMB]", "[" + PoltergeistCustomInputs.GetInteractString() + "]").ToString();
            }

            return retStr + " (" + GetCost().ToString("F0") + ")";
        }

        /**
         * Every frame, check if we should keep interacting
         */
        private void Update()
        {
            interactTime -= Time.deltaTime;
            if(interactTime > 0 && trigger.holdInteraction)
            {
                trigger.HoldInteractNotFilled();
            }
        }
    }
}
