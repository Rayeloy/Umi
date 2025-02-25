﻿using UnityEngine;
using UnityEditor;

public abstract class EditorWithSubEditors<TEditor, TTarget> : Editor
    where TEditor : Editor
    where TTarget : Object
{
    [SerializeField]
    public TEditor[] subEditors = new TEditor[0];


    protected virtual void CheckAndCreateSubEditors(ref TTarget[] subEditorTargets)
    {
        Debug.Log("CheckAndCreateSubEditors starts");
        if (subEditors != null && subEditors.Length == subEditorTargets.Length)
        {
            Debug.Log("CheckAndCreateSubEditors exit");
            return;
        }
        //Debug.Log("subEditors[] =  "+subEditors);

        CleanupEditors();

        subEditors = new TEditor[subEditorTargets.Length];

        for (int i = 0; i < subEditors.Length; i++)
        {
            subEditors[i] = CreateEditor(subEditorTargets[i]) as TEditor;
            SubEditorSetup(subEditors[i]);
            //Debug.Log("Editor for " + subEditorTargets[i]+" created");
        }
    }


    protected void CleanupEditors()
    {
        if (subEditors == null)
            return;

        for (int i = 0; i < subEditors.Length; i++)
        {
            DestroyImmediate(subEditors[i]);
        }

        subEditors = null;
    }


    protected abstract void SubEditorSetup(TEditor editor);
}