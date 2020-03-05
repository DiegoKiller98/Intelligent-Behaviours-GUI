﻿using UnityEngine;
using UnityEditor;

public class PopupWindow : EditorWindow
{
    static string typeOfElem;

    static GUIStyle DeleteStyle;
    static GUIStyle CancelStyle;

    static NodeEditor senderEditor;
    static int locIndex;

    public static void Init(NodeEditor sender, int index, string type)
    {
        senderEditor = sender;

        locIndex = index;
        typeOfElem = type;

        PopupWindow window = ScriptableObject.CreateInstance<PopupWindow>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
        window.ShowPopup();
    }

    public static void ClosePopup(PopupWindow popup)
    {
        popup.Close();
    }

    void OnGUI()
    {
        DeleteStyle = new GUIStyle(GUI.skin.button);
        DeleteStyle.normal.background = MakeBackground(Color.red);

        CancelStyle = new GUIStyle(GUI.skin.button);
        CancelStyle.normal.background = MakeBackground(Color.gray);

        EditorGUILayout.LabelField("Do you want to delete this " + typeOfElem + "?", EditorStyles.boldLabel, GUILayout.Width(this.position.width - 10), GUILayout.ExpandHeight(true));
        if (senderEditor.currentElem is BehaviourTree)
        {
            int numberOfSons = ((BehaviourTree)senderEditor.currentElem).SonsCount(locIndex);
            if (numberOfSons > 0)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.normal.textColor = Color.red;

                EditorGUILayout.LabelField(numberOfSons + " child nodes will be deleted as well", labelStyle, GUILayout.Width(this.position.width - 10), GUILayout.ExpandHeight(true));
            }
        }

        GUILayout.Space(70);

        if (GUILayout.Button("Delete", DeleteStyle))
        {
            senderEditor.Delete(typeOfElem, locIndex);
            this.Close();
        }
        if (GUILayout.Button("Cancel", CancelStyle))
        {
            this.Close();
        }
    }

    //Habrá que cambiarlo
    private Texture2D MakeBackground(Color col)
    {
        Color[] pix = new Color[2 * 2];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(2, 2);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}