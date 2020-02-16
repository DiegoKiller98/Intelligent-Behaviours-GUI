﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class BaseNode : ScriptableObject
{
    public Rect windowRect;

    public bool hasInputs = false;

    public string windowTitle = "";

    public virtual void DrawWindow()
    {
        windowTitle = EditorGUILayout.TextField("Title", windowTitle);
    }

    public virtual void DrawCurves()
    {

    }

    public virtual void SetInput(BaseNode input, Vector2 clickPos)
    {

    }

    public virtual void NodeDeleted(BaseNode node)
    {

    }

    public virtual BaseNode ClickedOnNode(Vector2 pos)
    {
        return null;
    }

    public virtual string getResult()
    {
        return "None";
    }
}
