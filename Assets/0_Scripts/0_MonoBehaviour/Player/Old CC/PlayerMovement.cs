﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#region ----[ PUBLIC ENUMS ]----

#endregion

#region ----[ REQUIRECOMPONENT ]----
[RequireComponent(typeof(PlayerCombatNew))]
[RequireComponent(typeof(PlayerAnimation_01))]
[RequireComponent(typeof(PlayerWeapons))]
[RequireComponent(typeof(PlayerHook))]
#endregion
public class PlayerMovement : MonoBehaviour
{
    #region ----[ VARIABLES FOR DESIGNERS ]----
    [Header(" --- Referencias --- ")]
    public bool disableAllDebugs;
    //public PlayerCombat myPlayerCombat;
    public PlayerCombatNew myPlayerCombatNew;
    public PlayerWeapons myPlayerWeap;
    public PlayerPickups myPlayerPickups;
    //public PlayerAnimation myPlayerAnimation;
    public PlayerAnimation_01 myPlayerAnimation_01;
    public PlayerHook myPlayerHook;
    public Controller3D controller;
    public PlayerBody myPlayerBody;
    public PlayerObjectDetection myPlayerObjectDetection;
    public PlayerModel myPlayerModel;
    public PlayerVFX myPlayerVFX;
    public PassiveCollisions myPassiveCollisions;

    public bool startBeingHitAnimation = false;

    public GameControllerBase gC;
    public CameraControllerUmi myCamera;
    public Transform cameraFollow;
    public Transform rotateObj;


    //CONTROLES
    public PlayerActions Actions { get; set; }

    //[Header("Body and body color")]

    //VARIABLES DE MOVIMIENTO

    [Header(" --- ROTATION --- ")]
    [Tooltip("Min angle needed in a turn to activate the instant rotation of the character (and usually the hardSteer mechanic too)")]
    [Range(0, 180)]
    public float instantRotationMinAngle = 120;
    float rotationRestrictedPercentage = 1;

    [Header("--- SPEED ---")]
    public float maxFallSpeed = 100;
    public float maxMoveSpeed = 10;
    float maxAttackingMoveSpeed = 10;
    [Tooltip("Maximum speed that you can travel at horizontally when hit by someone")]
    public float maxKnockbackSpeed = 300f;
    public float maxAimingSpeed = 5f;
    public float maxHookingSpeed = 3.5f;
    public float maxGrapplingSpeed = 3.5f;
    public float maxSpeedInWater = 5f;
    public float maxVerticalSpeedInWater = 3f;

    [Header(" - IMPULSE - ")]
    [Range(0, 1)]
    public float airImpulsePercentage = 0.5f;
    [Range(0, 180)]
    public float maxImpulseAngle = 60;

    [Header("--- BOOST ---")]
    public float boostSpeed = 20f;
    public float boostCapacity = 1f;
    [Tooltip("1 -> 1 capacity per second")]
    public float boostDepletingSpeed = 1f;
    [Tooltip("1 -> 1 capacity per second")]
    public float boostRechargingSpeed = 0.3f;
    [HideInInspector]
    public float boostCurrentFuel = 1;
    public float boostCDMaxTime = 0.3f;
    float boostCDTime = 0;
    bool boostCDStarted = false;
    [Tooltip("0-> none; 1-> All the fuel")]
    [Range(0, 1)]
    public float boostFuelLostOnStart = 0.15f;
    [Tooltip("0-> none; 1-> All the fuel")]
    [Range(0, 1)]
    public float boostMinFuelNeeded = 0.2f;
    [HideInInspector]
    public bool boostReady
    {
        get
        {
            return ((boostCurrentFuel > boostCapacity * boostMinFuelNeeded) && !boostCDStarted && !haveFlag && !inWater);
        }
    }
    Vector3 boostDir;

    [Header(" --- ACCELERATIONS --- ")]
    public float initialAcc = 30;
    public float airInitialAcc = 30;
    public float breakAcc = -30;
    public float airBreakAcc = -5;
    public float hardSteerAcc = -60;
    public float airHardSteerAcc = -10;
    public float movingAcc = 2.0f;
    public float airMovingAcc = 0.5f;
    [Tooltip("Acceleration used when breaking from a boost.")]
    public float hardBreakAcc = -120f;
    [Tooltip("Breaking negative acceleration that is used under the effects of a knockback (stunned). Value is clamped to not be higher than breakAcc.")]
    public float knockbackBreakAcc = -30f;
    //public float breakAccOnHit = -2.0f;
    [HideInInspector]
    public float gravity;
    float currentGravity;

    [Header(" --- JUMP --- ")]
    public float jumpHeight = 4f;
    public float jumpApexTime = 0.4f;
    float jumpVelocity;
    float timePressingJump = 0.0f;
    float maxTimePressingJump;
    [Tooltip("How fast the 'stop jump early' stops in the air. This value is multiplied by the gravity and then applied to the vertical speed.")]
    public float breakJumpForce = 2.0f;
    [Tooltip("During how much part of the jump (in time to reach the apex) is the player able to stop the jump. 1 is equals to the whole jump, and 0.5 is equals the half of the jump time.")]
    public float pressingJumpActiveProportion = 0.7f;
    public float maxTimeJumpInsurance = 0.2f;
    float timeJumpInsurance = 0;
    bool jumpInsurance;
    bool jumpingFromWater;

    [Header(" --- WALLJUMP --- ")]
    public float wallJumpVelocity = 10f;
    public float wallJumpSlidingAcc = -2.5f;
    [Range(0, 1)]
    public float wallJumpMinHeightPercent = 0.55f;
    public float stopWallMaxTime = 0.5f;
    float stopWallTime = 0;
    [HideInInspector]
    public bool wallJumping = false;
    Vector3 anchorPoint;
    Vector3 wallNormal;
    [Tooltip("Vertical angle in which the player wall-jumps.")]
    [Range(0, 89)]
    public float wallJumpAngle = 30;
    [Tooltip("Minimum horizontal angle in which the player wall-jumps. This number ranges from 0 to 90. 0 --> parallel to the wall; 90 --> perpendicular to the wall")]
    [Range(0, 89)]
    public float wallJumpMinHorizAngle = 30;


    [Header(" --- CHARGE JUMP ---")]
    [Range(0, 4)]
    public float chargeJumpFallenHeightMultiplier = 1f;
    [Range(0, 2)]
    public float chargeJumpReleaseTimeBeforeLand = 0.5f;
    [Range(0, 2)]
    public float chargeJumpReleaseTimeAfterLand = 0.5f;
    public float chargeJumpMaxJumpHeight = 100;
    [Range(0, 1)]
    public float chargeJumpMinPercentageNeeded = 0.3f;
    float chargeJumpMaxHeight = 80;//clouds height
    bool chargeJumpChargingJump = false;
    float chargeJumpLastApexHeight;
    float chargeJumpCurrentJumpMaxHeight;
    float chargeJumpCurrentReleaseTime = 0;
    bool chargeJumpButtonReleased;
    bool chargeJumpLanded = false;
    float chargeJumpLandedTime = 0;
    float chargeJumpChargingStartHeight;
    bool failedJump = false;
    bool isChargeJump//for StartJump only
    {
        get
        {
            bool result = false;
            result = controller.collisions.below && chargeJumpChargingJump && isFloorChargeJumpable && (jumpSt == VerticalMovementState.Falling || jumpSt == VerticalMovementState.None);
            if (!disableAllDebugs) Debug.LogWarning("isChargeJump = " + result);
            return result;
        }
    }
    bool isFloorChargeJumpable
    {
        get
        {
            StageScript stageScript = controller.collisions.floor != null ? controller.collisions.floor.GetComponent<StageScript>() : null;
            return stageScript != null && stageScript.chargeJumpable;
        }
    }


    [Header(" --- BOUNCE JUMP ---")]
    [Range(0, 1)]
    public float bounceJumpMultiplier = 0.5f;
    public float bounceJumpMinHeight = 2.5f;
    bool isBounceJump//for StartJump only
    {
        get
        {
            bool result = false;
            float totalFallenHeight = chargeJumpLastApexHeight - transform.position.y;
            float auxBounceJumpCurrentMaxHeight = totalFallenHeight * bounceJumpMultiplier;
            result = controller.collisions.below && isFloorBounceJumpable && auxBounceJumpCurrentMaxHeight >= bounceJumpMinHeight 
                && jumpSt == VerticalMovementState.Falling;
            if (!disableAllDebugs) Debug.LogWarning("isBounceJump = " + result + "; controller.collisions.floor = " + controller.collisions.floor+ "; controller.collisions.below = "+ controller.collisions.below 
                + "; auxBounceJumpCurrentMaxHeight >= bounceJumpMinHeight = " + (auxBounceJumpCurrentMaxHeight >= bounceJumpMinHeight));
            return result;
        }
    }
    bool isFloorBounceJumpable
    {
        get
        {
            StageScript stageScript = controller.collisions.floor != null ? controller.collisions.floor.GetComponent<StageScript>() : null;
            if (stageScript != null) Debug.Log("stageScript = " + stageScript + "; stageScript.bounceJumpable = "
     + stageScript.bounceJumpable);
            return (stageScript == null || (stageScript != null && stageScript.bounceJumpable));
        }
    }


    [Header("TEAM COLORS")]
    public Gradient StinrayGradient;
    public Gradient OktiromeGradient;

    [Header("----- ONLINE VARIABLES ----")]
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    #endregion

    #region ----[ PROPERTIES ]----
    //Referencias que no se asignan en el inspector
    [HideInInspector]
    public Camera myUICamera;
    [HideInInspector]
    public PlayerHUD myPlayerHUD;

    ////ONLINE
    //[HideInInspector]
    //public bool online = false;

    //Eloy: FOR ONLINE
    [HideInInspector]
    public int playerNumber; //from 0 to maxPlayers

    //INFO GENERAL
    [HideInInspector]
    public Team team = Team.A;
    [HideInInspector]
    public PlayerSpawnInfo mySpawnInfo;
    //[Header("Body Mass")]
    //[Tooltip("Body Mass Index. 1 is for normal body mass.")]
    [HideInInspector] public float bodyMass;

    //BOOL PARA PERMITIR O BLOQUEAR INPUTS
    [HideInInspector]
    public bool noInput = false;

    //MOVIMIENTO
    [HideInInspector]
    public MoveState moveSt = MoveState.NotMoving;
    [HideInInspector]
    public Vector3 currentVel;
    Vector3 oldCurrentVel;
    [HideInInspector]
    public float currentSpeed = 0;
    //[HideInInspector]
    //public Vector3 currentMovDir;//= a currentInputDir??? 
    [HideInInspector]
    public Vector3 currentInputDir;
    [HideInInspector]
    public Vector3 currentFacingDir = Vector3.forward;
    [HideInInspector]
    public float facingAngle = 0;
    [HideInInspector]
    public Vector3 currentCamFacingDir = Vector3.zero;
    Vector3 hardSteerDir = Vector3.zero;
    float hardSteerAngleDiff = 0;
    bool hardSteerOn = false;
    bool hardSteerStarted = false;

    //IMPULSE
    [HideInInspector]
    public ImpulseInfo currentImpulse;
    bool impulseStarted = false;
    float impulseTime = 0;
    bool impulseDone = false;



    //SALTO
    [HideInInspector]
    public VerticalMovementState jumpSt = VerticalMovementState.None;
    [HideInInspector]
    public bool wallJumpAnim = false;

    //FLAG
    [HideInInspector]
    public bool haveFlag = false;
    [HideInInspector]
    public Flag flag = null;

    //WATER
    [HideInInspector]
    public bool inWater = false;

    #endregion

    #region ----[ VARIABLES ]----  
    int frameCounter = 0;

    //Movement
    [HideInInspector]
    public float currentMaxMoveSpeed; // is the max speed from which we aply the joystick sensitivity value
    float finalMaxMoveSpeed = 10.0f; // its the final max speed, after the joyjoystick sensitivity value
    //bool hardSteer = false;

    //JOYSTICK INPUT
    float joystickAngle;
    float deadzone = 0.2f;
    float lastJoystickSens = 0;
    float joystickSens = 0;

