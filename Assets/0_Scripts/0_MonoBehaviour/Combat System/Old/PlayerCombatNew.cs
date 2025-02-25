﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatNew : MonoBehaviour
{
    //Referencias
    [HideInInspector] public PlayerMovement myPlayerMovement;
    PlayerWeapons myPlayerWeap;
    PlayerHook myHook;
    [HideInInspector] public PlayerHUD myPlayerHUD;
    public Material[] hitboxMats;//0 -> charging; 1-> startup; 2 -> active; 3 -> recovery
    public Transform hitboxesParent;
    [HideInInspector] public Transform weaponEdge;
    [HideInInspector] public Transform weaponHandle;

    [Header(" --- SKILLS ---")]
    [HideInInspector] public WeaponSkill[] equipedWeaponSkills = new WeaponSkill[2];


    [HideInInspector] public WeaponData currentWeapon;
    bool hasWeapon
    {
        get
        {
            return currentWeapon != null && myPlayerWeap.hasWeapon;
        }
    }
    [HideInInspector]
    public bool canDoCombat
    {
        get
        {
            return !aiming && !myPlayerMovement.noInput && !myPlayerMovement.inWater && myPlayerMovement.moveSt != MoveState.Boost && attackStg == AttackPhaseType.ready &&
                !myPlayerMovement.wallJumping && !myPlayerMovement.hooked;
        }
    }

    //INVULNERABILTY
    float invulTime = 0;
    float maxInvulTime = 0;
    [HideInInspector]
    public bool invulnerable = false;

    //ATTACK
    [HideInInspector]
    public AttackPhaseType attackStg = AttackPhaseType.ready;
    float attackTime = 0;
    [HideInInspector]
    public AttackData currentAttack;
    [HideInInspector]
    public ImpulseInfo currentImpulse;
    [HideInInspector]
    public bool landedSinceAttackStarted = false;
    bool currentAttackHasRedirect = false;
    List<Hitbox> hitboxes;

    //Autocombo
    AutocomboData autocombo;
    [HideInInspector] public bool autocomboStarted;
    [HideInInspector] public byte autocomboIndex = 255;
    bool lastAutocomboAttackFinished = false;
    float autocomboTime = 0;
    [HideInInspector] public List<GameObject> currentHitboxes;

    //Parry
    AttackData parry;
    AttackEffect parryEffect;
    [HideInInspector]
    public bool parryStarted = false;
    float parryTimePercentage = 1;
    bool hitParryStarted = false;

    //SKILLS
    int weaponSkillIndex = -1;//can be 0 or 1;
    [HideInInspector]
    public bool weaponSkillStarted
    {
        get
        {
            return weaponSkillIndex >= 0;
        }
    }
    [HideInInspector]
    public WeaponSkill currentWeaponSkill
    {
        get
        {
            if (weaponSkillIndex >= 0)
            {
                return equipedWeaponSkills[weaponSkillIndex];
            }
            else
            {
                return null;
            }
        }
    }
    float skillCurrentTime = 0;

    [HideInInspector]
    public bool aiming;

    [HideInInspector]
    public bool isRotationRestricted
    {
        get
        {
            bool result = false;
            if (attackStg != AttackPhaseType.ready && currentAttack.GetAttackPhase(attackStg).rotationSpeedPercentage < 1)
                result = true;
            return result;
        }
    }

    #region ----[ MONOBEHAVIOUR FUNCTIONS ]----

    #region Awake
    public void KonoAwake()
    {
        myPlayerMovement = GetComponent<PlayerMovement>();
        myPlayerWeap = myPlayerMovement.myPlayerWeap;
        myHook = myPlayerMovement.myPlayerHook;
        myPlayerHUD = myPlayerMovement.myPlayerHUD;

        currentHitboxes = new List<GameObject>();
        attackStg = AttackPhaseType.ready;
        attackTime = 0;
        equipedWeaponSkills = new WeaponSkill[2];

    }
    #endregion

    #region Start
    public void KonoStart()
    {
        //FillMyAttacks();
        //ChangeAttackType(gC.attackX);
        HideAttackHitBox();
        //ChangeNextAttackType();

    }
    #endregion

    #region Update
    public void KonoUpdate()
    {
        //print("Trigger = " + Input.GetAxis(myPlayerMovement.contName + "LT"));

        if (hasWeapon)
        {
            //Autocombo  INPUT CHECK
            if (myPlayerMovement.Actions.X.WasPressed && !myPlayerWeap.canPickupWeapon)//Input.GetButtonDown(myPlayerMovement.contName + "X"))
            {
                StartOrContinueAutocombo();
            }

            //Parry
            if (myPlayerMovement.Actions.L1.WasPressed)
            {
                StartParry();
            }

            //Skill 1
            if (myPlayerMovement.Actions.Y.WasPressed)//Input.GetButtonDown(myPlayerMovement.contName + "Y"))
            {
                StartWeaponSkill(0);
            }

            //Skill 2
            if (myPlayerMovement.Actions.B.WasPressed)//Input.GetButtonDown(myPlayerMovement.contName + "B"))
            {
                StartWeaponSkill(1);
            }

            //HOOK INPUT CHECK
            if (!myPlayerMovement.noInput && !myPlayerMovement.inWater && (attackStg == AttackPhaseType.ready)
                && aiming && myPlayerMovement.Actions.R2.WasPressed)//Input.GetButtonDown(myPlayerMovement.contName + "RB"))
            {
                myHook.StartHook();
                //ChangeAttackType(gC.attackHook);
                //StartAttack();
            }
        }
        ProcessWeaponSkill();
        ProcessAutocombo();
        ProcessInvulnerability();
        ProcessAttack();
        //ProcessAttacksCD();

        if (myPlayerMovement.Actions.L2.WasPressed && myPlayerWeap.hasWeapon)
        {
            StartAiming();
        }
        if (myPlayerMovement.Actions.L2.WasReleased)
        {
            StopAiming();
        }
    }
    #endregion

    #endregion


    #region --- Change Attack / Hitbox ---

    public void ChangeAttackPhase(AttackPhaseType attackStage)
    {
        if (!myPlayerMovement.disableAllDebugs) Debug.Log(myPlayerMovement.gameObject.name + " ->Change attack phase to " + attackStage);
        attackStg = attackStage;

        //change hitbox
        //HideAttackHitBox();
        ChangeHitboxes(attackStg);
        if (attackStg != AttackPhaseType.ready && currentAttack.GetAttackPhase(attackStg).invulnerability) StartInvulnerability(currentAttack.GetAttackPhase(attackStg).duration);

        switch (attackStg)
        {
            case AttackPhaseType.ready:
                myPlayerMovement.ResetPlayerRotationSpeed();
                myPlayerMovement.ResetPlayerAttackMovementSpeed();
                currentImpulse = new ImpulseInfo();
                currentAttack = null;
                currentAttackHasRedirect = false;
                break;
            case AttackPhaseType.charging:
                if (currentAttack.chargingPhase.rotationSpeedPercentage < 1) myPlayerMovement.SetPlayerRotationSpeed(currentAttack.chargingPhase.rotationSpeedPercentage);
                if (currentAttack.chargingPhase.movementSpeedPercentage < 1) myPlayerMovement.SetPlayerAttackMovementSpeed(currentAttack.chargingPhase.movementSpeedPercentage);
                break;
            case AttackPhaseType.startup:
                if (currentAttack.startupPhase.rotationSpeedPercentage < 1) myPlayerMovement.SetPlayerRotationSpeed(currentAttack.startupPhase.rotationSpeedPercentage);
                if (currentAttack.startupPhase.movementSpeedPercentage < 1) myPlayerMovement.SetPlayerAttackMovementSpeed(currentAttack.startupPhase.movementSpeedPercentage);
                if (currentAttackHasRedirect) CalculateImpulse(currentAttack);
                break;
            case AttackPhaseType.active:
                if (currentAttack.activePhase.rotationSpeedPercentage < 1) myPlayerMovement.SetPlayerRotationSpeed(currentAttack.activePhase.rotationSpeedPercentage);
                if (currentAttack.activePhase.movementSpeedPercentage < 1) myPlayerMovement.SetPlayerAttackMovementSpeed(currentAttack.activePhase.movementSpeedPercentage);

                //WeaponTrails
                myPlayerMovement.myPlayerVFX.ActivateWeaponTrails();

                //Do impulse
                if (!currentAttackHasRedirect) CalculateImpulse(currentAttack);
                myPlayerMovement.StartImpulse(currentImpulse);
                break;
            case AttackPhaseType.recovery:
                if (currentAttack.recoveryPhase.rotationSpeedPercentage < 1) myPlayerMovement.SetPlayerRotationSpeed(currentAttack.recoveryPhase.rotationSpeedPercentage);
                if (currentAttack.recoveryPhase.movementSpeedPercentage < 1) myPlayerMovement.SetPlayerAttackMovementSpeed(currentAttack.recoveryPhase.movementSpeedPercentage);

                //WeaponTrails
                myPlayerMovement.myPlayerVFX.DeactivateWeaponTrails();
                break;
        }
    }

    void ChangeHitboxes(AttackPhaseType att)//hacer después de cambiar de attack stage, no antes!
    {
        //HideAttackHitBox();
        if (!myPlayerMovement.disableAllDebugs) Debug.Log(myPlayerMovement.gameObject.name + " ->Changing Hitboxes!");
        switch (attackStg)
        {
            case AttackPhaseType.ready:
                HideAttackHitBox();
                break;
            case AttackPhaseType.charging:
                hitboxes = new List<Hitbox>();
                for (int i = 0; i < currentAttack.activePhase.attackHitboxes.Length; i++)
                {
                    AddHitbox(currentAttack.activePhase.attackHitboxes[i]);
                }
                for (int i = 0; i < currentHitboxes.Count; i++)
                {
                    currentHitboxes[i].GetComponent<MeshRenderer>().material = hitboxMats[0];
                }
                break;
            case AttackPhaseType.startup:
                if (!currentAttack.hasChargingPhase)
                {
                    hitboxes = new List<Hitbox>();
                    for (int i = 0; i < currentAttack.activePhase.attackHitboxes.Length; i++)
                    {
                        AddHitbox(currentAttack.activePhase.attackHitboxes[i]);
                    }
                }
                for (int i = 0; i < currentHitboxes.Count; i++)
                {
                    currentHitboxes[i].GetComponent<MeshRenderer>().material = hitboxMats[1];
                }
                break;
            case AttackPhaseType.active:
                for (int i = 0; i < currentHitboxes.Count; i++)
                {
                    currentHitboxes[i].GetComponent<MeshRenderer>().material = hitboxMats[2];
                }
                break;
            case AttackPhaseType.recovery:
                for (int i = 0; i < currentHitboxes.Count; i++)
                {
                    currentHitboxes[i].GetComponent<MeshRenderer>().material = hitboxMats[3];
                }
                break;
        }
    }

    void AddHitbox(AttackHitbox attackHitbox)
    {
        if (!myPlayerMovement.disableAllDebugs) Debug.Log(myPlayerMovement.gameObject.name + " ->Adding hitbox of parent type " + attackHitbox.parentType);
        GameObject auxHitbox = null;
        switch (attackHitbox.parentType)
        {
            case HitboxParentType.player:
                auxHitbox = Instantiate(attackHitbox.hitboxPrefab, hitboxesParent);
                break;
            case HitboxParentType.weaponEdge:
                auxHitbox = Instantiate(attackHitbox.hitboxPrefab, weaponEdge);
                break;
            case HitboxParentType.weaponHandle:
                auxHitbox = Instantiate(attackHitbox.hitboxPrefab, weaponHandle);
                break;
            case HitboxParentType.player_localParent:
                auxHitbox = Instantiate(attackHitbox.hitboxPrefab, hitboxesParent);
                auxHitbox = auxHitbox.GetComponentInChildren<Hitbox>().gameObject;
                break;
            case HitboxParentType.player_followTransform:
                auxHitbox = Instantiate(attackHitbox.hitboxPrefab, hitboxesParent);
                //auxHitbox = auxHitbox.GetComponentInChildren<Hitbox>().gameObject;
                Transform follTrans = null;
                if (weaponSkillStarted)
                {
                    switch (currentWeaponSkill.myWeaponSkillData.skillName)
                    {
                        case "QTip_Cannon_Skill"://Q-Tip_Cannon
                            follTrans = currentHitboxes[0].GetComponent<Hitbox>().referencePos1;
                            break;
                    }
                }
                auxHitbox.GetComponent<FollowTransform>().followTransform = follTrans;
                break;
            default:
                Debug.LogError("Error: the hitboxParentType is not supported: " + attackHitbox.parentType);
                break;
        }
        //if(attackHitbox.parentType!= HitboxParentType.player_animated)
        //{
        if(auxHitbox != null)
        {
            auxHitbox.GetComponent<Hitbox>().KonoAwake();

            if (!myPlayerMovement.disableAllDebugs) Debug.Log(myPlayerMovement.gameObject.name + " ->CurrentHitboxes ADD -> " + auxHitbox.name);
            currentHitboxes.Add(auxHitbox);
            Hitbox hb = auxHitbox.GetComponent<Hitbox>();
            hb.myAttackHitbox = attackHitbox;
            hitboxes.Add(hb);
        }
    }

    void HideAttackHitBox()
    {
        hitboxes = new List<Hitbox>();
        for (int i = 0; i < 100 && currentHitboxes.Count > 0; i++)
        {
            Destroy(currentHitboxes[0]);
            currentHitboxes.RemoveAt(0);
            if (i == 99) Debug.LogError("Error: Trying to hide Hitboxes but we are hiding too many??(99). This was done to avoid an infinite loop and game crash.");
        }
        if (hitboxesParent.childCount > 0)
        {
            for (int i = 0; i < hitboxesParent.childCount; i++)
            {
                Destroy(hitboxesParent.GetChild(i).gameObject);
            }
        }
    }

    #endregion

    #region --- IMPULSE ---
    void CalculateImpulse(AttackData attack)
    {
        if (currentAttack.impulseDistance != 0)
        {
            Vector3 impulseDir = Vector3.zero;
            if (myPlayerMovement.currentInputDir != Vector3.zero)
            {
                float angleWithJoysitck = myPlayerMovement.SignedRelativeAngle(myPlayerMovement.rotateObj.forward, myPlayerMovement.currentInputDir, Vector3.up);
                angleWithJoysitck = Mathf.Clamp(angleWithJoysitck, -myPlayerMovement.maxImpulseAngle, myPlayerMovement.maxImpulseAngle);
                impulseDir = Quaternion.Euler(0, angleWithJoysitck, 0) * myPlayerMovement.rotateObj.forward;
            }
            else
            {
                impulseDir = myPlayerMovement.rotateObj.forward;
            }
            float impulseTime = currentAttack.activePhase.duration;
            //float acceleration = myPlayerMovement.breakAcc;
            //v=(2*d)/t; InitialSpeed =0;
            float InitialSpeed = (2 * currentAttack.impulseDistance) / impulseTime;
            // a = v/t;
            float acceleration = -(InitialSpeed / impulseTime);
            //float realInitialSpeed = myPlayerMovement.currentMaxMoveSpeed;
            //realInitialSpeed += InitialSpeed;
            //currentImpulse = impulseDir.normalized * realFinalSpeed;
            currentImpulse = new ImpulseInfo(impulseDir.normalized, currentAttack.impulseDistance, InitialSpeed, impulseTime, acceleration, Vector3.zero);
        }
        else
        {
            currentImpulse = new ImpulseInfo(Vector3.zero, 0, 0, 0, 0, Vector3.zero);
        }
    }

    #endregion

    #region --- AUTOCOMBO ---

    bool StartAutocombo()
    {
        bool exito = false;
        if (!autocomboStarted && canDoCombat)
        {
            if (!myPlayerMovement.disableAllDebugs) Debug.Log("Autocombo Started");
            exito = true;
            autocomboStarted = true;
            lastAutocomboAttackFinished = true;
            autocomboTime = 0;
            autocomboIndex = 255;
            StartNextAttackAutocombo();
        }
        return exito;
    }

    bool StartNextAttackAutocombo()
    {
        bool exito = false;
        if (autocomboStarted && canDoCombat)
        {
            exito = true;
            autocomboIndex++;
            lastAutocomboAttackFinished = false;
            StartAttack(autocombo.attacks[autocomboIndex]);
        }
        return exito;
    }

    public bool StartOrContinueAutocombo(bool calledFromBuffer = false)
    {
        if (!myPlayerMovement.disableAllDebugs) Debug.Log("Autocombo Start or Continue input. calledFromBuffer = " + calledFromBuffer + "; autocomboStarted = " + autocomboStarted);

        bool result = false;

        if (!autocomboStarted)
        {
            result = StartAutocombo();
        }
        else
        {
            result = StartNextAttackAutocombo();
        }

        if (!result && !calledFromBuffer)
        {
            if (!myPlayerMovement.disableAllDebugs) Debug.Log("Autocombo Input was BUFFERED");
            myPlayerMovement.BufferInput(PlayerInput.Autocombo);
        }
        if (!myPlayerMovement.disableAllDebugs) Debug.Log("StartOrContinueAutocombo result = " + result);
        return result;
    }

    void ProcessAutocombo()
    {
        if (autocomboStarted)
        {
            if (aiming)
            {
                StopAutocombo();
            }
            if (lastAutocomboAttackFinished)
            {
                autocomboTime += Time.deltaTime;
                if (autocomboTime >= autocombo.maxTimeBetweenAttacks)
                {
                    StopAutocombo();
                }
            }
        }
    }

    void StopAutocombo()
    {
        if (autocomboStarted)
        {
            if (!myPlayerMovement.disableAllDebugs) Debug.LogWarning("Autocombo Stopped");
            if (attackStg != AttackPhaseType.ready)
            {
                if (!myPlayerMovement.disableAllDebugs) Debug.LogWarning("PARAMOS EL ATAQUE PORQUE NOS HAN PEGAO O ALGO");
                EndAttack();
            }
            autocomboStarted = false;
            lastAutocomboAttackFinished = false;
            autocomboTime = 0;
            autocomboIndex = 255;
        }
    }

    #endregion

    #region --- ATTACK ---
    public void StartAttack(AttackData newAttack)
    {
        if (canDoCombat)
        {
            if (!myPlayerMovement.disableAllDebugs) Debug.Log(myPlayerMovement.gameObject.name+" -> Starting Attack " + newAttack.attackName);
            landedSinceAttackStarted = myPlayerMovement.controller.collisions.below ? true : false;
            currentAttack = newAttack;
            //targetsHit.Clear();
            attackTime = 0;
            attackStg = currentAttack.hasChargingPhase ? AttackPhaseType.charging : AttackPhaseType.startup;
            bool found = false;
            for (int i = 0; i < currentAttack.activePhase.attackHitboxes.Length && !found; i++)
            {
                for (int j = 0; j < currentAttack.activePhase.attackHitboxes[i].effects.Length && !found; j++)
                {
                    if (currentAttack.activePhase.attackHitboxes[i].effects[j].effectType == EffectType.knockback &&
                        currentAttack.activePhase.attackHitboxes[i].effects[j].knockbackType == KnockbackType.redirect)
                    {
                        currentAttackHasRedirect = true;
                    }
                }
            }
            ChangeAttackPhase(attackStg);
        }
    }

    void ProcessAttack()
    {
        if (attackStg != AttackPhaseType.ready)
        {
            if (!landedSinceAttackStarted && myPlayerMovement.controller.collisions.below) landedSinceAttackStarted = true;
            attackTime += Time.deltaTime;
            switch (attackStg)
            {
                case AttackPhaseType.charging:
                    if (attackTime >= currentAttack.chargingPhase.duration)
                    {
                        float charge = Mathf.Clamp01(attackTime / currentAttack.chargingPhase.duration);
                        attackTime = 0;
                        ChangeAttackPhase(AttackPhaseType.startup);
                    }
                    break;
                case AttackPhaseType.startup:
                    //animacion startup
                    if (attackTime >= currentAttack.startupPhase.duration)
                    {
                        attackTime = 0;
                        ChangeAttackPhase(AttackPhaseType.active);
                    }
                    break;
                case AttackPhaseType.active:
                    switch (currentAttack.activePhase.phaseCompletionType)
                    {
                        case PhaseCompletionType.time:
                            if (attackTime >= currentAttack.activePhase.duration)
                            {
                                attackTime = 0;
                                ChangeAttackPhase(AttackPhaseType.recovery);
                            }
                            break;
                        case PhaseCompletionType.none:
                            break;
                    }
                    break;
                case AttackPhaseType.recovery:
                    float recoveryTime = currentAttack.recoveryPhase.duration * parryTimePercentage;
                    if (attackTime >= recoveryTime)
                    {
                        EndAttack();
                    }
                    break;
            }
        }
    }

    public void EndAttack()
    {
        if (attackStg != AttackPhaseType.ready)
        {
            attackTime = 0;
            landedSinceAttackStarted = false;
            ChangeAttackPhase(AttackPhaseType.ready);
            if (autocomboStarted)
            {
                lastAutocomboAttackFinished = true;
                if (autocomboIndex + 1 >= autocombo.attacks.Length)
                {
                    StopAutocombo();
                }
            }
            StopParry();
            StopHitParry();

            //WeaponTrails
            myPlayerMovement.myPlayerVFX.DeactivateWeaponTrails();

            //myAttacks[attackIndex].StartCD();
        }
    }
    #endregion

    #region --- PARRY ---

    void StartParry()
    {
        if (!parryStarted && canDoCombat)
        {
            parryStarted = true;
            StartAttack(parry);
        }
    }

    //EN DESUSO
    void ProcessParry()
    {
        if (parryStarted)
        {

        }
    }

    void StopParry()
    {
        if (parryStarted)
        {
            parryStarted = false;
            EndAttack();
        }
    }

    public void HitParry()
    {
        if (!hitParryStarted)
        {
            hitParryStarted = true;
            parryTimePercentage = parryEffect.parryRecoveryTime;
        }
    }

    public void StopHitParry()
    {
        if (hitParryStarted)
        {
            hitParryStarted = false;
            parryTimePercentage = 1;
        }
    }

    #endregion

    #region --- INVULNERABILTY ---
    public void StartInvulnerability(float maxTime)
    {
        Debug.LogWarning("START INVULNERABILITY");
        float missingInvulTime = 0;
        if (invulnerable) missingInvulTime = maxInvulTime - invulTime;

        if (!invulnerable || (invulnerable && maxTime > (missingInvulTime)))
        {
            invulnerable = true;
            invulTime = 0;
            maxInvulTime = maxTime;
        }
    }

    void ProcessInvulnerability()
    {
        invulTime += Time.deltaTime;
        if (invulTime >= maxInvulTime)
        {
            StopInvulnerability();
        }
    }

    void StopInvulnerability()
    {
        invulTime = 0;
        maxInvulTime = 0;
        invulnerable = false;
    }
    #endregion

    #region --- SKILLS ---
    public void StartWeaponSkill(int _weaponSkillIndex)
    {
        if (canDoCombat && !weaponSkillStarted && equipedWeaponSkills[_weaponSkillIndex].weaponSkillSt == WeaponSkillState.ready)
        {
            StopDoingCombat();
            weaponSkillIndex = _weaponSkillIndex;
            skillCurrentTime = 0;
            if(!myPlayerMovement.disableAllDebugs)Debug.Log("Skill " + currentWeaponSkill.myWeaponSkillData.skillName + " started!");
            switch (currentWeaponSkill.myWeaponSkillData.weaponSkillType)
            {
                case WeaponSkillType.attack:
                    currentWeaponSkill.KonoAwake();
                    currentWeaponSkill.StartSkill();
                    break;
                case WeaponSkillType.attack_extend:
                    (currentWeaponSkill as WeaponSkill_AttackExtend).KonoAwake();
                    (currentWeaponSkill as WeaponSkill_AttackExtend).StartSkill();
                    break;
            }
        }
        else if (weaponSkillStarted && currentWeaponSkill.myWeaponSkillData.pressAgainToStopSkill && skillCurrentTime >= 0.3f)
        {
            if (!myPlayerMovement.disableAllDebugs) Debug.LogError("SKILL Y STOPPED!");
            StopWeaponSkill();
        }
        else//CANT DO SKILL
        {
            myPlayerHUD.StartSkillCantDoAnim(_weaponSkillIndex);
        }
    }

    void ProcessWeaponSkill()
    {
        for (int i = 0; i < equipedWeaponSkills.Length; i++)
        {
            if (weaponSkillStarted)
            {
                skillCurrentTime += Time.deltaTime;
            }
            //Debug.Log("PROCESS WEAPON SKILL: length = " + equipedWeaponSkills.Length + "; i = " + i);
            if (equipedWeaponSkills[i].weaponSkillSt != WeaponSkillState.ready)
            {
                equipedWeaponSkills[i].KonoUpdate();
            }
        }
    }

    public void StopWeaponSkill()
    {
        if (weaponSkillStarted)
        {

            switch (currentWeaponSkill.myWeaponSkillData.weaponSkillType)
            {
                case WeaponSkillType.attack:
                    WeaponSkill auxWeapSkill = currentWeaponSkill;
                    weaponSkillIndex = -1;
                    auxWeapSkill.StopSkill();
                    break;
                case WeaponSkillType.attack_extend:
                    if ((currentWeaponSkill as WeaponSkill_AttackExtend).attackExtendStg != AttackExtendStage.finished)
                    {
                        (currentWeaponSkill as WeaponSkill_AttackExtend).StartRetracting();
                    }
                    else
                    {
                        (currentWeaponSkill as WeaponSkill_AttackExtend).attackExtendStg = AttackExtendStage.notStarted;
                        auxWeapSkill = currentWeaponSkill;
                        weaponSkillIndex = -1;
                        (auxWeapSkill as WeaponSkill_AttackExtend).StopSkill();
                    }
                    break;
            }
        }
    }

    #endregion

    public void InitializeCombatSystem(WeaponData weaponData)
    {
        weaponEdge = myPlayerWeap.currentWeapon.weaponEdge;
        weaponHandle = myPlayerWeap.currentWeapon.weaponHandle;

        currentWeapon = weaponData;
        autocombo = currentWeapon.autocombo;

        //Parry
        parry = currentWeapon.parry;
        bool found = false;
        for (int i = 0; i < parry.activePhase.attackHitboxes.Length && !found; i++)
        {
            for (int j = 0; j < parry.activePhase.attackHitboxes[i].effects.Length && !found; j++)
            {
                if (parry.activePhase.attackHitboxes[i].effects[j].effectType == EffectType.parry)
                {
                    parryEffect = parry.activePhase.attackHitboxes[i].effects[j];
                    found = true;
                }
            }
        }

        //Skills
        for (int i = 0; i < currentWeapon.allWeaponSkills.Length; i++)
        {
            switch (currentWeapon.allWeaponSkills[i].weaponSkillType)
            {
                case WeaponSkillType.attack:
                    equipedWeaponSkills[i] = new WeaponSkill(this, currentWeapon.allWeaponSkills[i]);
                    break;
                case WeaponSkillType.attack_extend:
                    equipedWeaponSkills[i] = new WeaponSkill_AttackExtend(this, currentWeapon.allWeaponSkills[i]);
                    break;
            }
            equipedWeaponSkills[i].skillIndex = i;
        }

        myPlayerHUD.SetupSkillsHUD(equipedWeaponSkills);
    }

    public void DropWeapon()
    {
        weaponEdge = null;
        weaponHandle = null;

        currentWeapon = null;
        autocombo = null;
        parry = null;

        //Skill 1

        //Skill 2
    }

    public void StopDoingCombat()
    {
        StopAutocombo();
        StopParry();
        StopWeaponSkill();
    }

    public void StartAiming()
    {
        if (!aiming && attackStg == AttackPhaseType.ready && !myPlayerMovement.noInput)
        {
            aiming = true;
            myPlayerMovement.myCamera.SwitchCamera(CameraMode.Shoulder);
            myPlayerWeap.AttachWeaponToBack();
            myPlayerHUD.StartAim();
            //ChangeAttackType(GameController.instance.attackHook);
            myPlayerMovement.aimingAndTouchedGroundOnce = false;
            if (myPlayerMovement.controller.collisions.below) myPlayerMovement.aimingAndTouchedGroundOnce = true;
        }
    }

    public void StopAiming()
    {
        if (aiming)
        {
            aiming = false;
            myPlayerMovement.myCamera.SwitchCamera(CameraMode.Free);
            myPlayerWeap.AttatchWeapon();
            myPlayerHUD.StopAim();
        }
    }
}
#region ----[ STRUCTS & CLASSES ]----
public class ImpulseInfo
{
    public Vector3 impulseVel;
    public Vector3 impulseDir;
    public Vector3 initialPlayerPos;
    public float impulseInitialSpeed;
    public float impulseMaxTime;
    public float impulseAcc;
    public float impulseDistance;

