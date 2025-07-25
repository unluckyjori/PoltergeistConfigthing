﻿using GameNetcodeStuff;
using Poltergeist.GhostInteractibles;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Reflection;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.DebugUI;

namespace Poltergeist
{
    public class SpectatorCamController : MonoBehaviour
    {
        public static SpectatorCamController instance = null;

        //Camera stuff
        private Camera cam;
        private float camMoveSpeed = 5f;
        private Light[] lights = new Light[4];

        //Power stuff
        private float maxPower = 100;
        private float power = 0;
        public float Power => power;

        //Tracking certain things
        private Transform ghostParent = null;
        private PlayerControllerB clientPlayer = null;
        public PlayerControllerB ClientPlayer => clientPlayer;
        public GhostHead head = null;
        private IGhostInteractible currentGhostInteractible = null;

        //Control display
        private Transform hintPanelRoot = null;
        private Transform hintPanelOrigParent = null;
        private Transform deathUIRoot = null;
        private TextMeshProUGUI controlsText = null;
        private bool controlsHidden = true;

        //Control related things
        private float accelTime = -1;
        private float decelTime = -1;
        private bool altitudeLock = false;

        //Helpful stuff
        public static List<MaskedPlayerEnemy> masked = new List<MaskedPlayerEnemy>();
        private static DunGen.RandomStream rand = null;

        /**
         * On awake, make and grab the light
         */
        private void Awake()
        {
            //If the instance already exists, abort!
            if(instance != null)
            {
                Destroy(this);
                return;
            }

            instance = this;

            //Set up the lights
            for (int i = 0; i < 4; i++)
            {
                //Determine the direction it should face
                Vector3 dir = new Vector3();
                switch(i)
                {
                    case 0:
                        dir = new Vector3(50f, 0f, 0f); 
                        break;
                    case 1:
                        dir = new Vector3(120f, 0f, 0f); 
                        break;
                    case 2:
                        dir = new Vector3(50f, 90f, 0f); 
                        break;
                    case 3:
                        dir = new Vector3(50f, -90f, 0f); 
                        break;
                }

                //Actually make everything
                GameObject lightObj = new GameObject("GhostLight" + i);
                Light light = lightObj.AddComponent<Light>();
                HDAdditionalLightData lightData = lightObj.AddComponent<HDAdditionalLightData>();
                lightObj.transform.eulerAngles = dir;
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                light.intensity = Poltergeist.Config.LightIntensity.Value;
                lightObj.hideFlags = HideFlags.DontSave;
                lightData.affectsVolumetric = false;
                lights[i] = light;
            }

            //Grab the camera and change the mask to include invisible enemies
            cam = GetComponent<Camera>();
            cam.cullingMask |= 1 << 23;

            DisableCam();
        }

        /**
         * Tells the spectator to parent to something when ghost mode is active
         */
        public void ParentTo(Transform parent)
        {
            ghostParent = parent;
            if(enabled && !Patches.vanillaMode)
                transform.parent = ghostParent;
        }

        /**
         * Enables the spectator camera
         */
        public void EnableCam()
        {
            if (!enabled)
            {
                enabled = true;

                //Activate the head
                head.isActive = true;
                head.ApplyRandomMat();

                //Move the camera
                if (!Patches.vanillaMode)
                {
                    transform.parent = ghostParent;
                    Transform oldCam = StartOfRound.Instance.activeCamera.transform;
                    transform.position = oldCam.position;
                    transform.rotation = oldCam.rotation;
                    Patches.camControllerActive = true;
                }

                //If we don't have them, need to grab certain objects
                if (hintPanelRoot == null)
                {
                    hintPanelRoot = HUDManager.Instance.tipsPanelAnimator.transform.parent;
                    hintPanelOrigParent = hintPanelRoot.parent;
                    deathUIRoot = HUDManager.Instance.SpectateBoxesContainer.transform.parent;

                    //Also, make the controls display guy
                    GameObject go = Instantiate(HUDManager.Instance.holdButtonToEndGameEarlyText.gameObject, deathUIRoot);
                    controlsText = go.GetComponent<TextMeshProUGUI>();
                    go.name = "PoltergeistControlsText";
                }

                //Move the hint panel to the death UI
                hintPanelRoot.parent = deathUIRoot;

                //Zero the power
                power = 0;

                //Enable ghost-only interactables
                IGhostOnlyInteractible.SetGhostActivation(true);

                //Set the controls text
                UpdateControlText();

                //Make the rng thing
                if(rand == null)
                    rand = new DunGen.RandomStream();
            }
        }

