﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NodeEditor : EditorWindow
{
    private Vector2 mousePos;

    private Node selectednode;

    private object focusedObj;

    private bool makeTransitionMode = false;

    private List<FSM> FSMs = new List<FSM>();

    private FSM currentFSM;

    [MenuItem("Window/Node Editor")]
    static void ShowEditor()
    {
        NodeEditor editor = EditorWindow.GetWindow<NodeEditor>();
    }

    private void OnGUI()
    {
        Event e = Event.current;

        mousePos = e.mousePosition;

        if (e.button == 1 && !makeTransitionMode)
        {
            if (e.type == EventType.MouseDown)
            {
                bool clickedOnWindow = false;
                bool clickedOnTransition = false;
                int selectIndex = -1;

                for (int i = 0; i < currentFSM?.states.Count; i++)
                {
                    if (currentFSM.states[i].windowRect.Contains(mousePos))
                    {
                        selectIndex = i;
                        clickedOnWindow = true;
                        break;
                    }
                }

                for (int i = 0; i < currentFSM?.transitions.Count; i++)
                {
                    if (currentFSM.transitions[i].textBox.Contains(mousePos))
                    {
                        clickedOnTransition = true;
                        break;
                    }
                }

                if (!clickedOnWindow && !clickedOnTransition && currentFSM == null)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add FSM"), false, ContextCallback, "FSM");

                    menu.ShowAsContext();
                    e.Use();
                }
                else
                if (!clickedOnWindow && !clickedOnTransition && currentFSM != null)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add Node"), false, ContextCallback, "Node");

                    menu.ShowAsContext();
                    e.Use();
                }
                else if (clickedOnTransition)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Delete Transition"), false, ContextCallback, "deleteTransition");

                    menu.ShowAsContext();
                    e.Use();
                }
                else if (clickedOnWindow)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Make Transition"), false, ContextCallback, "makeTransition");
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete Node"), false, ContextCallback, "deleteNode");

                    if (!currentFSM.isEntryState(currentFSM.states[selectIndex]))
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Convert to Entry State"), false, ContextCallback, "entryState");
                    }

                    menu.ShowAsContext();
                    e.Use();
                }
            }
        }
        else if (e.button == 0 && e.type == EventType.MouseDown)
        {
            bool clickedOnWindow = false;
            bool clickedOnTransition = false;
            int selectIndex = -1;

            for (int i = 0; i < currentFSM?.states.Count; i++)
            {
                if (currentFSM.states[i].windowRect.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnWindow = true;
                    break;
                }
            }

            for (int i = 0; i < currentFSM?.transitions.Count; i++)
            {
                if (currentFSM.transitions[i].textBox.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnTransition = true;
                    break;
                }
            }

            if (clickedOnTransition)
            {
                if (focusedObj is Transition) ((Transition)focusedObj).isFocused = false;
                if (focusedObj is Node) ((Node)focusedObj).isFocused = false;
                currentFSM.transitions[selectIndex].isFocused = true;
                focusedObj = currentFSM.transitions[selectIndex];

                e.Use();
            }
            else if (clickedOnWindow)
            {
                if (focusedObj is Transition) ((Transition)focusedObj).isFocused = false;
                if (focusedObj is Node) ((Node)focusedObj).isFocused = false;
                currentFSM.states[selectIndex].isFocused = true;
                focusedObj = currentFSM.states[selectIndex];

                //e.Use();
            }
            else
            {
                if (focusedObj is Transition) ((Transition)focusedObj).isFocused = false;
                if (focusedObj is Node) ((Node)focusedObj).isFocused = false;

                focusedObj = null;

                e.Use();
            }

            if (makeTransitionMode)
            {
                if (clickedOnWindow && !currentFSM.states[selectIndex].Equals(selectednode))
                {
                    Transition transition = new Transition("New Transition", selectednode, currentFSM.states[selectIndex]);
                    transition.textBox = DrawTextBox(transition);

                    currentFSM.AddTransition(transition);

                    makeTransitionMode = false;
                    selectednode = null;
                }

                if (!clickedOnWindow)
                {
                    makeTransitionMode = false;
                    selectednode = null;
                }

                e.Use();
            }
        }

        if (makeTransitionMode && selectednode != null)
        {
            Rect mouseRect = new Rect(e.mousePosition.x, e.mousePosition.y, 10, 10);
            Rect nodeRect = new Rect(selectednode.windowRect);
            nodeRect.x = selectednode.windowRect.x + nodeRect.width / 2;

            DrawNodeCurve(nodeRect, mouseRect, true);

            Repaint();
        }

        if (currentFSM)
        {
            foreach (Node n in currentFSM.states)
            {
                n.DrawCurves();
            }
        }

        BeginWindows();

        if (currentFSM)
        {
            for (int i = 0; i < currentFSM.states.Count; i++)
            {
                GUIStyle style = new GUIStyle();
                style.contentOffset = new Vector2(0, -20);
                style.normal.background = MakeBackground(2, 2, (int)currentFSM.states[i].type, currentFSM.states[i].isFocused);

                currentFSM.states[i].windowRect = GUI.Window(i, currentFSM.states[i].windowRect, DrawNodeWindow, currentFSM.states[i].stateName, style);
            }
        }

        EndWindows();

        if (e.isKey)
        {
            if (!makeTransitionMode && Event.current.keyCode == KeyCode.Delete)
            {
                if (focusedObj is Node)
                {
                    currentFSM.DeleteNode(((Node)focusedObj));

                    if (currentFSM.isEntryState(((Node)focusedObj)))
                    {
                        FSMs.Remove(currentFSM);
                        currentFSM = null;
                    }

                    focusedObj = null;

                    e.Use();
                }
                if (focusedObj is Transition)
                {
                    currentFSM.DeleteTransition(((Transition)focusedObj));

                    focusedObj = null;

                    e.Use();
                }
            }
        }
    }

    //Habrá que cambiarlo
    private Texture2D MakeBackground(int width, int height, int type, bool isFocused)
    {
        Color col;

        if (type == 1)
        {
            col = Color.green;
        }
        else if (type == 2)
        {
            col = Color.red;
        }
        else
        {
            col = Color.grey;
        }

        if (!isFocused)
            col.a = 0.5f;

        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    void DrawNodeWindow(int id)
    {
        currentFSM.states[id].DrawWindow();
        GUI.DragWindow();
    }

    void ContextCallback(object obj)
    {
        string clb = obj.ToString();

        if (clb.Equals("FSM"))
        {
            Node node = new Node(1);
            node.windowRect = new Rect(mousePos.x, mousePos.y, 200, 100);

            currentFSM = new FSM("New FSM", node);

            FSMs.Add(currentFSM);
        }
        else if (clb.Equals("Node"))
        {
            var type = 1;
            foreach (Node n in currentFSM.states)
            {
                if (n.type == Node.stateType.Entry)
                {
                    type = 2;
                    break;
                }
            }

            Node node = new Node(type);
            node.windowRect = new Rect(mousePos.x, mousePos.y, 200, 100);

            currentFSM.AddState(node);
        }
        else if (clb.Equals("makeTransition"))
        {
            bool clickedOnWindow = false;
            int selectIndex = -1;

            for (int i = 0; i < currentFSM.states.Count; i++)
            {
                if (currentFSM.states[i].windowRect.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnWindow = true;
                    break;
                }
            }

            if (clickedOnWindow)
            {
                selectednode = currentFSM.states[selectIndex];
                makeTransitionMode = true;
            }
        }
        else if (clb.Equals("deleteNode"))
        {
            bool clickedOnWindow = false;
            int selectIndex = -1;

            for (int i = 0; i < currentFSM.states.Count; i++)
            {
                if (currentFSM.states[i].windowRect.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnWindow = true;
                    break;
                }
            }

            if (clickedOnWindow)
            {
                Node selNode = currentFSM.states[selectIndex];

                currentFSM.DeleteNode(selNode);

                if (currentFSM.isEntryState(selNode))
                {
                    FSMs.Remove(currentFSM);
                    currentFSM = null;
                }
            }
        }
        else if (clb.Equals("deleteTransition"))
        {
            bool clickedOnTransition = false;
            int selectIndex = -1;

            for (int i = 0; i < currentFSM.transitions.Count; i++)
            {
                if (currentFSM.transitions[i].textBox.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnTransition = true;
                    break;
                }
            }

            if (clickedOnTransition)
            {
                Transition transition = currentFSM.transitions[selectIndex];

                currentFSM.DeleteTransition(transition);
            }
        }
        else if (clb.Equals("entryState"))
        {
            bool clickedOnWindow = false;
            int selectIndex = -1;

            for (int i = 0; i < currentFSM.states.Count; i++)
            {
                if (currentFSM.states[i].windowRect.Contains(mousePos))
                {
                    selectIndex = i;
                    clickedOnWindow = true;
                    break;
                }
            }

            if (clickedOnWindow)
            {
                Node selNode = currentFSM.states[selectIndex];

                currentFSM.SetAsEntry(selNode);
            }
        }
    }

    public static void DrawNodeCurve(Rect start, Rect end, bool isFocused)
    {
        Vector3 startPos = new Vector3(start.x + start.width / 2, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3(end.x + end.width / 2, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Color shadowCol = new Color(0, 0, 0, 0.06f);
        int focusFactor = 3;

        if (isFocused)
        {
            shadowCol = new Color(1, 1, 1, 0.1f);
            focusFactor = 10;
        }

        for (int i = 0; i < focusFactor; i++)
        {
            Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
        }

        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1);
    }

    public static Rect DrawTextBox(Transition trans)
    {
        Vector2 pos = new Vector2(trans.fromNode.windowRect.center.x + (trans.toNode.windowRect.x - trans.fromNode.windowRect.x) / 2, trans.fromNode.windowRect.center.y + (trans.toNode.windowRect.y - trans.fromNode.windowRect.y) / 2);
        Rect textBox = new Rect(pos.x - 75, pos.y - 15, 150, 30);

        GUIStyle style = new GUIStyle();

        style.alignment = TextAnchor.UpperCenter;

        EditorGUI.LabelField(textBox, trans.transitionName, style);

        return textBox;
    }
}
