﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxHookBigCMF : MonoBehaviour
{
    PlayerMovementCMF myPlayerMov;
    PlayerHookCMF myPlayerHook;
    HookCMF myHook;

    public void KonoAwake(PlayerMovementCMF playerMov, PlayerHookCMF hook)
    {
        myPlayerMov = playerMov;
        myPlayerHook = hook;
        myHook = GetComponentInParent<HookCMF>();
    }
    private void OnTriggerEnter(Collider col)
    {
        //print("Hook: Collision with " + col.name);
        if (col.gameObject != myPlayerMov.gameObject)
        {
            /*if(!myPlayerMov.disableAllDebugs)*/
            //Debug.Log("HOOK has hit " + col.tag);
            if (myPlayerHook.canHookSomething)
            {
                switch (col.tag)
                {
                    case "Flag":
                        myPlayerHook.HookObject(col.transform);
                        break;
                    case "PlayerBody":
                        if (myPlayerHook.debugModeOn) Debug.Log("HOOK PLAYER: checking team");
                        PlayerMovementCMF otherPlayer = col.GetComponent<PlayerBodyCMF>().myPlayerMov;
                        if (myPlayerMov.team != otherPlayer.team)// IF ENEMY
                        {
                            if (otherPlayer.vertMovSt != VerticalMovementState.FloatingInWater)// OUTSIDE WATER
                            {
                                if (myPlayerHook.debugModeOn) Debug.Log("HOOK PLAYER: is an enemy!");
                                myPlayerHook.HookPlayer(otherPlayer);
                            }
                        }
                        else
                        {
                            if (otherPlayer.vertMovSt == VerticalMovementState.FloatingInWater)//IF ALLY IN WATER
                            {
                                myPlayerHook.HookPlayer(otherPlayer);
                            }
                        }
                        break;
                    case "Dummy":
                        Dummy dummy = col.GetComponent<Dummy>();
                        //myPlayerHook.HookPlayer(dummy);
                        break;
                    /*case "Stage":
                        StageScript stage = col.GetComponent<StageScript>();
                        if (stage != null)
                        {
                            if (stage.hookable)
                            {
                                myHook.StartGrappling();
                            }
                        }
                        break;*/
                    case "HookPoint":
                        //print("Hook: Collision with HookPoint 1");
                        CollideWithHookPoint(col);
                        break;
                    case "Hitbox":
                        HitboxCMF hb = col.GetComponent<HitboxCMF>();
                        Debug.LogError("Hook has hit a hitbox");
                        if (hb != null && hb.myPlayerMov.team != myPlayerMov.team)
                        {
                            Debug.LogError("Hook has hit a hitbox that is from an enemy");

                            if (hb.myAttackHitbox.GetEffect(EffectType.parry) != null && hb.myPlayerCombat.attackStg == AttackPhaseType.active)
                            {
                                Debug.LogError("Hook has hit a hitbox that is from an enemy and has a parry effect and is active");

                                int priorityDiff = hb.myPlayerCombat.currentAttack.attackPriority - myPlayerHook.hookPriority;
                                priorityDiff = priorityDiff != 0 ? (int)Mathf.Sign(priorityDiff) : 0;
                                Debug.Log("priorityDiff = " + priorityDiff + "; hb.myPlayerCombatNew.currentAttack.attackPriority = " + hb.myPlayerCombat.currentAttack.attackPriority +
                                    "; myPlayerHook.hookPriority = " + myPlayerHook.hookPriority);
                                switch (priorityDiff)
                                {
                                    case 1://parry with more priority than hook
                                        Debug.LogError("Hook has hit a hitbox that is from an enemy and has a parry effect and has more priority than my hook.");
                                        myPlayerHook.StopHook();
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
            else if (myPlayerHook.grappleSt == GrappleState.throwing)
            {
                CollideWithHookPoint(col);
            }
        }
    }

    void CollideWithHookPoint(Collider col)
    {
        if (col.name.Contains("SmallTrigger"))
        {
            //print("Hook: Collision with HookPoint 2");
            HookPoint hookPoint = col.transform.parent.GetComponent<HookPoint>();
            if (hookPoint != null)
            {
                RaycastHit hit;
                Vector3 rayOrigin = myPlayerHook.currentHook.transform.position;
                Vector3 rayEnd = col.transform.parent.position;
                LayerMask lm = LayerMask.GetMask("Stage");
                Debug.DrawLine(rayOrigin, rayEnd, Color.yellow, 3);
                //Debug.Log("hook pos = " + rayOrigin.ToString("F4"));

                Transform hookPos;
                if (Physics.Linecast(rayOrigin, rayEnd, out hit, lm, QueryTriggerInteraction.Collide))
                {
                    hookPos = hookPoint.GetHookPoint(hit.point);//col.ClosestPointOnBounds(myHook.transform.position));
                }
                else
                {
                    //Debug.Log("Error: Can't find a collision point between the hook and the hookPoint. " +
                    //    "There must be one since we already collided and we are just trying to find a collisions point");
                    hookPos = hookPoint.GetHookPoint(col.ClosestPointOnBounds(myPlayerHook.transform.position));
                }
                myHook.transform.position = hookPos.position;
                myPlayerHook.StartGrappling(col.transform.parent.GetComponent<HookPoint>(), hookPos);
                //myHook.transform.parent = col.transform.parent.GetComponent<HookPoint>().transform;
            }
        }
    }
}