    //WALL JUMP
    bool firstWallJumpDone = false;
    float wallJumpCurrentWallAngle = 0;
    GameObject wallJumpCurrentWall = null;
    float lastWallAngle = -500;
    GameObject lastWall = null;
    float wallJumpRadius;
    float walJumpConeHeight = 1;
    Axis wallJumpRaycastAxis = Axis.none;
    int wallJumpCheckRaysRows = 5;
    int wallJumpCheckRaysColumns = 5;
    float wallJumpCheckRaysRowsSpacing;
    float wallJumpCheckRaysColumnsSpacing;
    LayerMask auxLM;//LM = Layer Mask

    //Attack effect suffering
    EffectType sufferingEffect;
    float effectTime = 0;
    float effectMaxTime = 0;
    bool knockbackDone;
    Vector3 knockback;
    bool sufferingStunLikeEffect
    {
        get
        {
            return (sufferingEffect == EffectType.softStun);//|| sufferingEffect == EffectType.stun || sufferingEffect == EffectType.knockdown
        }
    }

    //FIXED JUMPS (Como el trampolín)
    bool fixedJumping;
    bool fixedJumpDone;
    float noMoveMaxTime;
    float noMoveTime;

    //HOOK
    [HideInInspector]
    public bool hooked = false;
    bool hooking = false;
    [HideInInspector] public bool aimingAndTouchedGroundOnce = false;
    [HideInInspector] public bool hookingAndTouchedGroundOnce = false;


    ////GRAPPLE
    //bool grappling = false;


    //WATER
    [HideInInspector]
    public bool jumpedOutOfWater = true;

    BufferedInput[] inputsBuffer;//Eloy: para Juan: esta variable iría aquí? o a "Variables", JUAN: A variables mejor
    #endregion

    #region ----[ MONOBEHAVIOUR FUNCTIONS ]----

    #region AWAKE

    //public void Awake()
    //{
    //    online = PhotonNetwork.IsConnected;
    //}

    public void KonoAwake(bool isMyCharacter = false)
    {
        //if (!online || (online && isMyCharacter))
        //{
            if (breakAcc != knockbackBreakAcc) Debug.LogError("The breakAcceleration and the KnockbackAcceleration should be the same!");
            maxMoveSpeed = 10;
            currentSpeed = 0;
            boostCurrentFuel = boostCapacity;
            noInput = false;
            lastWallAngle = 0;
            SetupInputsBuffer();
            myPlayerHook.myCameraBase = myCamera;
            hardSteerAcc = Mathf.Clamp(hardSteerAcc, hardSteerAcc, breakAcc);
            airHardSteerAcc = Mathf.Clamp(airHardSteerAcc, airHardSteerAcc, airBreakAcc);

            //PLAYER MODEL
            SwitchTeam(team);

            //WALLJUMP
            wallJumpCheckRaysRows = 5;
            wallJumpCheckRaysColumns = 5;
            wallJumpCheckRaysRowsSpacing = (controller.coll.bounds.size.y * wallJumpMinHeightPercent) / (wallJumpCheckRaysRows - 1);
            wallJumpCheckRaysColumnsSpacing = controller.coll.bounds.size.x / (wallJumpCheckRaysColumns - 1);
            auxLM = LayerMask.GetMask("Stage");

            PlayerAwakes();

            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            //if (online)
            //{
            //    if (photonV)
            //    {
            //        PlayerMovement.LocalPlayerInstance = this.gameObject;
            //    }
            //}
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            //DontDestroyOnLoad(this.gameObject);
        //}
        //else
        //{
        //    //PLAYER MODEL
        //    SwitchTeam(team);
        //}


    }

    //todos los konoAwakes
    void PlayerAwakes()
    {
        myPlayerHUD.KonoAwake();
        myPlayerCombatNew.KonoAwake();
        //myPlayerAnimation.KonoAwake();
        myPlayerAnimation_01.KonoAwake();
        myPlayerHook.KonoAwake();
        myPlayerWeap.KonoAwake();
        myPlayerBody.KonoAwake();
        myPlayerVFX.KonoAwake();
    }
    #endregion

