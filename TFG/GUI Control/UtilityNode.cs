﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public enum utilityType
{
    Variable,
    Fusion,
    Action,
    Curve
}

public enum fusionType
{
    Weighted,
    GetMax,
    GetMin
}

public enum curveType
{
    Linear,
    Exponential,
    LinearParts
}

public class UtilityNode : BaseNode
{
    /// <summary>
    /// The type of <see cref="UtilityNode"/>
    /// </summary>
    public utilityType type;

    /// <summary>
    /// The type of fusion if this <see cref="UtilityNode"/> is of type Fusion
    /// </summary>
    public fusionType fusionType;

    /// <summary>
    /// The type of curve if this <see cref="UtilityNode"/> is of type Curve
    /// </summary>
    public curveType curveType;

    /// <summary>
    /// Reference to the editor
    /// </summary>
    public NodeEditor editor;

    /// <summary>
    /// Min value for the Variable nodes
    /// </summary>
    public float variableMin;

    /// <summary>
    /// Max value for the Variable nodes
    /// </summary>
    public float variableMax;

    /// <summary>
    /// Slope value for the Curve nodes
    /// </summary>
    public float slope = 1;

    /// <summary>
    /// Exponent value for the Curve nodes
    /// </summary>
    public float exp = 1;

    /// <summary>
    /// Displacement on X value for the Curve nodes
    /// </summary>
    public float displX;

    /// <summary>
    /// Displacement on Y value for the Curve nodes
    /// </summary>
    public float displY;

    /// <summary>
    /// Boolean for keeping in memory if the curve visualizer foldout is open
    /// </summary>
    public bool openFoldout;

    /// <summary>
    /// Returns the <see cref="utilityType"/> properly written
    /// </summary>
    /// <returns></returns>
    public override string GetTypeString()
    {
        if (subElem is null)
            return type.ToString();
        else
            return subElem.GetTypeString();
    }

    /// <summary>
    /// The Initializer for the <seealso cref="UtilityNode"/>
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="typeNumber"></param>
    /// <param name="posx"></param>
    /// <param name="posy"></param>
    /// <param name="subElem"></param>
    public void InitUtilityNode(NodeEditor sender, ClickableElement parent, utilityType type, float posx, float posy, ClickableElement subElem = null)
    {
        InitBaseNode(parent);

        this.type = type;
        this.editor = sender;

        if (subElem != null)
        {
            this.subElem = subElem;
            nodeName = this.subElem.elementName;
            windowRect = new Rect(posx, posy, ClickableElement.width, ClickableElement.height);
        }
        else
        {
            string nameToAdd = "New " + type;
            if (type != utilityType.Variable)
                nameToAdd += " Node ";
            nodeName = parent.elementNamer.AddName(identificator, nameToAdd);

            if (type == utilityType.Fusion)
            {
                windowRect = new Rect(posx, posy, width, height * 1.7f);
            }
            else if (type == utilityType.Curve)
            {
                windowRect = new Rect(posx, posy, width, height * 1.5f);
            }
            else
            {
                windowRect = new Rect(posx, posy, width, height);
            }
        }
    }

