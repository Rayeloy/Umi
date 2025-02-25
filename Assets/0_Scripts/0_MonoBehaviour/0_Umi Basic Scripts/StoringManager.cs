﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoringManager : MonoBehaviour {

    public static StoringManager instance;
    public List<Transform> objectsStored;
    public GameObject hookPrefab;
    public GameObject flagPrefab;
    public GameObject fakeFlagPrefab;
    public GameObject fakeHookPrefab;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        objectsStored = new List<Transform>();
    }
    /// <summary>
    /// Looks for an object that contains(name)
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsObjectStored(string tag)
    {
        bool result = false;
        foreach (Transform transf in objectsStored)
        {
            if (transf.tag == tag)
            {
                return true;
            }
        }
        return result;
    }

    public Transform LookForObjectStored(string tag)
    {
        Transform result = null;
        foreach (Transform transf in objectsStored)
        {
            if (transf.tag == tag)
            {
                return transf;
            }
        }
        return result;
    }

    public Transform LookForObjectStoredTag(string tag)
    {
        Transform result = null;
        foreach (Transform transf in objectsStored)
        {
            if (transf.tag == tag)
            {
                return transf;
            }
        }
        return result;
    }

    public Transform Spawn(GameObject prefab, Vector3 newPos, Quaternion newRot)
    {
        string tag = prefab.tag;
        if (IsObjectStored(tag))
        {
            return TakeObjectStored(tag, newPos, newRot);
        }
        else
        {
            return Instantiate(prefab, newPos, newRot, transform).transform;
        }
    }

    public Transform Spawn(GameObject prefab, Transform newParent, Vector3 newPos, Quaternion newRot)
    {
        string tag = prefab.tag;
        if (IsObjectStored(tag))
        {
            return TakeObjectStored(tag, newParent, newPos, newRot);
        }
        else
        {
            return Instantiate(prefab, newPos, newRot, transform).transform;
        }
    }

    /// <summary>
    /// Takes the object stored in the StoringManager that contains "name", puts "newParent" as parent, positions as "newPos" and rotation as "newRot", 
    /// then it removes object's transform from objectsStored list;
    /// </summary>
    /// <param name="name"></param>
    /// <param name="newParent"></param>
    /// <param name="newPos"></param>
    /// <param name="newRot"></param>
    /// <returns></returns>
    public Transform TakeObjectStored(string tag,Transform newParent, Vector3 newPos, Quaternion newRot)
    {
        Transform result = null;
        for (int i=0; i < objectsStored.Count; i++)
        {
            if(objectsStored[i].tag == tag)
            {
                result = objectsStored[i];
                objectsStored.RemoveAt(i);
                result.SetParent(newParent);
                result.position = newPos;
                result.rotation = newRot;
                return result;
            }
        }
        return result;
    }
    /// <summary>
    /// Takes the object stored in the StoringManager that contains "name", puts StoringManager as parent, positions as newPos and rotation as newRot, 
    /// then it removes object's transform from objectsStored list;
    /// </summary>
    /// <param name="name"></param>
    /// <param name="newPos"></param>
    /// <param name="newRot"></param>
    /// <returns></returns>
    public Transform TakeObjectStored(string tag, Vector3 newPos, Quaternion newRot)
    {
        //print("Looking for Object Stored " + name);
        Transform result = null;
        for (int i = 0; i < objectsStored.Count; i++)
        {
            //print("Checking Object Stord with name " + objectsStored[i].name);
            if (objectsStored[i].tag == tag)
            {
                result = objectsStored[i];
                objectsStored.RemoveAt(i);
                result.SetParent(transform);
                result.position = newPos;
                result.rotation = newRot;
                return result;
            }
        }
        return result;
    }

    public void StoreObject(Transform newObject)
    {
        newObject.SetParent(transform);
        newObject.localPosition = Vector3.zero;
        //print("Storing Manager: " + newObject.name + " localPosition is " + newObject.localPosition);
        newObject.rotation = Quaternion.identity;
        objectsStored.Add(newObject);
    }

    public void UnstoreObject(Transform oldObject)
    {
        if (IsObjectStored(oldObject.tag))
        {
            for (int i = 0; i < objectsStored.Count; i++)
            {
                if (objectsStored[i].tag == tag)
                {
                    objectsStored.RemoveAt(i);
                }
            }  
        }
    }

    [HideInInspector]
    public bool isEmpty
    {
        get
        {
            return objectsStored.Count ==0;
        }
    }
    [HideInInspector]
    public int objectsCount
    {
        get
        {
            return objectsStored.Count;
        }
    }
}