    //public float missingImpulseDistance;

    public ImpulseInfo()
    {
        impulseVel = Vector3.zero;
        impulseDir = Vector3.zero;
        impulseInitialSpeed = 0;
        impulseMaxTime = 0;
        impulseAcc = 0;
        impulseDistance = 0;
        //missingImpulseDistance = 0;
    }

    public ImpulseInfo(Vector3 _impulseDir, float _impulseDistance, float _impulseInitialSpeed, float _impulseMaxTime, float _impulseAcc, Vector3 _initialPlayerPos)
    {
        impulseDir = _impulseDir.normalized;
        impulseDistance = _impulseDistance;
        impulseInitialSpeed = _impulseInitialSpeed;
        impulseMaxTime = _impulseMaxTime;
        impulseAcc = _impulseAcc;
        impulseVel = impulseDir * impulseInitialSpeed;
        initialPlayerPos = _initialPlayerPos;
    }

    public void RecalculateImpulseParameters(float newInitialSpeed, float breakAcc)
    {
        impulseInitialSpeed = newInitialSpeed;
        impulseDistance = (impulseInitialSpeed * impulseMaxTime) * ((breakAcc * Mathf.Pow(impulseMaxTime, 2)) / 2);
        impulseDistance = -impulseDistance;
        impulseVel = impulseVel.normalized * impulseInitialSpeed;
    }

    public float CalculateMissingDistance(Vector3 currentPos)
    {
        if (currentPos != Vector3.zero)
        {
            currentPos.y = 0;
            initialPlayerPos.y = 0;
            return impulseDistance - (currentPos - initialPlayerPos).magnitude;
        }
        else
        {
            Debug.LogError("currentPos should not be == Vector.zero");
            return 0;
        }
    }
}
#endregion