    /// <summary>
    /// Draws all the elements inside the <see cref="UtilityNode"/>
    /// </summary>
    public override void DrawWindow()
    {
        switch (type)
        {
            case utilityType.Variable:
                nodeName = CleanName(EditorGUILayout.TextArea(nodeName, Styles.TitleText, GUILayout.ExpandWidth(true), GUILayout.Height(25)));
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Min:", Styles.NonEditable, GUILayout.Width(40), GUILayout.Height(25));
                float.TryParse(EditorGUILayout.TextArea(variableMin.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out variableMin);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Max:", Styles.NonEditable, GUILayout.Width(40), GUILayout.Height(25));
                float.TryParse(EditorGUILayout.TextArea(variableMax.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out variableMax);
                GUILayout.EndHorizontal();

                GUILayout.EndHorizontal();
                break;
            case utilityType.Fusion:
                nodeName = CleanName(EditorGUILayout.TextArea(nodeName, Styles.TitleText, GUILayout.ExpandWidth(true), GUILayout.Height(25)));
                GUILayout.BeginHorizontal();
                GUILayout.Space(windowRect.width * 0.2f);
                GUILayout.BeginVertical();
                if (GUILayout.Toggle(fusionType == fusionType.Weighted, fusionType.Weighted.ToString(), EditorStyles.radioButton))
                    fusionType = fusionType.Weighted;
                if (GUILayout.Toggle(fusionType == fusionType.GetMax, fusionType.GetMax.ToString(), EditorStyles.radioButton))
                    fusionType = fusionType.GetMax;
                if (GUILayout.Toggle(fusionType == fusionType.GetMin, fusionType.GetMin.ToString(), EditorStyles.radioButton))
                    fusionType = fusionType.GetMin;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                break;
            case utilityType.Action:
                nodeName = CleanName(EditorGUILayout.TextArea(nodeName, Styles.TitleText, GUILayout.ExpandWidth(true), GUILayout.Height(25)));
                break;
            case utilityType.Curve:
                nodeName = CleanName(EditorGUILayout.TextArea(nodeName, Styles.TitleText, GUILayout.ExpandWidth(true), GUILayout.Height(25)));

                // Type of curve selector
                GUILayout.BeginHorizontal();
                GUILayout.Space(windowRect.width * 0.2f);
                if (GUILayout.Button(curveType.ToString(), EditorStyles.toolbarDropDown))
                {
                    GenericMenu toolsMenu = new GenericMenu();

                    foreach (string name in Enum.GetNames(typeof(curveType)).ToArray())
                    {
                        toolsMenu.AddItem(new GUIContent(name), false, () =>
                        {
                            curveType = (curveType)Enum.Parse(typeof(curveType), name);
                        });
                    }

                    toolsMenu.DropDown(new Rect(0, Event.current.mousePosition.y, 0, 0));
                    EditorGUIUtility.ExitGUI();
                }
                GUILayout.Space(windowRect.width * 0.2f);
                GUILayout.EndHorizontal();

                // Parameters
                switch (curveType)
                {
                    case curveType.Linear:
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(windowRect.width * 0.2f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("y = ", Styles.NonEditable, GUILayout.Width(20), GUILayout.Height(25));
                        float.TryParse(EditorGUILayout.TextArea(slope.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out slope);
                        GUILayout.Space(2);
                        GUILayout.Label("x + ", Styles.NonEditable, GUILayout.Width(20), GUILayout.Height(25));
                        float.TryParse(EditorGUILayout.TextArea(displX.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out displX);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(windowRect.width * 0.2f);
                        GUILayout.EndHorizontal();
                        break;
                    case curveType.Exponential:
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(windowRect.width * 0.2f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("y = (x - ", Styles.NonEditable, GUILayout.Width(20), GUILayout.Height(25));
                        GUILayout.Space(15);
                        float.TryParse(EditorGUILayout.TextArea(displY.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out displY);
                        GUILayout.Label(")", Styles.NonEditable, GUILayout.Width(10), GUILayout.Height(25));
                        float.TryParse(EditorGUILayout.TextArea(exp.ToString(), Styles.Exponent, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out exp);
                        GUILayout.Space(2);
                        GUILayout.Label(" + ", Styles.NonEditable, GUILayout.Width(20), GUILayout.Height(25));
                        float.TryParse(EditorGUILayout.TextArea(displX.ToString(), Styles.TitleText, GUILayout.ExpandWidth(false), GUILayout.Height(20)), out displX);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(windowRect.width * 0.2f);
                        GUILayout.EndHorizontal();
                        break;
                    case curveType.LinearParts:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Not implemented yet", Styles.WarningLabel);
                        GUILayout.EndHorizontal();
                        break;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                try
                {
                    openFoldout = EditorGUILayout.Foldout(openFoldout, "Visualize curve");

                    if (openFoldout)
                    {
                        windowRect.height = height * 2.5f;

                        Rect rect = new Rect(windowRect.width * 0.2f, windowRect.height * 0.6f, windowRect.width * 0.6f, 50);
                        EditorGUI.DrawRect(rect, new Color(0, 0, 1, 0.25f));

                        float yMin = -1;
                        float yMax = 1;
                        float step = 1 / editor.position.width;
                        float topThreshold = 1;
                        float bottomThreshold = -1;

                        Handles.color = new Color(0.6f, 0.6f, 0.6f);
                        DrawAxis(rect, yMin, yMax);

                        Handles.color = Color.white;

                        Vector3 prevPos = new Vector3(0, CurveFunc(-1), 0);
                        for (float t = step - 1; t < 1; t += step)
                        {
                            Vector3 pos = new Vector3((t + 1) / 2, CurveFunc(t), 0);

                            if (pos.y < topThreshold && pos.y > bottomThreshold)
                            {
                                Handles.DrawLine(
                                    new Vector3(rect.xMin + prevPos.x * rect.width, rect.yMax - ((prevPos.y - yMin) / (yMax - yMin)) * rect.height, 0),
                                    new Vector3(rect.xMin + pos.x * rect.width, rect.yMax - ((pos.y - yMin) / (yMax - yMin)) * rect.height, 0));
                            }

                            prevPos = pos;
                        }
                    }
                    else
                    {
                        windowRect.height = height * 1.5f;
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                break;
        }
    }

    private float CurveFunc(float t)
    {
        switch (curveType)
        {
            case curveType.Linear:
                return slope * t + displX;
            case curveType.Exponential:
                return Mathf.Pow(t - displY, exp) + displX;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Draws the axis in the center of the rect
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="yMin"></param>
    /// <param name="yMax"></param>
    private void DrawAxis(Rect rect, float yMin, float yMax)
    {
        Handles.DrawLine(
            new Vector3(rect.xMin + 0 * rect.width, rect.yMax - ((0 - yMin) / (yMax - yMin)) * rect.height, 0), 
            new Vector3(rect.xMin + 1 * rect.width, rect.yMax - ((0 - yMin) / (yMax - yMin)) * rect.height, 0));
        Handles.DrawLine(
            new Vector3(rect.xMin + 0.5f * rect.width, rect.yMax - ((-1 - yMin) / (yMax - yMin)) * rect.height, 0),
            new Vector3(rect.xMin + 0.5f * rect.width, rect.yMax - ((1 - yMin) / (yMax - yMin)) * rect.height, 0));
    }

    // TODO
    /// <summary>
    /// Creates and returns an <see cref="XMLElement"/> that corresponds to this <see cref="UtilityNode"/>
    /// </summary>
    /// <param name="args"></param>
    /// <returns>The <see cref="XMLElement"/> corresponding to this <see cref="BehaviourNode"/></returns>
    public override XMLElement ToXMLElement(params object[] args)
    {
        BehaviourTree parentTree = (BehaviourTree)args[0];

        XMLElement result;
        if (this.subElem)
        {
            result = this.subElem.ToXMLElement();
        }
        else
        {
            result = new XMLElement
            {
                name = CleanName(this.nodeName),
                elemType = this.GetType().ToString(),
                windowPosX = this.windowRect.x,
                windowPosY = this.windowRect.y,
                //isRandom = this.isRandom,
                //NProperty = this.NProperty,

                nodes = parentTree.connections.FindAll(o => this.Equals(o.fromNode)).Select(o => o.toNode).Cast<BehaviourNode>().ToList().ConvertAll((node) =>
                {
                    return node.ToXMLElement(parentTree);
                }),
            };
        }

        result.Id = this.identificator;
        result.secondType = this.type.ToString();

        return result;
    }

    /// <summary>
    /// Creates a copy of this <see cref="UtilityNode"/>
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override GUIElement CopyElement(params object[] args)
    {
        UtilitySystem parent = (UtilitySystem)args[0];

        GUIElement result = new UtilityNode
        {
            identificator = this.identificator,
            nodeName = this.nodeName,
            parent = parent,
            windowRect = new Rect(this.windowRect),
            type = this.type,
            fusionType = this.fusionType,
            curveType = this.curveType,
            variableMax = this.variableMax,
            variableMin = this.variableMin
        };

        if (this.subElem)
        {
            ((UtilityNode)result).subElem = (ClickableElement)this.subElem.CopyElement(parent);
        }

        return result;
    }

    /// <summary>
    /// Updates the value of the weights accordingly
    /// </summary>
    public void WeightsUpdate(string id)
    {
        List<TransitionGUI> weightedTransitions = ((UtilitySystem)parent).connections.Where(t => t.toNode.Equals(this)).ToList();
        float sumOfWeights = weightedTransitions.Sum(t => t.weight);

        if (sumOfWeights != 1)
        {
            foreach (TransitionGUI transition in weightedTransitions.Where(t => t.identificator != id))
            {
                transition.weight += (1 - sumOfWeights) * transition.weight / weightedTransitions.Where(t => t.identificator != id).Sum(t => t.weight);

                transition.weight = (float)decimal.Round((decimal)transition.weight, 2);
            }
        }
    }

    // TODO Ordenar la lista para que cuadre con la lista de factors asociada
    /// <summary>
    /// Returns a list of all the weights associated with this Fusion node
    /// </summary>
    /// <returns></returns>
    public List<float> GetWeightsAndFactors()
    {
        List<float> weights = ((UtilitySystem)parent).connections.Where(t => t.toNode.Equals(this)).Select(t => t.weight).ToList();

        return weights;
    }
}