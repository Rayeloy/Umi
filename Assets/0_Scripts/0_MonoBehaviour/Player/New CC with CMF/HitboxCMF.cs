﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxCMF : MonoBehaviour
{
    [HideInInspector]
    public PlayerMovementCMF myPlayerMov;
    [HideInInspector]
    public PlayerCombatCMF myPlayerCombat;
    [HideInInspector]
    public AttackHitbox myAttackHitbox;

    public Transform referencePos1;

    List<string> targetsHit;
    List<string> dummiesHit;
    List<string> targetsHitWait1Frame;

    private void Awake()
    {
        myPlayerMov = transform.GetComponentsInParent<PlayerMovementCMF>()[0];
        myPlayerCombat = transform.GetComponentsInParent<PlayerCombatCMF>()[0];
        if (myPlayerCombat.hitboxDebugsOn) Debug.Log(myPlayerMov.gameObject.name + " ->I'm hitbox " + name + " and I'm doing Awake");
        targetsHit = new List<string>();
        dummiesHit = new List<string>();
        targetsHitWait1Frame = new List<string>();
    }

    private void OnTriggerEnter(Collider col)
    {
        if (myPlayerCombat.hitboxDebugsOn) Debug.Log("HITBOX COLLIDED WITH " + col.name);
        if (col.gameObject != myPlayerMov.gameObject)
        {
            if (myPlayerCombat.weaponSkillStarted && myPlayerCombat.currentWeaponSkill.myWeaponSkillData.weaponSkillType == WeaponSkillType.attack_extend)
            {
                if ((myPlayerCombat.currentWeaponSkill as WeaponSkillCMF_AttackExtend).attackExtendStg == AttackExtendStage.extending)
                {
                    if (col.tag == "Stage")//|| col.tag == "Player"
                    {
                        (myPlayerCombat.currentWeaponSkill as WeaponSkillCMF_AttackExtend).StartRetracting();
                    }
                }
            }
        }
    }

    private void OnTriggerStay(Collider col)
    {
        //if (myPlayerCombat.hitboxDebugsOn) Debug.Log("Hitbox 1: I'm " + myPlayerMov.gameObject.name + " and I collided with " + col.gameObject);
        if (col.gameObject != myPlayerMov.gameObject)
        {
            if (myPlayerCombat.attackStg == AttackPhaseType.active)//(tag != "HookBigHB" && tag != "HookSmallHB") && 
            {
                if (myPlayerCombat.hitboxDebugsOn) Debug.Log("Hitbox 1.5: I'm " + myPlayerMov.gameObject.name + " and I collided with " + col.gameObject);
                Vector3 resultKnockback = Vector3.zero;
                EffectType stunLikeEffect = EffectType.none;
                float maxStunTime = 0;
                bool encontrado = false;
                switch (col.tag)
                {
                    #region --- PLAYER ---
                    case "PlayerBody":

                        PlayerMovementCMF otherPlayer = col.GetComponent<PlayerBodyCMF>().myPlayerMov;
                        if (myPlayerMov.team != otherPlayer.team && !myPlayerCombat.parryStarted)
                        {
                            encontrado = false;
                            for (int i = 0; i < targetsHit.Count && !encontrado; i++)
                            {
                                if (targetsHit[i] == otherPlayer.name)
                                {
                                    encontrado = true;
                                }
                            }
                            if (encontrado)
                            {
                                if (myPlayerCombat.hitboxDebugsOn) Debug.Log("I'm " + myPlayerMov.name + " and " + otherPlayer.name + " is already added to our list of enemies, so we don't attack him.");
                                return;
                            }

                            if (otherPlayer.myPlayerCombat.attackStg == AttackPhaseType.active && otherPlayer.myPlayerCombat.currentAttack.HasEffect(EffectType.parry) &&
                                !targetsHitWait1Frame.Contains(otherPlayer.name) /*&& !myPlayerMov.online*/)
                            {
                                targetsHitWait1Frame.Add(otherPlayer.name);
                            }
                            else
                            {
                                bool ignoreMass = false;
                                //What type of Effect?
                                for (int i = 0; i < myAttackHitbox.effects.Length; i++)
                                {
                                    if (stunLikeEffect != EffectType.parry)//Are we already giving effect parry? continue if no
                                    {
                                        //targetsHit.Add(otherPlayer.playerNumber);

                                        #region EFFECT TYPE SWITCH
                                        switch (myAttackHitbox.effects[i].effectType)
                                        {
                                            case EffectType.knockback:
                                                //calculate knockback vector
                                                if (!myPlayerMov.disableAllDebugs) Debug.Log("KNOCKBACK TYPE= " + myAttackHitbox.effects[i].knockbackType);
                                                switch (myAttackHitbox.effects[i].knockbackType)
                                                {
                                                    case KnockbackType.outwards:
                                                        Vector3 myPos = myPlayerMov.transform.position;
                                                        Vector3 colPos = col.transform.position;
                                                        resultKnockback = new Vector3(colPos.x - myPos.x, 0, colPos.z - myPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                    case KnockbackType.inwards:
                                                        myPos = myPlayerMov.transform.position;
                                                        colPos = col.transform.position;
                                                        resultKnockback = new Vector3(myPos.x - colPos.x, 0, myPos.z - colPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                    case KnockbackType.customDir:
                                                        //calculate real direction based on character's facing direction
                                                        float facingAngle = -myPlayerMov.facingAngle;
                                                        Vector3 customDir = myAttackHitbox.effects[i].knockbackDir;

                                                        float theta = facingAngle * Mathf.Deg2Rad;
                                                        float cs = Mathf.Cos(theta);
                                                        float sn = Mathf.Sin(theta);
                                                        float px = customDir.x * cs - customDir.z * sn;
                                                        float py = customDir.x * sn + customDir.z * cs;
                                                        resultKnockback = new Vector3(px, customDir.y, py).normalized;
                                                        resultKnockback = resultKnockback * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        //print("Facing Angle(localRot.y)= " + facingAngle + "; customDir = " + customDir);
                                                        break;
                                                    case KnockbackType.autoCenter:
                                                        ignoreMass = true;
                                                        float a = Mathf.Abs(myPlayerMov.breakAcc);
                                                        //float iT = myPlayerMov.MissingImpulseTime();
                                                        //float impulseDist = (a * Mathf.Pow(iT, 2)) / 2;
                                                        float impulseDist = myPlayerMov.currentImpulse.CalculateMissingDistance(myPlayerMov.transform.position);
                                                        /*if (!myPlayerMov.disableAllDebugs)*/ Debug.LogWarning("impulseDist = " + impulseDist);

                                                        Vector3 hitDir = /*myPlayerMov.currentImpulse.impulseInitialSpeed != 0 ? myPlayerMov.currentImpulse.impulseDir : */myPlayerMov.rotateObj.forward;
                                                        float meNoMaeDist = myAttackHitbox.effects[i].knockbackMagnitude + impulseDist;
                                                        Vector3 meNoMaePos = myPlayerMov.rotateObj.position + (hitDir * meNoMaeDist);//me no mae (目の前) means in front of your eyes
                                                        resultKnockback = (meNoMaePos - otherPlayer.transform.position);
                                                        resultKnockback.y = 0;
                                                        Debug.DrawLine(otherPlayer.transform.position, (meNoMaePos), Color.red, 4);
                                                        /*if (!myPlayerMov.disableAllDebugs)*/ Debug.LogWarning("hitDir = " + hitDir + "; meNoMaeDist = " + meNoMaeDist + "; meNoMaePos = " + meNoMaePos);

                                                        float d = resultKnockback.magnitude;
                                                        //vi = Mathf.Sqrt(2*a*d);
                                                        float vi = Mathf.Sqrt((2 * a * d));
                                                        if (!myPlayerMov.disableAllDebugs) Debug.LogWarning("distance = " + d.ToString("F4") + "; Initial velocity = " + vi.ToString("F4") +
                                                            "; myPlayerMov.breakAcc = " + a);
                                                        resultKnockback = resultKnockback.normalized * vi;
                                                        break;
                                                    case KnockbackType.redirect:
                                                        float inputRedirectAngle = SignedRelativeAngle(myPlayerMov.rotateObj.forward, myPlayerMov.currentInputDir, Vector3.up);
                                                        float finalRedirectAngle = Mathf.Clamp(inputRedirectAngle, -myAttackHitbox.effects[i].redirectMaxAngle, myAttackHitbox.effects[i].redirectMaxAngle);
                                                        resultKnockback = Quaternion.Euler(0, finalRedirectAngle, 0) * myPlayerMov.rotateObj.forward;
                                                        Debug.Log("inputRedirectAngle = "+ inputRedirectAngle + "; myPlayerMov.currentInputDir = "+ myPlayerMov.currentInputDir+"; foward = "+ myPlayerMov.rotateObj.forward+
                                                            "; finalRedirectAngle = " + finalRedirectAngle + "; resultKnockback = " + resultKnockback);
                                                        //CALCULATE Y ANGLE
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        //resultKnockback *= myAttackHitbox.effects[i].knockbackMagnitude;
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        if (!myPlayerMov.disableAllDebugs)
                                                        {
                                                            //Debug.LogError("myPlayerMov.currentInputDir = " + myPlayerMov.currentInputDir+ "; resultKnockback = " + resultKnockback);
                                                            Debug.DrawRay(myPlayerMov.transform.position, resultKnockback.normalized * 1, Color.red);
                                                        }
                                                        break;
                                                    case KnockbackType.outwardsFromHitbox:
                                                        myPos = transform.position;
                                                        colPos = col.transform.position;
                                                        resultKnockback = new Vector3(colPos.x - myPos.x, 0, colPos.z - myPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                }
                                                if (!myPlayerMov.disableAllDebugs) print("KNOCKBACK RESULT= " + resultKnockback);
                                                break;
                                            case EffectType.softStun:
                                                //if (stunLikeEffect != EffectType.stun)
                                                //{
                                                stunLikeEffect = EffectType.softStun;
                                                maxStunTime = myAttackHitbox.effects[i].stunTime;
                                                //}
                                                break;
                                                //case EffectType.stun:
                                                //    if (stunLikeEffect != EffectType.knockdown)
                                                //    {
                                                //        stunLikeEffect = EffectType.stun;
                                                //        maxStunTime = myAttackHitbox.effects[i].stunTime;
                                                //    }
                                                //    break;
                                        }
                                        #endregion
                                    }
                                }
                                if (targetsHitWait1Frame.Contains(otherPlayer.name))
                                {
                                    targetsHitWait1Frame.Remove(otherPlayer.name);
                                }
                                if (myPlayerCombat.hitboxDebugsOn) Debug.Log("Soy " + myPlayerMov.name + " y añado al jugador " + otherPlayer.name + " a la lista de jugadores ya pegados");

                                otherPlayer.StartReceiveHit(myPlayerMov, resultKnockback, stunLikeEffect, ignoreMass, maxStunTime, myPlayerCombat.autocomboIndex);

                                if(!targetsHit.Contains(otherPlayer.name))targetsHit.Add(otherPlayer.name);
                            }
                        }
                        break;
                    #endregion

                    #region --- DUMMY ---
                    case "Dummy":

                        /*DummyMovementCMF dummy = col.GetComponent<DummyBodyCMF>().myPlayerMov;
                        if (myPlayerMov.team != dummy.team && !myPlayerCombat.parryStarted) {
                            //CONTAINS !!!JULIO!!!
                            encontrado = false;
                            for (int i = 0; i < targetsHit.Count && !encontrado; i++) {
                                if (targetsHit[i] == dummy.name) {
                                    encontrado = true;
                                }
                            }
                            if (encontrado) {
                                if (myPlayerCombat.hitboxDebugsOn) Debug.Log("I'm " + myPlayerMov.name + " and " + dummy.name + " is already added to our list of enemies, so we don't attack him.");
                                return;
                            }

                            if (dummy.myPlayerCombat.attackStg == AttackPhaseType.active && dummy.myPlayerCombat.currentAttack.HasEffect(EffectType.parry) &&
                                !targetsHitWait1Frame.Contains(dummy.name) ) {
                                targetsHitWait1Frame.Add(dummy.name);
                            } else {
                                //What type of Effect?
                                for (int i = 0; i < myAttackHitbox.effects.Length; i++) {
                                    if (stunLikeEffect != EffectType.parry)//Are we already giving effect parry? continue if no
                                    {
                                        //targetsHit.Add(otherPlayer.playerNumber);

                                        #region EFFECT TYPE SWITCH
                                        switch (myAttackHitbox.effects[i].effectType) {
                                            case EffectType.knockback:
                                                //calculate knockback vector
                                                if (!myPlayerMov.disableAllDebugs) Debug.Log("KNOCKBACK TYPE= " + myAttackHitbox.effects[i].knockbackType);
                                                switch (myAttackHitbox.effects[i].knockbackType) {
                                                    case KnockbackType.outwards:
                                                        Vector3 myPos = myPlayerMov.transform.position;
                                                        Vector3 colPos = col.transform.position;
                                                        resultKnockback = new Vector3(colPos.x - myPos.x, 0, colPos.z - myPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                    case KnockbackType.inwards:
                                                        myPos = myPlayerMov.transform.position;
                                                        colPos = col.transform.position;
                                                        resultKnockback = new Vector3(myPos.x - colPos.x, 0, myPos.z - colPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                    case KnockbackType.customDir:
                                                        //calculate real direction based on character's facing direction
                                                        float facingAngle = -myPlayerMov.facingAngle;
                                                        Vector3 customDir = myAttackHitbox.effects[i].knockbackDir;

                                                        float theta = facingAngle * Mathf.Deg2Rad;
                                                        float cs = Mathf.Cos(theta);
                                                        float sn = Mathf.Sin(theta);
                                                        float px = customDir.x * cs - customDir.z * sn;
                                                        float py = customDir.x * sn + customDir.z * cs;
                                                        resultKnockback = new Vector3(px, customDir.y, py).normalized;
                                                        resultKnockback = resultKnockback * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        //print("Facing Angle(localRot.y)= " + facingAngle + "; customDir = " + customDir);
                                                        break;
                                                    case KnockbackType.autoCenter:
                                                        float a = Mathf.Abs(myPlayerMov.breakAcc);
                                                        //float iT = myPlayerMov.MissingImpulseTime();
                                                        //float impulseDist = (a * Mathf.Pow(iT, 2)) / 2;
                                                        float impulseDist = myPlayerMov.currentImpulse.CalculateMissingDistance(myPlayerMov.transform.position);
                                                        if (!myPlayerMov.disableAllDebugs) Debug.LogWarning("impulseDist = " + impulseDist);

                                                        Vector3 hitDir = myPlayerMov.currentImpulse.impulseInitialSpeed != 0 ? myPlayerMov.currentImpulse.impulseDir : myPlayerMov.rotateObj.forward;
                                                        float meNoMaeDist = myAttackHitbox.effects[i].knockbackMagnitude + impulseDist;
                                                        Vector3 meNoMaePos = myPlayerMov.rotateObj.position + (hitDir * meNoMaeDist);//me no mae (目の前) means in front of your eyes
                                                        resultKnockback = (meNoMaePos - dummy.transform.position);
                                                        resultKnockback.y = 0;
                                                        Debug.DrawLine(dummy.transform.position, (meNoMaePos), Color.red, 4);
                                                        if (!myPlayerMov.disableAllDebugs) Debug.LogWarning("hitDir = " + hitDir + "; meNoMaeDist = " + meNoMaeDist + "; meNoMaePos = " + meNoMaePos);

                                                        float d = resultKnockback.magnitude;
                                                        //vi = Mathf.Sqrt(2*a*d);
                                                        float vi = Mathf.Sqrt((2 * a * d));
                                                        if (!myPlayerMov.disableAllDebugs) Debug.LogWarning("distance = " + d.ToString("F4") + "; Initial velocity = " + vi.ToString("F4") +
                                                            "; myPlayerMov.breakAcc = " + a);
                                                        resultKnockback = resultKnockback.normalized * vi;
                                                        break;
                                                    case KnockbackType.redirect:
                                                        float inputRedirectAngle = SignedRelativeAngle(myPlayerMov.rotateObj.forward, myPlayerMov.currentInputDir, Vector3.up);
                                                        float finalRedirectAngle = Mathf.Clamp(inputRedirectAngle, -myAttackHitbox.effects[i].redirectMaxAngle, myAttackHitbox.effects[i].redirectMaxAngle);
                                                        resultKnockback = Quaternion.Euler(0, finalRedirectAngle, 0) * myPlayerMov.rotateObj.forward;
                                                        //CALCULATE Y ANGLE
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        //resultKnockback *= myAttackHitbox.effects[i].knockbackMagnitude;
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        if (!myPlayerMov.disableAllDebugs) {
                                                            //Debug.LogError("myPlayerMov.currentInputDir = " + myPlayerMov.currentInputDir+ "; resultKnockback = " + resultKnockback);
                                                            Debug.DrawRay(myPlayerMov.transform.position, resultKnockback.normalized * 1, Color.red);
                                                        }
                                                        break;
                                                    case KnockbackType.outwardsFromHitbox:
                                                        myPos = transform.position;
                                                        colPos = col.transform.position;
                                                        resultKnockback = new Vector3(colPos.x - myPos.x, 0, colPos.z - myPos.z).normalized;
                                                        resultKnockback = Quaternion.Euler(0, myAttackHitbox.effects[i].knockbackYAngle, 0) * resultKnockback;
                                                        resultKnockback = CalculateYAngle(col.transform.position, resultKnockback.normalized, myAttackHitbox.effects[i].knockbackYAngle);
                                                        resultKnockback = resultKnockback.normalized * myAttackHitbox.effects[i].knockbackMagnitude;
                                                        break;
                                                }
                                                if (!myPlayerMov.disableAllDebugs) print("KNOCKBACK RESULT= " + resultKnockback);
                                                break;
                                            case EffectType.softStun:
                                                //if (stunLikeEffect != EffectType.stun)
                                                //{
                                                stunLikeEffect = EffectType.softStun;
                                                maxStunTime = myAttackHitbox.effects[i].stunTime;
                                                //}
                                                break;
                                                //case EffectType.stun:
                                                //    if (stunLikeEffect != EffectType.knockdown)
                                                //    {
                                                //        stunLikeEffect = EffectType.stun;
                                                //        maxStunTime = myAttackHitbox.effects[i].stunTime;
                                                //    }
                                                //    break;
                                        }
                                        #endregion
                                    }
                                }
                                if (targetsHitWait1Frame.Contains(dummy.name)) {
                                    targetsHitWait1Frame.Remove(dummy.name);
                                }
                                if (myPlayerCombat.hitboxDebugsOn) Debug.Log("Soy " + myPlayerMov.name + " y añado al jugador " + dummy.name + " a la lista de jugadores ya pegados");

                                dummy.StartReceiveHit(myPlayerMov, resultKnockback, stunLikeEffect, maxStunTime, myPlayerCombat.autocomboIndex);

                                //!!!JULIO!!!
                                if (!targetsHit.Contains(dummy.name)) targetsHit.Add(dummy.name);
                            }
                        }*/
                        break;
                        
                    #endregion

                    #region --- HITBOX ---
                    case "Hitbox":
                        HitboxCMF otherHitbox = col.GetComponent<HitboxCMF>();
                        PlayerMovementCMF enemy = otherHitbox.myPlayerMov;
                        if (otherHitbox != null && enemy != null)
                        {
                            if (myPlayerMov.team != enemy.team)
                            {
                                encontrado = false;
                                for (int i = 0; i < targetsHit.Count && !encontrado; i++)
                                {
                                    if (targetsHit[i] == enemy.name)
                                    {
                                        encontrado = true;
                                    }
                                }
                                if (!encontrado)
                                {

                                    //QUE TIPO DE EFFECT

                                    if (enemy.myPlayerCombat.attackStg == AttackPhaseType.active)
                                    {
                                        int priorityDiff = myPlayerCombat.currentAttack.attackPriority - otherHitbox.myPlayerCombat.currentAttack.attackPriority;
                                        priorityDiff = priorityDiff != 0 ? (int)Mathf.Sign(priorityDiff) : 0;
                                        AttackEffect parryEffect = myAttackHitbox.GetEffect(EffectType.parry);
                                        switch (priorityDiff)
                                        {
                                            case -1://TENEMOS MENOS PRIORIDAD
                                                break;
                                            case 1://TENEMOS MÁS PRIORIDAD
                                                //targetsHit.Add(enemy.playerNumber);
                                                if (myPlayerCombat.hitboxDebugsOn) Debug.LogWarning("Soy " + myPlayerMov.name + " y hago efecto parry con mi ataque " + myPlayerCombat.currentAttack.attackName + " al jugador " + enemy.name);
                                                enemy.StartReceiveParry(myPlayerMov, parryEffect);
                                                //targetsHit.RemoveAt(targetsHit.Count - 1);
                                                break;
                                            case 0://TENEMOS IGUAL PRIORIDAD
                                                   //targetsHit.Add(enemy.playerNumber);
                                                if (myPlayerCombat.hitboxDebugsOn) Debug.LogWarning("Soy " + myPlayerMov.name + " y hago efecto parry MUTUO con mi ataque " + myPlayerCombat.currentAttack.attackName + " al jugador " + enemy.name);
                                                enemy.StartReceiveParry(myPlayerMov);
                                                myPlayerMov.StartReceiveParry(enemy);
                                                //targetsHit.RemoveAt(targetsHit.Count - 1);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (myPlayerCombat.hitboxDebugsOn) Debug.Log("Enemy is not in active phase, so we don't collide hitboxes");
                                    }
                                }
                                else
                                {
                                    if (myPlayerCombat.hitboxDebugsOn) Debug.LogWarning("Soy " + myPlayerMov.name + " y " + enemy.name + " ya está en nuestra lista de hitboxes añadidas, así que no atacamos");
                                }
                            }
                        }
                        else
                        {
                            if (myPlayerCombat.hitboxDebugsOn) Debug.LogError("Couldn't find the hitbox or PlayerMovementCMF scripts");
                        }
                        break;
                        #endregion
                }
            }
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject != myPlayerMov.gameObject)
        {
            if (myPlayerCombat.attackStg == AttackPhaseType.active)//(tag != "HookBigHB" && tag != "HookSmallHB") && 
            {
                //print("I'm " + myPlayerMov.gameObject.name + " and I collided with " + col.gameObject);
                switch (col.tag)
                {
                    case "PlayerBody":
                        PlayerMovementCMF otherPlayer = col.GetComponent<PlayerBodyCMF>().myPlayerMov;
                        if (myPlayerMov.team != otherPlayer.team)
                        {
                            bool encontrado = false;
                            for (int i = 0; i < targetsHit.Count && !encontrado; i++)
                            {
                                if (targetsHit[i] == otherPlayer.name)
                                {
                                    encontrado = true;
                                    targetsHit.RemoveAt(i);
                                }
                            }
                        }
                        break;
                    case "Dummy":
                        /*DummyMovementCMF otherDummy = col.GetComponent<DummyBodyCMF>().myPlayerMov;
                        if (myPlayerMov.team != otherDummy.team) {
                            bool encontrado = false;
                            for (int i = 0; i < targetsHit.Count && !encontrado; i++) {
                                if (targetsHit[i] == otherDummy.name) {
                                    encontrado = true;
                                    targetsHit.RemoveAt(i);
                                }
                            }
                        }*/
                        break;
                    case "Hitbox":
                        otherPlayer = col.GetComponent<HitboxCMF>().myPlayerMov;
                        if (myPlayerMov.team != otherPlayer.team)
                        {
                            if (myPlayerCombat.hitboxDebugsOn) Debug.Log("" + otherPlayer.name + "'s hitbox has exit our(" + myPlayerMov.name + ") hitbox. Let's check if it was in our list:");
                            bool encontrado = false;
                            for (int i = 0; i < targetsHit.Count && !encontrado; i++)
                            {
                                if (targetsHit[i] == otherPlayer.name)
                                {
                                    encontrado = true;
                                    if (myPlayerCombat.hitboxDebugsOn) Debug.Log(myPlayerMov.name + " hitting " + otherPlayer + "'s hitbox, and it has exit our hitbox");
                                    targetsHit.RemoveAt(i);
                                }
                            }
                        }
                        break;
                }
            }
        }
    }

    //AUXILIAR 

    /// <summary>
    /// //Funcion que calcula el angulo de un vector respecto a otro que se toma como referencia de "foward"
    /// </summary>
    /// <param name="referenceForward"></param>
    /// <param name="newDirection"></param>
    /// <returns></returns>
    float SignedRelativeAngle(Vector3 referenceForward, Vector3 newDirection, Vector3 referenceUp)
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

    public static Vector3 CalculateYAngle(Vector3 enemyPos, Vector3 originDir, float verticalAngle)
    {
        //Vector3 perpVector = new Vector3(-originDir.z, 0, originDir.x);
        //Calculate point along originDir

        Vector3 originDirPoint = enemyPos + originDir.normalized * 1;
        //tan C = c/b; height = c; verticalAngle = C; b = 1;
        float height = Mathf.Tan(verticalAngle * Mathf.Deg2Rad) * 1;
        Vector3 finalPoint = originDirPoint + Vector3.up * height;
        //Debug.DrawLine(enemyPos, originDirPoint, Color.white,4);
        //Debug.DrawLine(originDirPoint, finalPoint, Color.yellow,4);
        //Debug.DrawLine(finalPoint, enemyPos, Color.red,4);
        return (finalPoint - enemyPos);
    }
}