        /**
         * Disables the spectator camera
         */
        public void DisableCam()
        {
            if (enabled)
            {
                //Basics
                enabled = false;
                foreach (Light l in lights)
                    l.enabled = false;
                Patches.vanillaMode = Poltergeist.Config.DefaultToVanilla.Value;
                Patches.camControllerActive = false;
                altitudeLock = false;

                //Deactivate the head
                if (head != null)
                {
                    head.isActive = false;
                    head.Deactivate();
                }

                //If these aren't null, we moved them and need to put them back
                if (hintPanelRoot != null)
                {
                    hintPanelRoot.parent = hintPanelOrigParent;
                }

                //Disable ghost-only interactables
                IGhostOnlyInteractible.SetGhostActivation(false);

                //Hide the control display
                controlsHidden = true;
            }
        }

        /**
         * Updates the contents of the control text
         */
        private void UpdateControlText()
        {
            //If it's hidden, only show how to toggle it
            if(controlsHidden)
            {
                controlsText.text = "Show Poltergeist Controls; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.ToggleControlsKey) + "]";
                return;
            }

            //If it's not hidden, show everything
            string str = "Hide Poltergeist Controls; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.ToggleControlsKey) + "]\n";
            str += "Switch Spectate Mode; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.ToggleButton) + "]\n\n";

            //Most only shown if we're not in vanilla mode
            if (!Patches.vanillaMode)
            {
                str += "Increase Speed; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.AccelerateButton) + "]\n";
                str += "Decrease Speed; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.DecelerateButton) + "]\n";
                str += "Up; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.UpKey) + "]\n";
                str += "Down; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.DownKey) + "]\n";
                str += "Lock Altitude; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.LockKey) + "]\n\n";
                str += "Teleport to players; [1-9]\n";
                str += "Toggle Ghost Light; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.SwitchLightButton) + "]\n";
                if (Poltergeist.Config.EnableManifest.Value)
                    str += "Manifest; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.ManifestKey) + "] (Cost: "
                        + Poltergeist.Config.ManifestCost.Value + ")\n";
                if (Poltergeist.Config.EnableAudio.Value)
                    str += "Play Audio; [" + PoltergeistCustomInputs.GetKeyString(PoltergeistCustomInputs.instance.BarkKey) + "] (Cost: "
                        + Poltergeist.Config.BarkCost.Value + ")";
            }
            controlsText.text = str;
        }

        /**
         * When the left mouse is clicked, switch the light
         */
        private void SwitchLight(InputAction.CallbackContext context)
        {
            //Cancel if this isn't a "performed" action
            if (!context.performed || Patches.vanillaMode)
            {
                return;
            }

            //If in the right conditions, switch the light
            if (clientPlayer.isPlayerDead && !clientPlayer.isTypingChat && !clientPlayer.quickMenuManager.isMenuOpen)
            {
                foreach(Light l in lights)
                    l.enabled = !l.enabled;
            }
        }

        /**
         * Attempts to teleport to the specified player
         */
        public void TeleportToPlayer(PlayerControllerB player)
        {
            //Display errors depending on circumstance
            MethodInfo tipMethod = HUDManager.Instance.GetType().GetMethod("DisplaySpectatorTip", BindingFlags.NonPublic | BindingFlags.Instance);
            
            //Player is dead, check for body/masked
            if(player.isPlayerDead)
            {
                //If some registered masked is mimicking this player, go there
                MaskedPlayerEnemy targetMasked = null;
                foreach (MaskedPlayerEnemy enemy in masked)
                {
                    if (enemy.mimickingPlayer == player)
                    {
                        targetMasked = enemy;
                        break;
                    }
                }
                if (targetMasked != null)
                {
                    //If alive, move to the face
                    if(!targetMasked.isEnemyDead)
                    {
                        Transform target = targetMasked.transform.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/spine.004");
                        transform.position = target.position + (target.up * 0.2f);
                        transform.eulerAngles = new Vector3(target.eulerAngles.x, target.eulerAngles.y, 0);
                    }

                    //Otherwise, go over the body
                    else
                    {
                        transform.position = targetMasked.transform.position + Vector3.up;
                    }
                }

                //If the player has a corpse, move to it
                else if (player.deadBody != null && !player.deadBody.deactivated)
                {
                    //Move to the corpse
                    transform.position = player.deadBody.transform.position + Vector3.up;
                }

                //No corpse or masked, can't do anything
                else
                    HUDManager.Instance.DisplayTip("Can't Teleport", "Specified player is dead with no body!", true);
                return;
            }

            //Player is not connected, can't teleport
            if(!player.isPlayerControlled)
            {
                HUDManager.Instance.DisplayTip("Can't Teleport", "Specified player is not connected!", true);
                return;
            }

            //Otherwise, move the camera to that player
            transform.position = player.gameplayCamera.transform.position;
            transform.rotation = player.gameplayCamera.transform.rotation;

            //Apply the effects
            clientPlayer.spectatedPlayerScript = player;
            clientPlayer.SetSpectatedPlayerEffects(false);
        }

        /**
         * When the interact key is pressed, try to use the current ghost interactible
         */
        private void DoInteract(InputAction.CallbackContext context)
        {
            //Cancel if this isn't a "performed" action
            if (!context.performed || Patches.vanillaMode)
            {
                return;
            }

            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //If not null, use the interactible
            if (currentGhostInteractible != null)
            power -= currentGhostInteractible.Interact(clientPlayer.transform);
        }

        /**
         * When scrolling up is done, set the camera up to change speed
         */
        private void Accelerate(InputAction.CallbackContext context)
        {
            if (Patches.vanillaMode)
                return;
            accelTime = Time.time + 0.3f;
            decelTime = -1;
        }

        /**
         * When scrolling down is done, set the camera up to change speed
         */
        private void Decelerate(InputAction.CallbackContext context)
        {
            if (Patches.vanillaMode)
                return;
            decelTime = Time.time + 0.3f;
            accelTime = -1;
        }

        /**
         * Switches modes between the vanilla and modded spectate
         */
        private void SwitchModes(InputAction.CallbackContext context)
        {
            //Only do it if performing
            if (!context.performed)
                return;

            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //Change the flag
            Patches.vanillaMode = !Patches.vanillaMode;

            //Handle switching to vanilla
            if(Patches.vanillaMode)
            {
                foreach (Light l in lights)
                    l.enabled = false;
                clientPlayer.spectatedPlayerScript = null;
                currentGhostInteractible = null;
                clientPlayer.cursorTip.text = "";
                StartOfRound.Instance.SetSpectateCameraToGameOverMode(Patches.shouldGameOver, clientPlayer);
                Patches.camControllerActive = false;

                //Set the controls text
                UpdateControlText();
            }

            //Handle switching to modded
            else
            {
                transform.parent = ghostParent;
                Patches.camControllerActive = true;

                //Set the controls text
                UpdateControlText();
            }
        }

        /**
         * Lock the player's altitude for the standard movement
         */
        private void LockAltitude(InputAction.CallbackContext context)
        {
            //Only do it if performing and not in vanilla mode
            if (!context.performed || Patches.vanillaMode)
                return;

            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //Change the flag
            altitudeLock = !altitudeLock;
        }

        /**
         * Manifest the player's head in the living world
         */
        private void ManifestHead(InputAction.CallbackContext context)
        {
            //Only do it if performing and not in vanilla mode
            if (!context.performed || Patches.vanillaMode)
                return;

            if (!Poltergeist.Config.EnableManifest.Value)
                return;

            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //Call it on the head if there's enough power (and it's not already being done)
            if (power >= Poltergeist.Config.ManifestCost.Value && !head.IsManifesting())
            {
                head.ManifestServerRpc();
                power -= Poltergeist.Config.ManifestCost.Value;
            }
        }

        /**
         * Play bark audio
         */
        private void Bark(InputAction.CallbackContext context)
        {
            //Only do it if performing and not in vanilla mode
            if (!context.performed || Patches.vanillaMode)
                return;

            if (!Poltergeist.Config.EnableAudio.Value)
                return;
            
            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //Call it on the head if there's enough power (and it's not already being done)
            if (power >= Poltergeist.Config.BarkCost.Value && !head.IsBarking())
            {
                head.BarkServerRpc(rand.Next());
                power -= Poltergeist.Config.BarkCost.Value;
            }
        }

        /**
         * Changes the visibility of the controls on the HUD
         */
        private void ToggleControlVis(InputAction.CallbackContext context)
        {
            //Only do it if performing
            if (!context.performed)
                return;

            //Don't do things if paused
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen)
                return;

            //Swap the visibility and update the text
            controlsHidden = !controlsHidden;
            UpdateControlText();
        }

        /**
         * Add and remove the different control listeners as needed
         */
        private void OnEnable()
        {
            PoltergeistCustomInputs.instance.SwitchLightButton.performed += SwitchLight;
            PoltergeistCustomInputs.instance.InteractButton.performed += DoInteract;
            PoltergeistCustomInputs.instance.AccelerateButton.performed += Accelerate;
            PoltergeistCustomInputs.instance.DecelerateButton.performed += Decelerate;
            PoltergeistCustomInputs.instance.ToggleButton.performed += SwitchModes;
            PoltergeistCustomInputs.instance.LockKey.performed += LockAltitude;
            if (Poltergeist.Config.EnableManifest.Value)
                PoltergeistCustomInputs.instance.ManifestKey.performed += ManifestHead;
            if (Poltergeist.Config.EnableAudio.Value)
                PoltergeistCustomInputs.instance.BarkKey.performed += Bark;
            PoltergeistCustomInputs.instance.ToggleControlsKey.performed += ToggleControlVis;

        }
        private void OnDisable()
        {
            PoltergeistCustomInputs.instance.SwitchLightButton.performed -= SwitchLight;
            PoltergeistCustomInputs.instance.InteractButton.performed -= DoInteract;
            PoltergeistCustomInputs.instance.AccelerateButton.performed -= Accelerate;
            PoltergeistCustomInputs.instance.DecelerateButton.performed -= Decelerate;
            PoltergeistCustomInputs.instance.ToggleButton.performed -= SwitchModes;
            PoltergeistCustomInputs.instance.LockKey.performed -= LockAltitude;
            if (Poltergeist.Config.EnableManifest.Value)
                PoltergeistCustomInputs.instance.ManifestKey.performed -= ManifestHead;
            if (Poltergeist.Config.EnableAudio.Value)
                PoltergeistCustomInputs.instance.BarkKey.performed -= Bark;
            PoltergeistCustomInputs.instance.ToggleControlsKey.performed -= ToggleControlVis;
        }

        /**
         * When destroyed, need to manually destroy the ghost light
         */
        private void OnDestroy()
        {
            foreach (Light l in lights)
            {
                if (l != null)
                    DestroyImmediate(l.gameObject);
            }
        }

        /**
         * Updates the controls text
         */
        private void PositionControlText()
        {
            //Figure out where to actually put the text
            Transform tf = controlsText.transform;
            Bounds bounds = HUDManager.Instance.holdButtonToEndGameEarlyVotesText.textBounds;

            //Need to account for the votes text being empty
            if(bounds.m_Extents.y < 0)
                tf.position = new Vector3(tf.position.x, HUDManager.Instance.holdButtonToEndGameEarlyVotesText.transform.position.y, tf.position.z);
            else
                tf.localPosition = new Vector3(tf.localPosition.x, (bounds.min + HUDManager.Instance.holdButtonToEndGameEarlyVotesText.transform.localPosition).y
                    - (controlsText.bounds.extents.y + 22), 
                    tf.localPosition.z);
        }

        /**
         * Just before rendering, handle camera input
         */
        private void LateUpdate ()
        {
            //Need to wait for the player controller to be registered
            if(clientPlayer == null)
            {
                clientPlayer = StartOfRound.Instance.localPlayerController;
                if (clientPlayer == null)
                    return;
            }

            //Calculate the max power based on # of connected players dead
            float connected = -1 * Poltergeist.Config.AliveForMax.Value; //Negative because we want max power at AliveForMax living players
            float dead = 0;
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) //First, count them
            {
                if (player.isPlayerDead)
                {
                    connected++;
                    dead++;
                }
                else if (player.isPlayerControlled)
                    connected++;
            }
            dead = Mathf.Min(dead, connected); //Make sure we don't go above max power
            if (connected <= 0) //If few enough players connected, always max power
                maxPower = Poltergeist.Config.MaxPower.Value;
            else
                maxPower = (dead / connected) * Poltergeist.Config.MaxPower.Value;

            //If dead, player should always be gaining power
            power = Mathf.Min(maxPower, power + (Poltergeist.Config.PowerRegen.Value * Time.deltaTime));

            //If the player is in the menu (or we're in vanilla mode), don't do update stuff
            if (clientPlayer.isTypingChat || clientPlayer.quickMenuManager.isMenuOpen || Patches.vanillaMode)
            {
                currentGhostInteractible = null;

                //Should still move the ghost head to the correct position
                if (head != null && head.isActive)
                {
                    head.transform.position = transform.position;
                    head.transform.rotation = transform.rotation;
                }

                return;
            }

            //Take raw inputs
            Vector2 moveInput = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
            Vector2 lookInput = clientPlayer.playerActions.Movement.Look.ReadValue<Vector2>() * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;
            if (!IngamePlayerSettings.Instance.settings.invertYAxis)
            {
                lookInput.y *= -1f;
            }
            bool sprint = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>() > 0.3f;

            //Rotate the camera
            transform.Rotate(0, lookInput.x, 0, Space.World);

            //Need to correct the rotation to not allow looking too high or low
            float newX = (transform.eulerAngles.x % 360) + lookInput.y;
            if (newX < 270 && newX > 90)
            {
                if (270 - newX < newX - 90)
                    transform.eulerAngles = new Vector3(270, transform.eulerAngles.y, 0);
                else
                    transform.eulerAngles = new Vector3(90, transform.eulerAngles.y, 0);
            }
            else
                transform.eulerAngles = new Vector3(newX, transform.eulerAngles.y, 0);

            //Move the camera
            float curMoveSpeed = camMoveSpeed;
            if (sprint)
                curMoveSpeed *= 5;
            Vector3 rightMove = transform.right * moveInput.x * curMoveSpeed * Time.deltaTime;
            Vector3 forwardMove;

            //Normally, just move forward
            if (!altitudeLock)
            {
                forwardMove = transform.forward * moveInput.y * curMoveSpeed * Time.deltaTime;
            }

            //If their altitude is locked, need special logic
            else
            {
                //If they're facing straight down, actually take the up vector
                if (transform.forward.y < -0.99)
                    forwardMove = transform.up;

                //If they're facing straight up, actually take the down vector
                else if(transform.forward.y > 0.99)
                    forwardMove = transform.up * -1;

                //Otherwise, take the forward vector
                else
                    forwardMove = transform.forward;

                //Trim the y component to prevent vertical motion
                forwardMove.y = 0;
                forwardMove = forwardMove.normalized;
                forwardMove = forwardMove * moveInput.y * curMoveSpeed * Time.deltaTime;
            }
            transform.position += rightMove + forwardMove;

            //Handle the vertical controls
            float vertMotion = (PoltergeistCustomInputs.instance.UpKey.ReadValue<float>() - PoltergeistCustomInputs.instance.DownKey.ReadValue<float>())
                * curMoveSpeed * Time.deltaTime;
            transform.position += Vector3.up * vertMotion;

            //Actually do the speed change
            if(accelTime > Time.time)
            {
                camMoveSpeed += Time.deltaTime * camMoveSpeed;
                camMoveSpeed = Mathf.Clamp(camMoveSpeed, 0, 100);
            }
            else if(decelTime > Time.time)
            {
                camMoveSpeed -= Time.deltaTime * camMoveSpeed;
                camMoveSpeed = Mathf.Clamp(camMoveSpeed, 0, 100);
            }

            //Display the current power
            HUDManager.Instance.spectatingPlayerText.text = "Power: " + power.ToString("F0") + " / " + maxPower.ToString("F0");

            //Lets the player teleport to other players
            int teleIndex = -1;
            for(Key i = Key.Digit1; i <= Key.Digit0; i++)
            {
                if (Keyboard.current[i].wasPressedThisFrame) 
                {
                    teleIndex = (i - Key.Digit1);
                    break;
                }
            }
            if(teleIndex != -1)
            {
                PlayerControllerB[] playerList = StartOfRound.Instance.allPlayerScripts;
                if(teleIndex >= playerList.Length)
                    HUDManager.Instance.DisplayTip("Cannot Teleport", "Specified player index is invalid!", isWarning: true);
                else
                    TeleportToPlayer(playerList[teleIndex]);
            }

            //Lets the player detect ghost interactibles
            RaycastHit hit;
            currentGhostInteractible = null;
            clientPlayer.cursorTip.text = "";
            if (Physics.Raycast(transform.position, transform.forward, out hit, 5, 0b10000000001101000000) && hit.collider.gameObject.layer != 8)
            {
                //Check if it is a naive interactible, or if a child is a networked one
                IGhostInteractible ghostInteractible = hit.collider.gameObject.GetComponent<NaiveInteractible>();
                if(ghostInteractible == null)
                {
                    foreach(Transform tf in hit.collider.transform)
                    {
                        ghostInteractible = tf.GetComponent<NetworkedInteractible>();
                        if (ghostInteractible != null)
                            break;
                    }
                }

                //If we found one, select it
                if (ghostInteractible != null)
                {
                    currentGhostInteractible = ghostInteractible;
                    clientPlayer.cursorTip.text = ghostInteractible.GetTipText();
                }
            }

            //Move the ghost head to the player
            if(head != null && head.isActive)
            {
                head.transform.position = transform.position;
                head.transform.rotation = transform.rotation;
            }

            //Update the controls text
            PositionControlText();

            //Make all of the usernames visible
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != clientPlayer && player.isPlayerControlled)
                {
                    player.ShowNameBillboard();
                    player.usernameBillboard.LookAt(transform.position);
                }
            }
        }
    }
}