    #region START
    public void KonoStart()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpApexTime, 2);
        currentGravity = gravity;
        jumpVelocity = Mathf.Abs(gravity * jumpApexTime);
        maxTimePressingJump = jumpApexTime * pressingJumpActiveProportion;
        wallJumpRadius = Mathf.Tan(wallJumpAngle * Mathf.Deg2Rad) * walJumpConeHeight;
        print("wallJumpRaduis = " + wallJumpRadius + "; tan(wallJumpAngle)= " + Mathf.Tan(wallJumpAngle * Mathf.Deg2Rad));
        wallJumpMinHorizAngle = Mathf.Clamp(wallJumpMinHorizAngle, 0, 90);
        print("Gravity = " + gravity + "; Jump Velocity = " + jumpVelocity);

        finalMaxMoveSpeed = currentMaxMoveSpeed = maxMoveSpeed;
        knockbackBreakAcc = Mathf.Clamp(knockbackBreakAcc, -float.MaxValue, breakAcc);//menos de break Acc lo haría ver raro

        ShowFlagFlow();
        //EquipWeaponAtStart();

        PlayerStarts();
    }
    private void PlayerStarts()
    {
        myPlayerCombatNew.KonoStart();
        myPlayerHUD.KonoStart();
        myPlayerWeap.KonoStart();
        myPlayerVFX.KonoStart();
    }

    #endregion

    #region UPDATE
    public void KonoUpdate()
    {
        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    Debug.Log("I'm the owner of " + this.name + " ? " + base.photonView.IsMine);
        //}
        ResetMovementVariables();
        //Debug.LogError("CONTROLLER 3D: collisions.below = " + controller.collisions.below + "; lastBelow = " + controller.collisions.lastBelow+ "; jumpSt = " + jumpSt);
        //if (controller.collisions.below && !controller.collisions.lastBelow)
        //{
        //    Debug.LogError("LANDING");
        //}
        //Debug.Log("Mis acciones son " + Actions);
        if (!disableAllDebugs && currentSpeed != 0) Debug.LogWarning("CurrentVel 0= " + currentVel.ToString("F6") + "; currentSpeed =" + currentSpeed.ToString("F4"));
        if (Actions.Start.WasPressed) gC.PauseGame(Actions);

        if ((controller.collisions.above || controller.collisions.below) && !hooked)
        {
            currentVel.y = 0;
            if (controller.collisions.above) StopJump();
        }
        //print("FRAME NUMBER " + frameCounter);
        frameCounter++;
        UpdateFlagLightBeam();
        ProcessInputsBuffer();

        //print("CurrentVel 1= " + currentVel.ToString("F6"));
        HorizontalMovement();
        //print("CurrentVel 2= " + currentVel.ToString("F6"));
        //print("vel = " + currentVel.ToString("F4"));
        UpdateFacingDir();
        VerticalMovement();
        //print("vel = " + currentVel.ToString("F4"));

        ProcessWallJump();//IMPORTANTE QUE VAYA ANTES DE LLAMAR A "MOVE"

        //Debug.Log("currentVel = " + currentVel + "; Time.deltaTime = " + Time.deltaTime + "; currentVel * Time.deltaTime = " + (currentVel * Time.deltaTime) + "; Time.fixedDeltaTime = " + Time.fixedDeltaTime);
        ////controller.Move(currentVel * Time.deltaTime);
        //print("CurrentVel 3= " + currentVel.ToString("F6"));

        myPlayerCombatNew.KonoUpdate();
        ////controller.collisions.ResetAround();

        //myPlayerAnimation.KonoUpdate();
        ////myPlayerAnimation_01.KonoUpdate();
        myPlayerWeap.KonoUpdate();
        myPlayerHook.KonoUpdate();
    }

    public void KonoFixedUpdate()
    {
    }

    public void KonoLateUpdate()
    {
        if(Time.deltaTime>0)
        controller.Move(currentVel * Time.deltaTime);
        //myPassiveCollisions.PassiveMove();
        controller.collisions.ResetAround();
        myPlayerAnimation_01.KonoUpdate();
        if (!disableAllDebugs && currentSpeed != 0) Debug.LogWarning("CurrentVel End = " + currentVel.ToString("F6") + "; currentVel.normalized = " + currentVel.normalized.ToString("F6") +
       "; currentSpeed =" + currentSpeed.ToString("F4") + "; Player position = " + transform.position.ToString("F6"));
    }
    #endregion

    #endregion

    #region ----[ CLASS FUNCTIONS ]----

    void SwitchTeam(Team team)
    {
        //myPlayerModel.SwitchTeam(team);
        TrailRenderer dashTR = myPlayerVFX.GetEffectGO(PlayerVFXType.DashTrail).GetComponent<TrailRenderer>();
        dashTR.colorGradient = team == Team.A ? StinrayGradient : OktiromeGradient;
        //dashTR.endColor = team == Team.A ? StinrayColors [1]: OktiromeColors[1];
    }

    #region INPUTS BUFFERING

    void SetupInputsBuffer()
    {
        inputsBuffer = new BufferedInput[gC.allBufferedInputs.Length];
        for (int i = 0; i < gC.allBufferedInputs.Length; i++)
        {
            inputsBuffer[i] = new BufferedInput(gC.allBufferedInputs[i]);
        }
    }

    void ProcessInputsBuffer()
    {
        for (int i = 0; i < inputsBuffer.Length; i++)
        {
            if (inputsBuffer[i].buffering)
            {
                switch (inputsBuffer[i].input.inputType)
                {
                    case PlayerInput.Jump:
                        if (StartJump(true))
                        {
                            inputsBuffer[i].StopBuffering();
                        }
                        break;
                    case PlayerInput.WallJump:
                        if (StartWallJump(true))
                        {
                            inputsBuffer[i].StopBuffering();
                        }
                        break;
                    case PlayerInput.StartChargingChargeJump:
                        if (StartChargingChargeJump(true))
                        {
                            inputsBuffer[i].StopBuffering();
                        }
                        break;
                    case PlayerInput.Autocombo:
                        if (!disableAllDebugs) Debug.Log("Trying to input autocombo from buffer... Time left = " + inputsBuffer[i].time);
                        if (myPlayerCombatNew.StartOrContinueAutocombo(true))
                        {
                            inputsBuffer[i].StopBuffering();
                        }
                        break;

                }
                inputsBuffer[i].ProcessTime();
            }
        }
    }

    public void BufferInput(PlayerInput _inputType)
    {
        bool found = false;
        for (int i = 0; i < inputsBuffer.Length && !found; i++)
        {
            if (inputsBuffer[i].input.inputType == _inputType)
            {
                inputsBuffer[i].StartBuffering();
                found = true;
            }
        }
        if (!found)
        {
            Debug.LogError("Error: Impossible to buffer the input " + _inputType + " because there is no BufferedInput with that type in the inputsBuffered array.");
        }
    }

    public void StopBufferedInput(PlayerInput _inputType)
    {
        for (int i = 0; i < inputsBuffer.Length; i++)
        {
            if (inputsBuffer[i].input.inputType == _inputType)
            {
                inputsBuffer[i].StopBuffering();
            }
        }
    }

    #endregion

    #region MOVEMENT -----------------------------------------------
    public void SetVelocity(Vector3 vel)
    {
        currentVel = vel;
        Vector3 horVel = new Vector3(currentVel.x, 0, currentVel.z);
        currentSpeed = horVel.magnitude;
    }

    void ResetMovementVariables()
    {
        //hardSteerOn = false;
        hardSteerAngleDiff = 0;
        currentInputDir = Vector3.zero;
        oldCurrentVel = new Vector3(currentVel.x, 0, currentVel.z);
    }

    public void CalculateMoveDir()
    {
        if (!noInput)
        {
            float horiz = Actions.LeftJoystick.X;//Input.GetAxisRaw(contName + "H");
            float vert = Actions.LeftJoystick.Y;//-Input.GetAxisRaw(contName + "V");
                                                // Check that they're not BOTH zero - otherwise dir would reset because the joystick is neutral.
                                                //if (horiz != 0 || vert != 0)Debug.Log("Actions.LeftJoystick.X = "+ Actions.LeftJoystick.X+ "Actions.LeftJoystick.Y" + Actions.LeftJoystick.Y);
            Vector3 temp = new Vector3(horiz, 0, vert);
            lastJoystickSens = joystickSens;
            joystickSens = temp.magnitude;
            //print("temp.magnitude = " + temp.magnitude);
            if (temp.magnitude >= deadzone)
            {
                joystickSens = joystickSens >= 0.88f ? 1 : joystickSens;//Eloy: esto evita un "bug" por el que al apretar el joystick 
                                                                        //contra las esquinas no da un valor total de 1, sino de 0.9 o así
                moveSt = MoveState.Moving;
                currentInputDir = temp;
                currentInputDir.Normalize();
                switch (myCamera.camMode)
                {
                    case CameraMode.Fixed:
                        currentInputDir = RotateVector(-facingAngle, temp);
                        break;
                    case CameraMode.Shoulder:
                        currentInputDir = RotateVector(-facingAngle, temp);
                        break;
                    case CameraMode.Free:
                        Vector3 camDir = (transform.position - myCamera.transform.GetChild(0).position).normalized;
                        camDir.y = 0;
                        // ANGLE OF JOYSTICK
                        joystickAngle = Mathf.Acos(((0 * currentInputDir.x) + (1 * currentInputDir.z)) / (1 * currentInputDir.magnitude)) * Mathf.Rad2Deg;
                        joystickAngle = (horiz > 0) ? -joystickAngle : joystickAngle;
                        //rotate camDir joystickAngle degrees
                        currentInputDir = RotateVector(joystickAngle, camDir);
                        //HARD STEER CHECK
                        //if(!disableAllDebugs)Debug.LogError(" hardSteerOn = "+ hardSteerOn + "; isRotationRestricted = " + myPlayerCombatNew.isRotationRestricted);
                        if (!(!hardSteerOn && myPlayerCombatNew.isRotationRestricted))
                        {
                            Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
                            hardSteerAngleDiff = Vector3.Angle(horizontalVel, currentInputDir);//hard Steer si > 90
                            hardSteerOn = hardSteerAngleDiff > instantRotationMinAngle ? true : false;
                            if (hardSteerOn && !hardSteerStarted)
                            {
                                //if (!disableAllDebugs && hardSteerOn) Debug.LogError("HARD STEER ON: STARTED");
                                hardSteerDir = currentInputDir;
                            }
                        }
                        RotateCharacter();
                        break;
                }
            }
            else
            {
                joystickSens = 1;//no estoy seguro de que esté bien
                moveSt = MoveState.NotMoving;
            }
        }
    }

    void HorizontalMovement()
    {

        float finalMovingAcc = 0;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
        #region//------------------------------------------------ DECIDO TIPO DE MOVIMIENTO --------------------------------------------
        #region//----------------------------------------------------- Efecto externo --------------------------------------------
        if (sufferingStunLikeEffect)
        {
            ProcessSufferingEffect();
        }
        else if (hooked)
        {
            ProcessHooked();
        }
        else if (fixedJumping)// FIXED JUMPING
        {

            ProcessFixedJump();
        }
        ProcessBoost();
        #endregion
        #region //----------------------------------------------------- Efecto interno --------------------------------------------
        if (!hooked && !fixedJumping && moveSt != MoveState.Boost && moveSt != MoveState.Knockback && moveSt != MoveState.Impulse)
        {
            //------------------------------------------------ Direccion Joystick, aceleracion, maxima velocidad y velocidad ---------------------------------
            //------------------------------- Joystick Direction -------------------------------
            CalculateMoveDir();//Movement direction
            //ProcessHardSteer();
            if (!myPlayerCombatNew.aiming && Actions.R2.WasPressed)//Input.GetButtonDown(contName + "RB"))
            {
                StartBoost();
            }

            #region ------------------------------ Max Move Speed ------------------------------
            currentMaxMoveSpeed = myPlayerCombatNew.attackStg != AttackPhaseType.ready && myPlayerCombatNew.landedSinceAttackStarted ? maxAttackingMoveSpeed : maxMoveSpeed;//maxAttackingMoveSpeed == maxMoveSpeed if not attacking
            ProcessWater();//only apply if the new max move speed is lower
            ProcessAiming();//only apply if the new max move speed is lower
            ProcessHooking();//only apply if the new max move speed is lower
            if (currentSpeed > (currentMaxMoveSpeed + 0.1f) && (moveSt == MoveState.Moving || moveSt == MoveState.NotMoving) && !knockbackDone && !impulseDone)
            {
                //Debug.LogWarning("Warning: moveSt set to MovingBreaking!: currentSpeed = "+currentSpeed+ "; maxMoveSpeed2 = " + maxMoveSpeed2 + "; currentVel.magnitude = "+currentVel.magnitude);
                moveSt = MoveState.MovingBreaking;
            }

            finalMaxMoveSpeed = lastJoystickSens > joystickSens && moveSt == MoveState.Moving ? (lastJoystickSens / 1) * currentMaxMoveSpeed : (joystickSens / 1) * currentMaxMoveSpeed;
            ProcessImpulse(currentMaxMoveSpeed);
            #endregion


            #region ------------------------------- Acceleration -------------------------------
            float finalAcc = 0;
            float finalBreakAcc = controller.collisions.below ? breakAcc : airBreakAcc;
            float finalHardSteerAcc = controller.collisions.below ? hardSteerAcc : airHardSteerAcc;
            float finalInitialAcc = controller.collisions.below ? initialAcc : airInitialAcc;
            finalMovingAcc = (controller.collisions.below ? movingAcc : airMovingAcc) * rotationRestrictedPercentage; //Turning accleration
            //if (!disableAllDebugs && rotationRestrictedPercentage!=1) Debug.LogWarning("finalMovingAcc = " + finalMovingAcc+ "; rotationRestrictedPercentage = " + rotationRestrictedPercentage+
            //    "; attackStg = " + myPlayerCombatNew.attackStg);
            //finalBreakAcc = currentSpeed < 0 ? -finalBreakAcc : finalBreakAcc;
            if (knockbackDone && impulseDone)
            {
                Debug.LogError("ERROR, they should not happen at the same time!");
                StopImpulse();
            }
            else if (knockbackDone)
            {
                finalAcc = knockbackBreakAcc;
            }
            else if (impulseDone)
            {
                finalAcc = currentImpulse.impulseAcc;
            }
            else
            {
                switch (moveSt)
                {
                    case MoveState.Moving:
                        if (hardSteerOn)//hard Steer
                        {
                            finalAcc = finalHardSteerAcc;
                        }
                        else //Debug.LogWarning("Moving: angleDiff <= 90");
                        {
                            finalAcc = lastJoystickSens > joystickSens ? finalBreakAcc : finalInitialAcc;
                        }
                        //}
                        break;
                    case MoveState.MovingBreaking:
                        finalAcc = hardBreakAcc;//breakAcc * 3;
                        break;
                    case MoveState.NotMoving:
                        finalAcc = (currentSpeed == 0) ? 0 : finalBreakAcc;
                        break;
                    default:
                        finalAcc = finalBreakAcc;
                        break;
                }
            }
            #endregion

            #region ----------------------------------- Speed ---------------------------------- 
            switch (moveSt)
            {
                case MoveState.Moving:
                    if (hardSteerOn && Mathf.Sign(currentSpeed) == 1)//hard Steer
                    {
                        if (!hardSteerStarted)
                        {
                            hardSteerStarted = true;
                            //Debug.Log("Current speed: Moving: angleDiff > instantRotationMinAngle -> Calculate velNeg" + "; currentInputDir = " + currentInputDir.ToString("F6"));
                            float angle = 180 - hardSteerAngleDiff;
                            float hardSteerInitialSpeed = Mathf.Cos(angle * Mathf.Deg2Rad) * horizontalVel.magnitude;//cos(angle) = velNeg /horizontalVel.magnitude;
                            currentSpeed = hardSteerInitialSpeed;
                        }
                    }
                    break;
                case MoveState.NotMoving:
                    break;
            }
            if (!disableAllDebugs && currentSpeed != 0) Debug.Log("CurrentSpeed 1.2 = " + currentSpeed.ToString("F4") + "; finalAcc = " + finalAcc + "; moveSt = " + moveSt +
                "; currentSpeed =" + currentSpeed.ToString("F4"));
            float currentSpeedB4 = currentSpeed;
            currentSpeed = currentSpeed + finalAcc * Time.deltaTime;
            if (moveSt == MoveState.NotMoving && Mathf.Sign(currentSpeedB4) != Mathf.Sign(currentSpeed))
            {
                currentSpeed = 0;
            }
            if (moveSt == MoveState.Moving && Mathf.Sign(currentSpeed) < 0 && hardSteerOn)
            {
                currentSpeed = -currentSpeed;
                horizontalVel = hardSteerDir * currentSpeed;
                currentVel = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
            }
            //Debug.Log("CurrentSpeed 1.2 = " + currentSpeed);
            float maxSpeedClamp = knockbackDone || impulseDone ? maxKnockbackSpeed : finalMaxMoveSpeed;
            float minSpeedClamp = (lastJoystickSens > joystickSens && moveSt == MoveState.Moving) ? (joystickSens / 1) * currentMaxMoveSpeed : 0;
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeedClamp, maxSpeedClamp);
            if (hardSteerStarted && !hardSteerOn)
            {
                hardSteerStarted = false;
            }
            if (knockbackDone && currentSpeed <= maxMoveSpeed)
            {
                knockbackDone = false;
                startBeingHitAnimation = false;
            }
        }
        #endregion
        #endregion
        #endregion
        #region//------------------------------------------------ PROCESO EL TIPO DE MOVIMIENTO DECIDIDO ---------------------------------
        Vector3 horVel = new Vector3(currentVel.x, 0, currentVel.z);
        if (!disableAllDebugs && currentSpeed != 0) print("CurrentVel before processing= " + currentVel.ToString("F6") + "; currentSpeed =" + currentSpeed.ToString("F4") +
            "; MoveState = " + moveSt + "; currentMaxMoveSpeed = " + finalMaxMoveSpeed + "; below = " + controller.collisions.below + "; horVel.magnitude = " + horVel.magnitude);
        //print("CurrentVel 1.3= " + currentVel.ToString("F6")+ "MoveState = " + moveSt);
        if (jumpSt != VerticalMovementState.WallJumping || (jumpSt == VerticalMovementState.WallJumping && moveSt == MoveState.Knockback))
        {
            horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
            switch (moveSt)
            {
                case MoveState.Moving: //MOVING WITH JOYSTICK
                    Vector3 newDir;
                    if (hardSteerOn)//hard Steer
                    {
                        newDir = horizontalVel;
                    }
                    else
                    {
                        Vector3 oldDir = horizontalVel.magnitude == 0 && myPlayerCombatNew.attackStg != AttackPhaseType.ready ? rotateObj.forward.normalized : horizontalVel.normalized;
                        newDir = oldDir + (currentInputDir * (finalMovingAcc * Time.deltaTime));
                        float auxAngle = Vector3.Angle(oldCurrentVel, newDir);
                        if (!disableAllDebugs) Debug.LogWarning("MOVING: finalMovingAcc2 = " + finalMovingAcc + ";  auxAngle = " + auxAngle + "; (currentInputDir * finalMovingAcc * Time.deltaTime).magnitude = "
                             + (currentInputDir * finalMovingAcc * Time.deltaTime).magnitude + "; (currentInputDir * finalMovingAcc * Time.deltaTime) = "
                             + (currentInputDir * finalMovingAcc * Time.deltaTime) + "; newDir = " + newDir);
                    }
                    horizontalVel = newDir.normalized * currentSpeed;
                    currentVel = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
                    if (!disableAllDebugs) Debug.LogWarning("MOVING: CurrentVel.normalized = " + currentVel.normalized.ToString("F6"));
                    break;
                case MoveState.NotMoving: //NOT MOVING JOYSTICK
                    horizontalVel = horizontalVel.normalized * currentSpeed;
                    currentVel = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
                    break;
                case MoveState.Boost:
                    //if (controller.collisions.collisionHorizontal)//BOOST CONTRA PARED
                    //{
                    //    WallBoost(controller.collisions.horWall);
                    //}
                    //else//BOOST NORMAL
                    //{
                    //boostDir: dirección normalizada en la que quieres hacer el boost
                    horizontalVel = new Vector3(boostDir.x, 0, boostDir.z);
                    horizontalVel = horizontalVel.normalized * boostSpeed;
                    SetVelocity(new Vector3(horizontalVel.x, 0, horizontalVel.z));
                    //}
                    break;
                case MoveState.Knockback:
                    if (!knockbackDone)
                    {
                        if (!disableAllDebugs) print("KNOCKBACK");
                        currentVel = knockback;
                        horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
                        currentSpeed = horizontalVel.magnitude;
                        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxKnockbackSpeed);
                        knockbackDone = true;
                        knockback = Vector3.zero;
                        moveSt = MoveState.NotMoving;
                    }
                    else
                    {
                        Debug.LogError("Error: knockback was already done!");
                    }
                    //print("vel.y = " + currentVel.y);
                    break;
                case MoveState.MovingBreaking://FRENADA FUERTE
                    newDir = horizontalVel.normalized + (currentInputDir * finalMovingAcc * Time.deltaTime);
                    horizontalVel = newDir.normalized * currentSpeed;
                    currentVel = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
                    break;
                case MoveState.Hooked:
                    currentSpeed = horizontalVel.magnitude;
                    break;
                case MoveState.FixedJump:
                    currentVel = knockback;
                    horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
                    currentSpeed = horizontalVel.magnitude;
                    currentSpeed = Mathf.Clamp(currentSpeed, 0, maxKnockbackSpeed);
                    break;
                case MoveState.NotBreaking:
                    currentSpeed = horizontalVel.magnitude;
                    break;
                case MoveState.Impulse:
                    if(!disableAllDebugs)Debug.LogWarning("DOING IMPULSE");
                    impulseDone = true;
                    Vector3 finalImpulse = new Vector3(currentImpulse.impulseVel.x, 0, currentImpulse.impulseVel.z);
                    float maxMag = finalImpulse.magnitude;
                    finalImpulse = horizontalVel + finalImpulse;
                    float finalMag = Mathf.Clamp(finalImpulse.magnitude, 0, maxMag);
                    finalImpulse = finalImpulse.normalized * finalMag;
                    finalImpulse = new Vector3(finalImpulse.x, currentVel.y, finalImpulse.z);
                    SetVelocity(finalImpulse);
                    moveSt = MoveState.NotMoving;
                    break;
            }
        }
        horVel = new Vector3(currentVel.x, 0, currentVel.z);
        //print("CurrentVel after processing= " + currentVel.ToString("F6") + "; CurrentSpeed 1.4 = " + currentSpeed + "; horVel.magnitude = " 
        //    + horVel.magnitude + "; currentInputDir = " + currentInputDir.ToString("F6"));
        #endregion
    }

    public void SetPlayerAttackMovementSpeed(float movementPercent)
    {
        maxAttackingMoveSpeed = movementPercent * maxMoveSpeed;
    }

    //not in use?
    public void ResetPlayerAttackMovementSpeed()
    {
        maxAttackingMoveSpeed = maxMoveSpeed;
    }

    public void SetPlayerRotationSpeed(float rotSpeedPercentage)
    {
        rotationRestrictedPercentage = rotSpeedPercentage;
    }

    public void ResetPlayerRotationSpeed()
    {
        rotationRestrictedPercentage = 1;
    }

    #region -- IMPULSE --
    public void StartImpulse(ImpulseInfo _impulse)
    {
        if (_impulse.impulseDistance != 0)
        {
            currentImpulse = _impulse;
            currentImpulse.initialPlayerPos = transform.position;

            if (!controller.collisions.below)
            {
                currentImpulse.impulseVel *= airImpulsePercentage;
                //IMPORTANT TO DO: 
                //RECALCULATE ALL IMPULSE PARAMETERS
            }
            impulseStarted = true;
            impulseDone = false;
            impulseTime = 0;
            moveSt = MoveState.Impulse;

            //Character rotation
            float angle = Mathf.Acos(((0 * currentImpulse.impulseDir.x) + (1 * currentImpulse.impulseDir.z)) / (1 * currentImpulse.impulseDir.magnitude)) * Mathf.Rad2Deg;
            angle = currentImpulse.impulseDir.x < 0 ? 360 - angle : angle;
            RotateCharacterInstantly(angle);
            if (!disableAllDebugs) Debug.LogWarning("Impulse = " + currentImpulse.impulseVel + "; impulseMaxTime = " + currentImpulse.impulseMaxTime +
                "; impulse.impulseInitialVel = " + currentImpulse.impulseInitialSpeed + "; currentImpulse.impulseDistance = " + currentImpulse.impulseDistance);
        }
    }

    void ProcessImpulse(float currentMaxMoveSpeed)
    {
        if (impulseStarted)
        {
            impulseTime += Time.deltaTime;
            //if (!disableAllDebugs && currentSpeed <= maxMoveSpeed) Debug.LogError("CurrentSpeed = "+ currentSpeed + "; maxMoveSpeed = " + maxMoveSpeed);
            if (impulseTime >= currentImpulse.impulseMaxTime || currentSpeed <= currentMaxMoveSpeed)
            {
                StopImpulse();
            }
            else
            {
                moveSt = MoveState.NotMoving;
            }
        }
    }

    void StopImpulse()
    {
        if (impulseStarted)
        {
            if (!disableAllDebugs) Debug.LogWarning("STOP IMPULSE: impulseTime = " + impulseTime + "; currentImpulse.impulseMaxTime = " + currentImpulse.impulseMaxTime +
                  ";currentSpeed  = " + currentSpeed + "; currentMaxMoveSpeed = " + currentMaxMoveSpeed);
            //moveSt = MoveState.NotMoving;
            impulseStarted = false;
            impulseDone = false;
            moveSt = MoveState.NotMoving;
        }
    }

    public float MissingImpulseTime()
    {
        return Mathf.Clamp(currentImpulse.impulseMaxTime - impulseTime, 0, float.MaxValue);
    }
    #endregion

    void VerticalMovement()
    {
        if (!jumpedOutOfWater && !inWater && controller.collisions.below)
        {
            jumpedOutOfWater = true;

            maxTimePressingJump = jumpApexTime * pressingJumpActiveProportion;
        }

        if (lastWallAngle != -500 && controller.collisions.below)//RESET WALL JUMP VARIABLE TO MAKE IT READY
        {
            lastWallAngle = -500;
            lastWall = null;
        }

        if (Actions.A.WasPressed)//Input.GetButtonDown(contName + "A"))
        {
            PressA();
        }

        if (moveSt != MoveState.Boost)
        {
            if (jumpSt == VerticalMovementState.None || jumpSt == VerticalMovementState.Falling)
            {
                ProcessChargingChargeJump();
            }

            switch (jumpSt)
            {
                case VerticalMovementState.None:
                    if (currentVel.y < 0 && !controller.collisions.below)
                    {
                        if(!disableAllDebugs) Debug.Log("JumpSt = Falling");
                        jumpSt = VerticalMovementState.Falling;
                        chargeJumpLastApexHeight = transform.position.y;
                        //Debug.Log("START FALLING");
                    }
                    currentVel.y += gravity * Time.deltaTime;
                    break;
                case VerticalMovementState.Falling:

                    if (!chargeJumpChargingJump && controller.collisions.below)
                    {
                        if(StartBounceJump())
                        break;
                    }

                    if (currentVel.y >= 0 || controller.collisions.below)
                    {
                        //Debug.Log("STOP FALLING");
                        if(!disableAllDebugs)Debug.Log("JumpSt = None");
                        jumpSt = VerticalMovementState.None;
                    }

                    currentVel.y += gravity * Time.deltaTime;
                    break;
                case VerticalMovementState.Jumping:
                    currentVel.y += gravity * Time.deltaTime;
                    timePressingJump += Time.deltaTime;
                    if (timePressingJump >= maxTimePressingJump)
                    {
                        StopJump();
                    }
                    else
                    {
                        if (Actions.A.WasReleased)//Input.GetButtonUp(contName + "A"))
                        {
                            jumpSt = VerticalMovementState.JumpBreaking;
                        }
                    }
                    break;
                case VerticalMovementState.JumpBreaking:
                    currentVel.y += (gravity * breakJumpForce) * Time.deltaTime;
                    if (currentVel.y <= 0)
                    {
                        if(!disableAllDebugs)Debug.Log("JumpSt = None");
                        jumpSt = VerticalMovementState.None;
                    }
                    break;
                case VerticalMovementState.WallJumping:
                    currentVel.y += wallJumpSlidingAcc * Time.deltaTime;
                    break;
                case VerticalMovementState.ChargeJumping:
                    if (currentVel.y <= 0)
                    {
                        if (!disableAllDebugs) Debug.Log("JumpSt = None");
                        jumpSt = VerticalMovementState.None;
                    }
                    currentVel.y += currentGravity * Time.deltaTime;
                    break;
                case VerticalMovementState.BounceJumping:
                    if (currentVel.y <= 0)
                    {
                        if (!disableAllDebugs) Debug.Log("JumpSt = None");
                        jumpSt = VerticalMovementState.None;
                    }
                    currentVel.y += currentGravity * Time.deltaTime;
                    break;
            }
        }

        if (inWater)
        {
            currentVel.y = Mathf.Clamp(currentVel.y, -maxVerticalSpeedInWater, float.MaxValue);
        }

        ProcessJumpInsurance();

        if (currentVel.y < 0)
        {
            currentVel.y = Mathf.Clamp(currentVel.y, -maxFallSpeed, maxFallSpeed);
        }
    }
    #endregion

    #region JUMP ---------------------------------------------------
    void PressA()
    {
        if (!StartJump())
        {
            if (!StartWallJump())
            {
                StartChargingChargeJump();
            }
        }
    }

    bool StartJump(bool calledFromBuffer = false)
    {
        bool result = false;
        if (!noInput && moveSt != MoveState.Boost)
        {
            if (!disableAllDebugs) Debug.Log("START JUMP: below = "+ controller.collisions.below + "; jumpInsurance = " + jumpInsurance+ "; inWater = "+ inWater);
            if ((controller.collisions.below || jumpInsurance) && !isChargeJump && !isBounceJump &&
                (!inWater || (inWater && controller.collisions.around &&
                ((gC.gameMode == GameMode.CaptureTheFlag && !(gC as GameController_FlagMode).myScoreManager.prorroga) || (gC.gameMode != GameMode.CaptureTheFlag)))))
            {
                if (!disableAllDebugs) Debug.LogWarning("JUMP!");
                //PlayerAnimation_01.startJump = true;
                myPlayerAnimation_01.SetJump(true);
                result = true;
                currentVel.y = jumpVelocity;
                jumpSt = VerticalMovementState.Jumping;
                timePressingJump = 0;
                //myPlayerAnimation.SetJump(true);

            }
        }
        else
        {
            if (!disableAllDebugs) Debug.LogWarning("Warning: Can't jump because: player is in noInput mode(" + !noInput + ") / moveSt != Boost (" + (moveSt != MoveState.Boost) + ")");
        }

        if (!result && !calledFromBuffer /*&& (jumpSt != JumpState.Falling || (jumpSt == JumpState.Falling) && jumpInsurance)*/)
        {
            BufferInput(PlayerInput.Jump);
        }
        return result;
    }

    void StopJump()
    {
        myPlayerAnimation_01.SetJump(false);
        if (!disableAllDebugs) Debug.Log("JumpSt = None");
        jumpSt = VerticalMovementState.None;
        timePressingJump = 0;
    }

    void ProcessJumpInsurance()
    {
        if (!jumpInsurance)
        {
            //Debug.LogWarning(" controller.collisions.lastBelow = " + (controller.collisions.lastBelow) + "; controller.collisions.below = " + (controller.collisions.below) +
            //   "; jumpSt = " + jumpSt);
            if (controller.collisions.lastBelow && !controller.collisions.below && (jumpSt == VerticalMovementState.None || jumpSt == VerticalMovementState.Falling) &&
                (jumpSt != VerticalMovementState.BounceJumping && jumpSt != VerticalMovementState.ChargeJumping) && jumpedOutOfWater)
            {
                //print("Jump Insurance");
                jumpInsurance = true;
                timeJumpInsurance = 0;
            }
        }
        else
        {
            timeJumpInsurance += Time.deltaTime;
            if (timeJumpInsurance >= maxTimeJumpInsurance || jumpSt == VerticalMovementState.Jumping)
            {
                jumpInsurance = false;
            }
        }

    }

    /// <summary>
    /// In order to be able to save the input for Input buffering, we return true if the input was successful, 
    /// and false if the input was not successful and should be buffered.
    /// </summary>
    /// <returns></returns>
    bool StartWallJump(bool calledFromBuffer = false)
    {
        if (!disableAllDebugs) Debug.LogWarning("Check Wall jump: wall real normal = " + controller.collisions.closestHorRaycast.slopeAngle);
        bool result = false;
        float slopeAngle = controller.collisions.closestHorRaycast.slopeAngle;
        bool goodWallAngle = !(slopeAngle >= 110 && slopeAngle <= 180);
        wallJumpCurrentWall = controller.collisions.horWall;
        if (!controller.collisions.below && !inWater && jumpedOutOfWater && controller.collisions.collisionHorizontal && goodWallAngle &&
            (!firstWallJumpDone || lastWallAngle != controller.collisions.wallAngle || (lastWallAngle == controller.collisions.wallAngle &&
            lastWall != controller.collisions.horWall)) && wallJumpCurrentWall.tag == "Stage")
        {
            if (wallJumpCurrentWall.GetComponent<StageScript>() == null || wallJumpCurrentWall.GetComponent<StageScript>().wallJumpable)
            {
                if (!disableAllDebugs) Debug.Log("Wall jumping start");
                //PARA ORTU: PlayerAnimation_01.startJump = true;
                jumpSt = VerticalMovementState.WallJumping;
                result = true;
                //wallJumped = true;
                stopWallTime = 0;
                SetVelocity(Vector3.zero);
                wallJumping = true;
                anchorPoint = transform.position;
                wallNormal = controller.collisions.wallNormal;
                wallNormal.y = 0;
                wallJumpCurrentWallAngle = controller.collisions.wallAngle;
                wallJumpRaycastAxis = controller.collisions.closestHorRaycast.axis;
                //Debug.Log("WALL JUMP RAY HEIGHT PERCENTAGE : " + controller.collisions.closestHorRaycast.rayHeightPercentage + "%; wall = " + wallJumpCurrentWall.name);

                //STOP OTHER JUMPS BUFFERINGS AND PROCESSES
                StopBufferedInput(PlayerInput.Jump);
                StopBufferedInput(PlayerInput.StartChargingChargeJump);
                StopChargingChargeJump();
            }

        }
        else
        {
            if (!disableAllDebugs) Debug.Log("Couldn't wall jump because:  !controller.collisions.below (" + !controller.collisions.below + ") && !inWater(" + !inWater + ") &&" +
                " jumpedOutOfWater(" + jumpedOutOfWater + ") && controller.collisions.collisionHorizontal(" + controller.collisions.collisionHorizontal + ") && " +
                "(!firstWallJumpDone(" + !firstWallJumpDone + ") || lastWallAngle != controller.collisions.wallAngle (" + (lastWallAngle != controller.collisions.wallAngle) + ") || " +
                "(lastWallAngle == controller.collisions.wallAngle (" + (lastWallAngle == controller.collisions.wallAngle) + ")&& " +
                "lastWall != controller.collisions.horWall(" + (lastWall != controller.collisions.horWall) + ")))");
        }
        if (!result && !calledFromBuffer)
        {
            BufferInput(PlayerInput.WallJump);
        }
        return result;
    }

    void ProcessWallJump()//IMPORTANTE QUE VAYA ANTES DE LLAMAR A "MOVE"
    {
        if (wallJumping)
        {
            stopWallTime += Time.deltaTime;
            if (Actions.A.WasReleased || !Actions.A.IsPressed)
            {
                EndWallJump();
                return;
            }

            if (stopWallTime >= stopWallMaxTime)
            {
                StopWallJump();
                return;
            }

            #region --- WALL JUMP CHECK RAYS ---
            //Check continuously if we are still attached to wall
            float rayLength = controller.coll.bounds.extents.x + 0.5f;
            RaycastHit hit;

            //calculamos el origen de todos los rayos y columnas: esquina inferior (izda o dcha,no sé)
            //del personaje. 
            Vector3 paralelToWall = new Vector3(-wallNormal.z, 0, wallNormal.x).normalized;
            Vector3 rowsOrigin = new Vector3(controller.coll.bounds.center.x, controller.coll.bounds.min.y, controller.coll.bounds.center.z);
            rowsOrigin -= paralelToWall * controller.coll.bounds.extents.x;
            Vector3 dir = -wallNormal.normalized;
            bool success = false;
            for (int i = 0; i < wallJumpCheckRaysRows && !success; i++)
            {
                Vector3 rowOrigin = rowsOrigin + Vector3.up * wallJumpCheckRaysRowsSpacing * i;
                for (int j = 0; j < wallJumpCheckRaysColumns && !success; j++)
                {
                    Vector3 rayOrigin = rowOrigin + paralelToWall * wallJumpCheckRaysColumnsSpacing * j;
                    //Debug.Log("WallJump: Ray[" + i + "," + j + "] with origin = " + rayOrigin.ToString("F4") + "; rayLength =" + rayLength);
                    //Debug.DrawRay(rayOrigin, dir * rayLength, Color.blue, 3);

                    if (Physics.Raycast(rayOrigin, dir, out hit, rayLength, auxLM, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.gameObject == wallJumpCurrentWall)
                        {
                            success = true;
                            //if(!disableAllDebugs)Debug.Log("WallJump: Success! still walljumping!");
                        }
                        else
                        {
                            //if (!disableAllDebugs) Debug.Log("WallJump: this wall (" + hit.transform.gameObject + ")is not the same wall that I started walljumping from " +
                            //    "(" + wallJumpCurrentWall + ").");
                        }
                    }
                }
            }
            #endregion

            if (!success)
            {
                //if (!disableAllDebugs) Debug.LogError("STOPPED WALLJUMPING DUE TO NOT DETECTING THE WALL ANYMORE. wallJumpCheckRaysRows = " + wallJumpCheckRaysRows);
                StopWallJump();
            }
        }

    }

    void EndWallJump()
    {
        if (!disableAllDebugs) Debug.Log("End Wall jump");
        if (!firstWallJumpDone) firstWallJumpDone = true;
        lastWallAngle = wallJumpCurrentWallAngle;
        lastWall = wallJumpCurrentWall;
        wallJumping = false;
        wallJumpAnim = true;
        jumpSt = VerticalMovementState.None;
        wallJumpRaycastAxis = Axis.none;
        //CALCULATE JUMP DIR
        //LEFT OR RIGHT ORIENTATION?
        //Angle
        Vector3 circleCenter = anchorPoint + Vector3.up * walJumpConeHeight;
        Vector3 circumfPoint = CalculateReflectPoint(wallJumpRadius, wallNormal, circleCenter);
        Vector3 finalDir = (circumfPoint - anchorPoint).normalized;
        if (!disableAllDebugs) Debug.LogWarning("FINAL DIR= " + finalDir.ToString("F4"));

        SetVelocity(finalDir * wallJumpVelocity);
        Vector3 newMoveDir = new Vector3(finalDir.x, 0, finalDir.z);
        RotateCharacter(newMoveDir);

        //myPlayerAnimation.SetJump(true);

        Debug.DrawLine(anchorPoint, circleCenter, Color.white, 20);
        Debug.DrawLine(anchorPoint, circumfPoint, Color.yellow, 20);
    }

    public void StopWallJump()
    {
        //if (!disableAllDebugs) print("STOP WALLJUMP");
        wallJumping = false;
        wallJumpAnim = true;
        jumpSt = VerticalMovementState.None;
    }

    bool StartChargingChargeJump(bool calledFromBuffer = false)
    {
        bool result = false;
        //Debug.Log("chargeJumpChargingJump = " + chargeJumpChargingJump);
        if (!chargeJumpChargingJump && !chargeJumpButtonReleased && jumpSt == VerticalMovementState.Falling && !inWater && !noInput && !jumpInsurance)
        {
            result = true;
            chargeJumpChargingJump = true;
            chargeJumpLanded = false;
            //chargeJumpButtonReleased = false;
            chargeJumpCurrentReleaseTime = 0;
            chargeJumpLandedTime = 0;
            chargeJumpChargingStartHeight = transform.position.y;
            failedJump = false;
            //StopBufferedInput(PlayerInput.Jump);
            Debug.Log("Start Charging Jump");
        }

        if (!result && !calledFromBuffer)
        {
            BufferInput(PlayerInput.StartChargingChargeJump);
        }
        return result;
    }

    void ProcessChargingChargeJump()
    {
        if (chargeJumpChargingJump)
        {
            if (!chargeJumpButtonReleased && Actions.A.WasReleased)
            {
                chargeJumpButtonReleased = true;
            }

            if (!chargeJumpLanded && controller.collisions.below)
            {
                if(!isFloorChargeJumpable)
                {
                    StopChargingChargeJump();
                    StartBounceJump();
                    return;
                }
                chargeJumpLanded = true;
            }

            //Debug.Log("charging jump: button released = " + chargeJumpButtonReleased);
            if (chargeJumpButtonReleased)
            {
                //Debug.Log("charging jump: chargeJumpLanded = " + chargeJumpLanded);
                if (chargeJumpLanded)
                {
                    StopChargingChargeJump();
                    if (failedJump)
                    {
                        StartBounceJump();
                    }
                    else
                    {
                        StartChargeJump();
                    }
                    return;
                }

                //FAIL?
                if (chargeJumpCurrentReleaseTime >= chargeJumpReleaseTimeBeforeLand)//FAIL: Released too early
                {
                    //Debug.LogWarning("Charged Jump-> FAILED JUMP: RELEASED TOO EARLY");
                    failedJump = true;
                    return;
                }

                chargeJumpCurrentReleaseTime += Time.deltaTime;
            }

            //FAIL?
            if (chargeJumpLanded && !chargeJumpButtonReleased)
            {
                if (chargeJumpLandedTime >= chargeJumpReleaseTimeAfterLand)//FAIL: Released too late
                {
                    Debug.LogWarning("Charged Jump-> FAILED JUMP: RELEASED TOO LATE");
                    StopChargingChargeJump();
                    StartBounceJump();
                    return;
                }
                Debug.Log("chargeJumpLandedTime  = " + chargeJumpLandedTime);
                chargeJumpLandedTime += Time.deltaTime;
            }
        }
    }

    void StopChargingChargeJump()
    {
        if (chargeJumpChargingJump)
        {
            chargeJumpChargingJump = false;
            chargeJumpLanded = false;
            chargeJumpButtonReleased = false;
            chargeJumpCurrentReleaseTime = 0;
            chargeJumpLandedTime = 0;
            Debug.Log("Stop Charging Jump");
        }
    }

    void StartChargeJump()
    {
        if (isFloorChargeJumpable && !inWater && !noInput)
        {
            Debug.Log("DO CHARGE JUMP!");
            float totalFallenHeight = chargeJumpLastApexHeight - transform.position.y;
            if (totalFallenHeight == 0) Debug.LogError("Charge Jump Error: totalFallenHeight = 0");
            float totalChargedHeight = chargeJumpChargingStartHeight - transform.position.y;
            float percentageCharged = totalChargedHeight / totalFallenHeight;
            if (percentageCharged >= chargeJumpMinPercentageNeeded)
            {
                float auxChargeJumpCurrentJumpMaxHeight = totalFallenHeight + (totalFallenHeight * percentageCharged * chargeJumpFallenHeightMultiplier);
                float distToCloudHeight = Mathf.Abs(chargeJumpMaxHeight - transform.position.y);
                float currentMaxHeight = Mathf.Min(chargeJumpMaxJumpHeight, distToCloudHeight);
                chargeJumpCurrentJumpMaxHeight = Mathf.Clamp(auxChargeJumpCurrentJumpMaxHeight, jumpHeight, currentMaxHeight);

                float chargeJumpApexTime = Mathf.Sqrt((2 * chargeJumpCurrentJumpMaxHeight) / Mathf.Abs(currentGravity));
                float chargeJumpJumpVelocity = Mathf.Abs(currentGravity * chargeJumpApexTime);

                currentVel.y = chargeJumpJumpVelocity;

                jumpSt = VerticalMovementState.ChargeJumping;
                Debug.Log("percentageCharged = " + percentageCharged + "; totalFallenHeight = " + totalFallenHeight + "; chargeJumpMaxHeight = " + chargeJumpMaxHeight + "; transform.position.y = "
                    + transform.position.y + "; distToCloudHeight = " + distToCloudHeight + "; currentMaxHeight = " + currentMaxHeight
                    + "; auxChargeJumpCurrentJumpMaxHeight = " + auxChargeJumpCurrentJumpMaxHeight + "; chargeJumpCurrentJumpMaxHeight = " + chargeJumpCurrentJumpMaxHeight + "; chargeJumpApexTime = "
                    + chargeJumpApexTime + "; chargeJumpJumpVelocity = " + chargeJumpJumpVelocity);
            }
            else
            {
                Debug.LogWarning("Charged Jump-> Failed Jump: Charged less than a " + (chargeJumpMinPercentageNeeded * 100) + "% of the fall");
                StartBounceJump();
            }
        }
    }

    bool StartBounceJump()//WHEN CHARGEJUMP FAILS
    {
        bool result = false;

        if (!inWater && isFloorBounceJumpable)
        {
            if (!disableAllDebugs) Debug.Log("DO BOUNCE JUMP");
            float totalFallenHeight = chargeJumpLastApexHeight - transform.position.y;
            chargeJumpCurrentJumpMaxHeight = totalFallenHeight * bounceJumpMultiplier;
            if (chargeJumpCurrentJumpMaxHeight >= bounceJumpMinHeight)
            {
                float chargeJumpApexTime = Mathf.Sqrt((2 * chargeJumpCurrentJumpMaxHeight) / Mathf.Abs(currentGravity));
                float chargeJumpJumpVelocity = Mathf.Abs(currentGravity * chargeJumpApexTime);
                currentVel.y = chargeJumpJumpVelocity;

                Debug.Log("JumpSt = BounceJumping");
                jumpSt = VerticalMovementState.BounceJumping;
                StopBufferedInput(PlayerInput.Jump);
                StopBufferedInput(PlayerInput.WallJump);
                result = true;
            }
            else
            {
                if (!disableAllDebugs) Debug.LogWarning("Bounce Jump -> bounceJump height was too low (min = "+ bounceJumpMinHeight + ")");
            }
        }
        return result;
    }

    #endregion

    #region  DASH ---------------------------------------------

    void StartBoost()
    {
        if (!noInput && boostReady && !inWater && myPlayerCombatNew.attackStg == AttackPhaseType.ready)
        {
            //noInput = true;
            //PARA ORTU: Variable para empezar boost

            //myPlayerAnimation_01.dash = true;
            boostCurrentFuel -= boostCapacity * boostFuelLostOnStart;
            boostCurrentFuel = Mathf.Clamp(boostCurrentFuel, 0, boostCapacity);
            moveSt = MoveState.Boost;
            if (currentInputDir != Vector3.zero)//Usando el joystick o dirección
            {
                boostDir = currentInputDir;
                RotateCharacter(boostDir);
            }
            else//sin tocar ninguna dirección/joystick
            {
                Vector3 newMoveDir = boostDir = new Vector3(currentCamFacingDir.x, 0, currentCamFacingDir.z);//nos movemos en la dirección en la que mire la cámara
                RotateCharacter(newMoveDir);
            }
            myPlayerHUD.StartCamVFX(CameraVFXType.Dash);
            myPlayerVFX.ActivateEffect(PlayerVFXType.DashWaterImpulse);
            myPlayerVFX.ActivateEffect(PlayerVFXType.DashTrail);
            myPlayerCombatNew.StopDoingCombat();
            StopImpulse();
        }
        else
        {
            myPlayerHUD.StartDashHUDCantDoAnimation();
        }

    }

    void ProcessBoost()
    {
        if (moveSt == MoveState.Boost)
        {
            //print("PROCESS BOOST: boostTime = " + boostCurrentFuel + "; boostDuration = "+ boostCapacity);
            boostCurrentFuel -= boostDepletingSpeed * Time.deltaTime;
            boostCurrentFuel = Mathf.Clamp(boostCurrentFuel, 0, boostCapacity);
            if (boostCurrentFuel > 0)
            {
                if (Actions.R2.WasReleased)
                {
                    StopBoost();
                }
                //moveSt = MoveState.Boost;
            }
            else
            {
                StopBoost();
            }
        }
        else
        {
            if (boostCurrentFuel < boostCapacity)
            {
                ProcessBoostRecharge();
            }
            ProcessBoostCD();
        }
    }

    public void StopBoost()
    {
        if (moveSt == MoveState.Boost)
        {
            //print("STOP BOOST");
            moveSt = MoveState.None;
            StartBoostCD();
            myPlayerHUD.StopCamVFX(CameraVFXType.Dash);
            myPlayerVFX.DeactivateEffect(PlayerVFXType.DashWaterImpulse);
            myPlayerVFX.DeactivateEffect(PlayerVFXType.DashTrail);
        }
        //noInput = false;
        //PARA ORTU: Variable para terminar boost

        //myPlayerAnimation_01.dash = false;
    }

    void ProcessBoostRecharge()
    {
        if (moveSt != MoveState.Boost && boostCurrentFuel < boostCapacity)
        {
            boostCurrentFuel += boostRechargingSpeed * Time.deltaTime;
            boostCurrentFuel = Mathf.Clamp(boostCurrentFuel, 0, boostCapacity);
        }
    }

    void StartBoostCD()
    {
        if (!boostCDStarted)
        {
            boostCDStarted = true;
            boostCDTime = 0;
        }
    }

    void ProcessBoostCD()
    {
        if (boostCDStarted)
        {
            boostCDTime += Time.deltaTime;
            if (boostCDTime >= boostCDMaxTime)
            {
                StopBoostCD();
            }
        }
    }

    void StopBoostCD()
    {
        if (boostCDStarted)
        {
            boostCDStarted = false;
        }
    }

    void WallBoost(GameObject wall)
    {
        if (wall.tag == "Stage")
        {
            //CALCULATE JUMP DIR
            Vector3 circleCenter = transform.position;
            Vector3 circumfPoint = CalculateReflectPoint(1, controller.collisions.wallNormal, circleCenter);
            Vector3 finalDir = (circumfPoint - circleCenter).normalized;
            Debug.LogWarning("WALL BOOST: FINAL DIR = " + finalDir.ToString("F4"));

            currentVel = finalDir * currentVel.magnitude;
            currentSpeed = currentVel.magnitude;
            boostDir = new Vector3(finalDir.x, 0, finalDir.z);
            RotateCharacter(boostDir);
        }
    }

    #endregion

    #region  FACING DIR AND ANGLE & BODY ROTATION---------------------------------------------

    void UpdateFacingDir()//change so that only rotateObj rotates, not whole body
    {
        switch (myCamera.camMode)
        {
            case CameraMode.Fixed:
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                //Calculate looking dir of camera
                Vector3 camPos = myCamera.transform.GetChild(0).position;
                Vector3 myPos = transform.position;
                //currentFacingDir = new Vector3(myPos.x - camPos.x, 0, myPos.z - camPos.z).normalized;
                currentCamFacingDir = myCamera.myCamera.transform.forward.normalized;
                break;
            case CameraMode.Shoulder:
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                //currentFacingDir = RotateVector(-myCamera.transform.localRotation.eulerAngles.y, Vector3.forward).normalized;
                currentCamFacingDir = myCamera.myCamera.transform.forward.normalized;
                //print("CurrentFacingDir = " + currentFacingDir);
                break;
            case CameraMode.Free:
                facingAngle = rotateObj.localRotation.eulerAngles.y;
                //currentFacingDir = RotateVector(-rotateObj.localRotation.eulerAngles.y, Vector3.forward).normalized;
                currentCamFacingDir = (cameraFollow.position - myCamera.myCamera.transform.position).normalized;
                break;
        }
        currentFacingDir = rotateObj.forward;
        //print("currentFacingDir = " + currentFacingDir + "; currentCamFacingDir = " + currentCamFacingDir);

    }

    public void RotateCharacter(float rotSpeed = 0)
    {
        switch (myCamera.camMode)
        {
            case CameraMode.Fixed:
                Vector3 point1 = transform.position;
                Vector3 point2 = new Vector3(point1.x, point1.y + 1, point1.z);
                Vector3 dir = new Vector3(point2.x - point1.x, point2.y - point1.y, point2.z - point1.z);
                rotateObj.Rotate(dir, rotSpeed * Time.deltaTime);
                break;
            case CameraMode.Shoulder:
                point1 = transform.position;
                point2 = new Vector3(point1.x, point1.y + 1, point1.z);
                dir = new Vector3(point2.x - point1.x, point2.y - point1.y, point2.z - point1.z);
                rotateObj.Rotate(dir, rotSpeed * Time.deltaTime);
                break;
            case CameraMode.Free:
                Vector3 horVel = new Vector3(currentVel.x, 0, currentVel.z);
                Vector3 lookingDir = wallJumping ? currentInputDir : hardSteerOn ? hardSteerDir : horVel; lookingDir.Normalize();
                if (lookingDir != Vector3.zero)
                {
                    float angle = Mathf.Acos(((0 * lookingDir.x) + (1 * lookingDir.z)) / (1 * lookingDir.magnitude)) * Mathf.Rad2Deg;
                    angle = lookingDir.x < 0 ? 360 - angle : angle;
                    RotateCharacterInstantly(angle);
                }
                //if (currentInputDir != Vector3.zero)
                //{
                //    currentRotation = rotateObj.localRotation.eulerAngles.y;

                //    float angle = Mathf.Acos(((0 * currentInputDir.x) + (1 * currentInputDir.z)) / (1 * currentInputDir.magnitude)) * Mathf.Rad2Deg;
                //    angle = currentInputDir.x < 0 ? 360 - angle : angle;
                //    targetRotation = angle;
                //    Vector3 targetRotationVector = AngleToVector(targetRotation);
                //    Vector3 currentRotationVector = AngleToVector(currentRotation);
                //    float rotationDiff = Vector3.Angle(targetRotationVector, currentRotationVector);
                //    if (hasCharacterRotation)
                //    {
                //        #region --- Character Rotation with rotation speed ---
                //        if (currentRotation != targetRotation)
                //        {
                //            if (!myPlayerCombatNew.isRotationRestricted && rotationDiff > instantRotationMinAngle)//Instant rotation
                //            {
                //                //Debug.LogError("INSTANT BODY ROTATION, angle = "+targetRotation);
                //                RotateCharacterInstantly(targetRotation);
                //                //Debug.LogError("INSANT BODYY ROTATION after, angle = " + rotateObj.localRotation.eulerAngles.y);
                //            }
                //            else
                //            {//rotate with speed
                //                #region --- Decide rotation direction ---
                //                int sign = 1;
                //                bool currentRotOver180 = currentRotation > 180 ? true : false;
                //                bool targetRotOver180 = targetRotation > 180 ? true : false;
                //                if (currentRotOver180 == targetRotOver180)
                //                {
                //                    sign = currentRotation > targetRotation ? -1 : 1;
                //                }
                //                else
                //                {
                //                    float oppositeAngle = currentRotation + 180;
                //                    oppositeAngle = oppositeAngle > 360 ? oppositeAngle - 360 : oppositeAngle;
                //                    //print("oppositeAngle = " + oppositeAngle);
                //                    sign = oppositeAngle < targetRotation ? -1 : 1;
                //                }
                //                #endregion

                //                float newAngle = currentRotation + (sign * currentRotationSpeed * Time.deltaTime);
                //                Vector3 newAngleVector = AngleToVector(newAngle);
                //                float newAngleDiff = Vector3.Angle(currentRotationVector, newAngleVector);
                //                if (newAngleDiff > rotationDiff) newAngle = targetRotation;
                //                rotateObj.localRotation = Quaternion.Euler(0, newAngle, 0);
                //            }
                //        }
                //        #endregion
                //    }
                //    else
                //    {
                //        RotateCharacterInstantly(targetRotation);
                //    }
                //}
                break;
        }
        //print("current angle = " + rotateObj.rotation.eulerAngles.y + "; currentInputDir = "+currentInputDir);
    }

    void RotateCharacter(Vector3 dir)
    {
        float angle = Mathf.Acos(dir.z / dir.magnitude) * Mathf.Rad2Deg;
        angle = dir.x < 0 ? 360 - angle : angle;
        rotateObj.localRotation = Quaternion.Euler(0, angle, 0);
    }

    void RotateCharacterInstantly(float angle)
    {
        rotateObj.localRotation = Quaternion.Euler(0, angle, 0);
    }

    #endregion

    #region RECIEVE HIT AND EFFECTS ---------------------------------------------
    PlayerMovement lastAttacker = null;
    int lastAutocomboIndex = -1;
    bool stunProtectionOn = false;
    public bool StartReceiveHit(PlayerMovement attacker, Vector3 _knockback, EffectType effect, float _maxTime = 0, byte autocomboIndex=255)
    {
        if (!myPlayerCombatNew.invulnerable)
        {
            //Variable para Ortu
            startBeingHitAnimation = true;

            if (!disableAllDebugs) print("Recieve hit with knockback= " + _knockback + "; effect = " + effect + "; maxtime = " + _maxTime);
            myPlayerHook.FinishAutoGrapple();
            myPlayerHook.StopHook();
            StopWallJump();
            StopBoost();
            myPlayerCombatNew.StopDoingCombat();
            StopImpulse();

            //if (sufferingEffect == EffectType.stun)
            //{
            //    efecto = EffectType.knockdown;
            //    _maxTime = AttackEffect.knockdownTime;
            //}
            #region STUN PROTECTION

            if(sufferingEffect == EffectType.softStun && (lastAttacker != attacker ||(lastAttacker == attacker && autocomboIndex == 0)))
            {
                Debug.Log("Stun protection ON");
                stunProtectionOn = true;
            }

            if (!stunProtectionOn)
            {
                Debug.Log("FULL EFFECT TIME");
                effectMaxTime = _maxTime;
            }
            else
            {
                Debug.Log("THIRD EFFECT TIME");
                effectMaxTime = _maxTime / 3;
            }
            #endregion

            sufferingEffect = effect;
            switch (effect)
            {
                case EffectType.softStun:
                    noInput = true;
                    break;
                //case EffectType.stun:
                //    if (disableAllDebugs) Debug.LogError("STUN !!!!!!!!");
                //    noInput = true;
                //    break;
                //case EffectType.knockdown:
                //    if (disableAllDebugs) Debug.LogError("KNOCKDOWN !!!!!!!!");
                //    noInput = true;
                //    //myPlayerCombatNew.StartInvulnerabilty(_maxTime);
                //    break;
                case EffectType.none:
                    break;
                default:
                    Debug.LogError("Error: cannot have a 'sufferingEffect' of type " + sufferingEffect);
                    break;
            }

            //Debug.Log("_maxTime = " + _maxTime + "; effectMaxTime = " + effectMaxTime);
            effectTime = 0;
            if (_knockback != Vector3.zero)
            {
                knockbackDone = false;
                _knockback = _knockback / bodyMass;
                knockback = _knockback;
            }

            //Give FLAG
            if (haveFlag)
            {
                flag.DropFlag();
            }
            lastAttacker = attacker;
            lastAutocomboIndex = autocomboIndex;
            if (!disableAllDebugs) print("Player " + playerNumber + " RECIEVED HIT");
            return true;
        }
        else return false;
    }

    public bool StartReceiveHit(Vector3 _knockback, EffectType effect, float _maxTime = 0)
    {
        if (!myPlayerCombatNew.invulnerable)
        {
            //Variable para Ortu
            startBeingHitAnimation = true;

            if (!disableAllDebugs) print("Recieve hit with knockback= " + _knockback + "; effect = " + effect + "; maxtime = " + _maxTime);
            myPlayerHook.FinishAutoGrapple();
            myPlayerHook.StopHook();
            StopWallJump();
            StopBoost();
            myPlayerCombatNew.StopDoingCombat();
            StopImpulse();

            sufferingEffect = effect;
            switch (effect)
            {
                case EffectType.softStun:
                    noInput = true;
                    break;
                case EffectType.none:
                    break;
                default:
                    Debug.LogError("Error: cannot have a 'sufferingEffect' of type " + sufferingEffect);
                    break;
            }

            //Debug.Log("_maxTime = " + _maxTime + "; effectMaxTime = " + effectMaxTime);
            effectTime = 0;
            if (_knockback != Vector3.zero)
            {
                knockbackDone = false;
                _knockback = _knockback / bodyMass;
                knockback = _knockback;
            }

            //Give FLAG
            if (haveFlag)
            {
                flag.DropFlag();
            }
            if (!disableAllDebugs) print("Player " + playerNumber + " RECIEVED HIT");
            return true;
        }
        else return false;
    }

    //ES RECEIVE
    public void StartRecieveParry(PlayerMovement enemy, AttackEffect effect = null)
    {
        if(!disableAllDebugs)Debug.Log("PARRY!!!");
        float knockbackMag = effect == null ? 10 : effect.knockbackMagnitude;
        float maxStunTime = effect != null && effect.parryStunTime > 0 ? effect.parryStunTime : 0.5f;

        Vector3 enemyPos = enemy.transform.position;
        //Reduce Recovery Time Player 1
        enemy.myPlayerCombatNew.HitParry();

        //StartRecieveParry Player2
        //Knockback outwards
        Vector3 parryMyPos = enemyPos;
        Vector3 parryColPos = transform.position;
        Vector3 resultKnockback = new Vector3(parryColPos.x - parryMyPos.x, 0, parryColPos.z - parryMyPos.z).normalized;
        //resultKnockback = Quaternion.Euler(0, effect.knockbackYAngle, 0) * resultKnockback;
        if(!disableAllDebugs)Debug.Log("resultKnockback 1 = " + resultKnockback);
        resultKnockback = Hitbox.CalculateYAngle(enemyPos, resultKnockback, 25f);
        if (!disableAllDebugs) Debug.Log("resultKnockback 2 = " + resultKnockback);
        resultKnockback = resultKnockback * knockbackMag;
        if (!disableAllDebugs) Debug.Log("resultKnockback 3 = " + resultKnockback + "; maxStunTime = " + maxStunTime);
        StartReceiveHit(enemy, resultKnockback, EffectType.softStun, maxStunTime);
    }

    void ReceiveKnockback()
    {
        if (knockback != Vector3.zero)
        {
            moveSt = MoveState.Knockback;
        }
    }

    void ProcessSufferingEffect()
    {
        ReceiveKnockback();
        if (sufferingEffect == EffectType.softStun)//|| sufferingEffect == EffectType.stun || sufferingEffect == EffectType.knockdown
        {
            effectTime += Time.deltaTime;
            if (effectTime >= effectMaxTime)
            {
                StopSufferingEffect();
            }
        }
    }

    void StopSufferingEffect()
    {
        if (!disableAllDebugs) print("Suffering effect " + sufferingEffect + " END");
        switch (sufferingEffect)
        {
            case EffectType.softStun:
                noInput = false;
                sufferingEffect = EffectType.none;
                effectTime = 0;
                stunProtectionOn = false;
                Debug.Log("Stun protection OFF");
                break;
            //case EffectType.stun:
            //    noInput = false;
            //    sufferingEffect = EffectType.none;
            //    effectTime = 0;
            //    break;
            //case EffectType.knockdown:
            //    noInput = false;
            //    sufferingEffect = EffectType.none;
            //    effectTime = 0;
            //    break;
            case EffectType.none:
                break;
            default:
                Debug.LogError("Error: cannot have a 'sufferingEffect' of type " + sufferingEffect);
                break;
        }
        startBeingHitAnimation = false;
    }

    #endregion

    #region FIXED JUMP ---------------------------------------------------

    public void StartFixedJump(Vector3 vel, float _noMoveMaxTime)
    {
        fixedJumping = true;
        fixedJumpDone = false;
        noInput = true;
        noMoveMaxTime = _noMoveMaxTime;
        noMoveTime = 0;
        knockback = vel;
        StopBoost();
        myPlayerCombatNew.StopDoingCombat();
    }

    void ProcessFixedJump()
    {
        if (fixedJumping)
        {
            if (!fixedJumpDone)
            {
                moveSt = MoveState.FixedJump;
                fixedJumpDone = true;
            }
            else
            {
                //print("notBreaking on");
                moveSt = MoveState.NotBreaking;
            }
            noMoveTime += Time.deltaTime;
            if (noMoveTime >= noMoveMaxTime)
            {
                StopFixedJump();
            }
        }
    }

    void StopFixedJump()
    {
        if (fixedJumping)
        {
            fixedJumping = false;
            noInput = false;
        }
    }

    #endregion

    #region HOOKING/HOOKED ---------------------------------------------
    public bool StartHooked()
    {
        if (!hooked && !myPlayerCombatNew.invulnerable)
        {
            noInput = true;
            hooked = true;
            if (jumpSt == VerticalMovementState.WallJumping)
            {
                StopWallJump();
            }
            if (moveSt == MoveState.Boost)
            {
                StopBoost();
            }
            //Stop attacking
            myPlayerCombatNew.StopDoingCombat();
            StopImpulse();
            return true;
        }
        else
        {
            return false;
        }
    }

    void ProcessHooked()
    {
        if (hooked)
        {
            moveSt = MoveState.Hooked;
        }
    }

    public void StopHooked(float reducedSpeedPercentage = 0)
    {
        //print("STOP HOOKED");
        if (hooked)
        {
            noInput = false;
            hooked = false;
            SetVelocity(currentVel * reducedSpeedPercentage);
        }
    }

    public void StartHooking()
    {
        if (!hooking)
        {
            hooking = true;
            myPlayerCombatNew.StopDoingCombat();
            hookingAndTouchedGroundOnce = false;
            if (controller.collisions.below) hookingAndTouchedGroundOnce = true;
        }
    }

    void ProcessHooking()
    {
        if (hooking)
        {
            if (!hookingAndTouchedGroundOnce)
            {
                if (controller.collisions.below) hookingAndTouchedGroundOnce = true;
            }
            else if (currentMaxMoveSpeed > maxHookingSpeed)
            {
                currentMaxMoveSpeed = maxHookingSpeed;
            }
        }
    }

    public void StopHooking()
    {
        if (hooking)
        {
            hooking = false;
        }
    }

    void ProcessAiming()
    {
        if (!aimingAndTouchedGroundOnce)
        {
            if (controller.collisions.below)
            {
                aimingAndTouchedGroundOnce = true;
            }
        }
        else if (myPlayerCombatNew.aiming && currentMaxMoveSpeed > maxAimingSpeed)
        {
            currentMaxMoveSpeed = maxAimingSpeed;
        }
    }
    #endregion

    #region PICKUP / FLAG / DEATH ---------------------------------------------

    public void PutOnFlag(Flag _flag)
    {
        flag = _flag;
        flag.transform.SetParent(rotateObj);
        flag.transform.localPosition = new Vector3(0, 0, -0.5f);
        flag.transform.localRotation = Quaternion.Euler(0, -135, 0);
        HideFlagFlow();
        StopBoost();
    }

    public void LoseFlag()
    {
        haveFlag = false;
        flag = null;
        ShowFlagFlow();
        (gC as GameController_FlagMode).HideFlagHomeLightBeam(team);
    }

    //En desuso
    public void Die()
    {
        if (haveFlag)
        {
            flag.SetAway(false);
        }
        gC.RespawnPlayer(this);
    }
    #endregion

    #region  WATER ---------------------------------------------

    public void EnterWater(Collider waterTrigger = null)
    {
        if (!inWater)
        {
            myPlayerAnimation_01.enterWater = true;
            inWater = true;
            jumpedOutOfWater = false;
            maxTimePressingJump = 0f;
            myPlayerWeap.AttachWeaponToBack();
            if (haveFlag)
            {
                //gC.RespawnFlag(flag.GetComponent<Flag>());
                flag.SetAway(false);
            }
            //Desactivar al jugadro si se esta en la prorroga.
            if (gC.gameMode == GameMode.CaptureTheFlag)
            {
                (gC as GameController_FlagMode).myScoreManager.PlayerEliminado();
            }
            myPlayerVFX.ActivateEffect(PlayerVFXType.SwimmingEffect);
            if (waterTrigger != null)
            {
                Vector3 waterSplashPos = myPlayerVFX.GetEffectGO(PlayerVFXType.WaterSplash).transform.position;//.y = waterTrigger.bounds.max.y;
                waterSplashPos.y = waterTrigger.bounds.max.y - 0.5f;
                myPlayerVFX.GetEffectGO(PlayerVFXType.WaterSplash).transform.position = waterSplashPos;
            }
            myPlayerVFX.ActivateEffect(PlayerVFXType.WaterSplash);
            myPlayerCombatNew.StopDoingCombat();
            myPlayerHook.StopHook();
        }
    }

    void ProcessWater()
    {
        if (inWater)
        {
            controller.AroundCollisions();
            if (currentMaxMoveSpeed > maxSpeedInWater)
            {
                currentMaxMoveSpeed = maxSpeedInWater;
            }
        }
    }

    public void ExitWater(Collider waterTrigger = null)
    {
        if (inWater)
        {
            myPlayerAnimation_01.exitWater = true;
            inWater = false;
            myPlayerWeap.AttatchWeapon();
            myPlayerVFX.DeactivateEffect(PlayerVFXType.SwimmingEffect);
            if (waterTrigger != null)
            {
                Vector3 waterSplashPos = myPlayerVFX.GetEffectGO(PlayerVFXType.WaterSplash).transform.position;//.y = waterTrigger.bounds.max.y;
                waterSplashPos.y = waterTrigger.bounds.max.y - 0.5f;
                myPlayerVFX.GetEffectGO(PlayerVFXType.WaterSplash).transform.position = waterSplashPos;
            }
            myPlayerVFX.ActivateEffect(PlayerVFXType.WaterSplash);
        }
    }
    #endregion

    #region  CHECK WIN && GAME OVER ---------------------------------------------
    public void CheckScorePoint(FlagHome flagHome)
    {
        if (haveFlag && team == flagHome.team && this.moveSt != MoveState.Hooked)
        {
            (gC as GameController_FlagMode).ScorePoint(team);
            if (flag != null)
            {
                flag.SetAway(true);
            }
        }
    }

    public void DoGameOver()
    {
        Debug.Log("GAME OVER");
        currentSpeed = 0;
        currentVel = Vector3.zero;
        controller.Move(currentVel * Time.deltaTime);
    }
    #endregion

    #region LIGHT BEAM ---------------------------------------------
    float distanceToFlag;
    void UpdateDistanceToFlag()
    {
        //if (gC.online)
        //{
        //}
        //else
        //{
            distanceToFlag = ((gC as GameController_FlagMode).flags[0].transform.position - transform.position).magnitude;
        //}
    }

    void UpdateFlagLightBeam()
    {
        if (gC.gameMode == GameMode.CaptureTheFlag)
        {
            if (!haveFlag)
            {
                UpdateDistanceToFlag();
                //print("distanceToFlag = " + distanceToFlag);
                if (distanceToFlag >= (gC as GameController_FlagMode).minDistToSeeBeam)
                {
                    ShowFlagLightBeam();
                }
                else
                {
                    StopFlagLightBeam();
                }
            }
            else
            {
                StopFlagLightBeam();
            }
        }
    }

    void ShowFlagLightBeam()
    {
        //print("SHOW FLAG LIGHT BEAM");
        myCamera.myCamera.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("FlagLightBeam");
        //int mask= myCamera.myCamera.GetComponent<Camera>().cullingMask;
        //myCamera.myCamera.GetComponent<Camera>().cullingMask = LayerMask.GetMask();
    }

    void StopFlagLightBeam()
    {
        //print("STOP FLAG LIGHT BEAM");
        myCamera.myCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer("FlagLightBeam"));
    }

    void ShowFlagFlow()
    {
        myCamera.myCamera.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("FlagGlow");
    }

    void HideFlagFlow()
    {
        myCamera.myCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer("FlagGlow"));
    }

    public void ShowFlagHomeLightBeam(Team team)
    {
        switch (team)
        {
            case Team.A:
                myCamera.myCamera.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("BlueLightBeam");
                break;
            case Team.B:
                myCamera.myCamera.GetComponent<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("RedLightBeam");
                break;
        }
    }

    public void HideFlagHomeLightBeam(Team team)
    {
        switch (team)
        {
            case Team.A:
                print("HIDE BLUE TEAM LIGHT BEAM");
                myCamera.myCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer("BlueLightBeam"));
                break;
            case Team.B:
                print("HIDE RED TEAM LIGHT BEAM");
                myCamera.myCamera.GetComponent<Camera>().cullingMask &= ~(1 << LayerMask.NameToLayer("RedLightBeam"));
                break;
        }
    }
    #endregion

    #region  AUXILIAR FUNCTIONS ---------------------------------------------

    public void ResetPlayer()
    {
        ExitWater();
        jumpSt = VerticalMovementState.None;
        myPlayerHook.ResetHook();
        if (haveFlag)
        {
            flag.SetAway(false);
        }
        boostCurrentFuel = boostCapacity;
        //myPlayerWeap.DropWeapon();
        //controller.collisionMask = LayerMask.GetMask("Stage", "WaterFloor", "SpawnWall");
    }

    //void EquipWeaponAtStart()
    //{
    //    //print("EQUIP WEAPON AT START");
    //    switch (team)
    //    {
    //        case Team.A:
    //            //print("EQUIP BLUE WEAPON");
    //            myPlayerWeap.PickupWeapon(gC.startingWeaponA);
    //            break;
    //        case Team.B:
    //            //print("EQUIP RED WEAPON");
    //            myPlayerWeap.PickupWeapon(gC.startingWeaponB);
    //            break;
    //    }
    //}

    public float SignedRelativeAngle(Vector3 referenceForward, Vector3 newDirection, Vector3 referenceUp)
    {
        // the vector perpendicular to referenceForward (90 degrees clockwise)
        // (used to determine if angle is positive or negative)
        Vector3 referenceRight = Vector3.Cross(referenceUp, referenceForward);
        // Get the angle in degrees between 0 and 180
        float angle = Vector3.Angle(newDirection, referenceForward);
        // Determine if the degree value should be negative.  Here, a positive value
        // from the dot product means that our vector is on the right of the reference vector   
        // whereas a negative value means we're on the left.
        float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
        return (sign * angle);//final angle
    }

    public void TeleportPlayer(Vector3 worldPos)
    {
        ExitWater();
        StopBoost();
        StopHooked(0);
        StopHooking();
        StopWallJump();
        transform.position = worldPos;
    }

    Vector3 AngleToVector(float angle)
    {
        angle = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
    }

    public Vector3 RotateVector(float angle, Vector3 vector)
    {
        //rotate angle -90 degrees
        float theta = angle * Mathf.Deg2Rad;
        float cs = Mathf.Cos(theta);
        float sn = Mathf.Sin(theta);
        float px = vector.x * cs - vector.z * sn;
        float py = vector.x * sn + vector.z * cs;
        return new Vector3(px, 0, py).normalized;
    }

    public Vector3 CalculateReflectPoint(float radius, Vector3 _wallNormal, Vector3 circleCenter)//needs wallJumpRadius and wallNormal
    {
        //LEFT OR RIGHT ORIENTATION?
        Vector3 wallDirLeft = Vector3.Cross(_wallNormal, Vector3.down).normalized;
        float ang = Vector3.Angle(wallDirLeft, currentFacingDir);
        float direction = ang > 90 ? -1 : 1;
        //Angle
        float angle = Vector3.Angle(currentFacingDir, _wallNormal);
        if (angle >= 90)
        {
            angle -= 90;
        }
        else
        {
            angle = 90 - angle;
        }
        angle = Mathf.Clamp(angle, wallJumpMinHorizAngle, 90);
        if (direction == -1)
        {
            float complementaryAng = 90 - angle;
            angle += complementaryAng * 2;
        }
        //Debug.LogWarning("ANGLE = " + angle);
        float offsetAngleDir = Vector3.Angle(wallDirLeft, Vector3.forward) > 90 ? -1 : 1;
        float offsetAngle = Vector3.Angle(Vector3.right, wallDirLeft) * offsetAngleDir;
        angle += offsetAngle;
        //CALCULATE CIRCUMFERENCE POINT
        float px = circleCenter.x + (radius * Mathf.Cos(angle * Mathf.Deg2Rad));
        float pz = circleCenter.z + (radius * Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 circumfPoint = new Vector3(px, circleCenter.y, pz);

        //Debug.LogWarning("; circleCenter= " + circleCenter + "; circumfPoint = " + circumfPoint + "; angle = " + angle + "; offsetAngle = " + offsetAngle + "; offsetAngleDir = " + offsetAngleDir
        //+ ";wallDirLeft = " + wallDirLeft);
        Debug.DrawLine(circleCenter, circumfPoint, Color.white, 20);

        return circumfPoint;
    }

    #endregion

    #endregion

    #region ----[ PUN CALLBACKS ]----
    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    online = false;
    //}

    //public void OnPhotonInstantiate(PhotonMessageInfo info)
    //{
    //    if (!base.photonView.IsMine)
    //    {
    //        gC = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameControllerBase>();
    //        team = gC.SetRandomOnlineTeam(PhotonNetwork.CurrentRoom.PlayerCount - 1);
    //        SwitchTeam(team);
    //        myPlayerWeap.SetTeamWeapon(team);
    //        Debug.Log("I'm player " + gameObject.name + " and my team is " + team);
    //    }
    //}
    #endregion

    #region ----[ RPC ]----
    //[PunRPC]
    //void OnlineSetUpPlayer(byte _team)
    //{
    //    team = (Team)_team;
    //}

    //Player lastAttacker_Online;
    //[PunRPC]
    //public void RPC_StartReceiveHit(Vector3 _knockback, byte effect, float _maxTime, byte autocomboIndex, PhotonMessageInfo info)
    //{
    //    //PhotonView phV = PhotonView.Get(this);
    //    Debug.Log("RPC: I RECEIVED A HIT");
    //    if (base.photonView.IsMine)
    //    {
    //        if (!myPlayerCombatNew.invulnerable)
    //        {
    //            //Variable para Ortu
    //            startBeingHitAnimation = true;

    //            if (!disableAllDebugs) print("Receive hit with knockback= " + _knockback + "; effect = " + (EffectType)effect + "; maxtime = " + _maxTime);
    //            myPlayerHook.FinishAutoGrapple();
    //            myPlayerHook.StopHook();
    //            StopWallJump();
    //            StopBoost();
    //            myPlayerCombatNew.StopDoingCombat();
    //            StopImpulse();

    //            //if (sufferingEffect == EffectType.stun)
    //            //{
    //            //    efecto = EffectType.knockdown;
    //            //    _maxTime = AttackEffect.knockdownTime;
    //            //}
    //            #region STUN PROTECTION

    //            if (sufferingEffect == EffectType.softStun && (!lastAttacker_Online.Equals(info.Sender) || (lastAttacker_Online.Equals(info.Sender) && autocomboIndex == 0)))
    //            {
    //                Debug.Log("Stun protection ON");
    //                stunProtectionOn = true;
    //            }

    //            if (!stunProtectionOn)
    //            {
    //                Debug.Log("FULL EFFECT TIME");
    //                effectMaxTime = _maxTime;
    //            }
    //            else
    //            {
    //                Debug.Log("THIRD EFFECT TIME");
    //                effectMaxTime = _maxTime / 3;
    //            }
    //            #endregion

    //            sufferingEffect = (EffectType)effect;
    //            switch ((EffectType)effect)
    //            {
    //                case EffectType.softStun:
    //                    noInput = true;
    //                    break;
    //                //case EffectType.stun:
    //                //    if (disableAllDebugs) Debug.LogError("STUN !!!!!!!!");
    //                //    noInput = true;
    //                //    break;
    //                //case EffectType.knockdown:
    //                //    if (disableAllDebugs) Debug.LogError("KNOCKDOWN !!!!!!!!");
    //                //    noInput = true;
    //                //    //myPlayerCombatNew.StartInvulnerabilty(_maxTime);
    //                //    break;
    //                case EffectType.none:
    //                    break;
    //                default:
    //                    Debug.LogError("Error: cannot have a 'sufferingEffect' of type " + sufferingEffect);
    //                    break;
    //            }

    //            //Debug.Log("_maxTime = " + _maxTime + "; effectMaxTime = " + effectMaxTime);
    //            effectTime = 0;
    //            if (_knockback != Vector3.zero)
    //            {
    //                knockbackDone = false;
    //                _knockback = _knockback / bodyMass;
    //                knockback = _knockback;
    //            }

    //            //Give FLAG
    //            if (haveFlag)
    //            {
    //                flag.DropFlag();
    //            }
    //            lastAttacker_Online = info.Sender;
    //            lastAutocomboIndex = autocomboIndex;
    //            if (!disableAllDebugs) print("Player " + playerNumber + " RECIEVED HIT");
    //            //return true;
    //        }
    //        //else return false;
    //    }
    //    else
    //    {
    //        Debug.Log("I'm not the owner of this player, but he received a hit. Missing code here? VFX? Invulnerabilty?");
    //    }
    //}
    #endregion

    #region ----[ NETWORK FUNCTIONS ]----

    #endregion

    #region ----[ IPUNOBSERVABLE ]----
    #endregion
}
