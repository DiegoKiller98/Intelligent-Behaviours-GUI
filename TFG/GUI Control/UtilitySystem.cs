﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UtilitySystem : ClickableElement
{
    /// <summary>
    /// The Initializer for the <seealso cref="UtilitySystem"/>
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="parent"></param>
    /// <param name="posx"></param>
    /// <param name="posy"></param>
    /// <param name="id"></param>
    public void InitUtilitySystem(ClickableElement parent, float posx, float posy)
    {
        InitClickableElement();

        this.editor = EditorWindow.GetWindow<NodeEditor>();
        this.parent = parent;

        if (parent != null)
            elementName = parent.elementNamer.AddName(identificator, "New US ");
        else
            elementName = editor.editorNamer.AddName(identificator, "New US ");

        windowRect = new Rect(posx, posy, width, height);
    }

    /// <summary>
    /// The Initializer for the <seealso cref="UtilitySystem"/> when it is being loaded from an XML
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="parent"></param>
    /// <param name="posx"></param>
    /// <param name="posy"></param>
    /// <param name="id"></param>
    public void InitUtilitySystemFromXML(ClickableElement parent, float posx, float posy, string id, string name)
    {
        InitClickableElement(id);

        this.editor = EditorWindow.GetWindow<NodeEditor>();
        this.parent = parent;

        if (parent != null)
            elementName = parent.elementNamer.AddName(identificator, name);
        else
            elementName = editor.editorNamer.AddName(identificator, name);

        windowRect = new Rect(posx, posy, width, height);
    }

    // TODO
    /// <summary>
    /// Creates and returns an <see cref="XMLElement"/> that corresponds to this <see cref="BehaviourTree"/>
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override XMLElement ToXMLElement(params object[] args)
    {
        XMLElement result = new XMLElement
        {
            name = CleanName(this.elementName),
            elemType = this.GetType().ToString(),
            windowPosX = this.windowRect.x,
            windowPosY = this.windowRect.y,
            nodes = nodes.ConvertAll((node) =>
            {
                return node.ToXMLElement(this);
            }),
            transitions = transitions.ConvertAll((conn) =>
            {
                return conn.ToXMLElement(this);
            }),
            Id = this.identificator
        };

        return result;
    }

    /// <summary>
    /// Creates a copy of this <see cref="UtilitySystem"/>
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public override GUIElement CopyElement(params object[] args)
    {
        ClickableElement parent = (ClickableElement)args[0];

        GUIElement result = new UtilitySystem
        {
            identificator = this.identificator,
            elementNamer = CreateInstance<UniqueNamer>(),
            elementName = this.elementName,
            parent = parent,
            editor = this.editor,
            windowRect = new Rect(this.windowRect)
        };

        ((UtilitySystem)result).nodes = this.nodes.Select(o => (BaseNode)o.CopyElement(result)).ToList();
        ((UtilitySystem)result).transitions = this.transitions.Select(o =>
        (TransitionGUI)o.CopyElement(((UtilitySystem)result).nodes.Find(n => n.identificator == o.fromNode.identificator),
                                     ((UtilitySystem)result).nodes.Find(n => n.identificator == o.toNode.identificator))).ToList();

        return result;
    }

    /// <summary>
    /// Returns the type properly written
    /// </summary>
    /// <returns></returns>
    public override string GetTypeString()
    {
        return "Utility System";
    }

    /// <summary>
    /// Draws all <see cref="TransitionGUI"/> curves for the <see cref="BehaviourTree"/>
    /// </summary>
    public void DrawCurves()
    {
        foreach (TransitionGUI elem in transitions)
        {
            if (elem.fromNode is null || elem.toNode is null)
                break;

            Rect fromNodeRect = new Rect(elem.fromNode.windowRect);
            Rect toNodeRect = new Rect(elem.toNode.windowRect);

            NodeEditor.DrawNodeCurve(fromNodeRect, toNodeRect, editor.focusedObjects.Contains(elem));
        }
    }

    /// <summary>
    /// Deletes the <paramref name="node"/>
    /// </summary>
    /// <param name="node"></param>
    public void DeleteNode(UtilityNode node, bool deleteTransitions = true)
    {
        if (nodes.Remove(node))
        {
            if (deleteTransitions)
            {
                foreach (TransitionGUI transition in transitions.FindAll(t => node.Equals(t.fromNode) || node.Equals(t.toNode)))
                {
                    DeleteConnection(transition);
                }
            }

            if (node.subElem == null)
                elementNamer.RemoveName(node.identificator);
            else
                elementNamer.RemoveName(node.subElem.identificator);
        }
    }

    /// <summary>
    /// Delete <paramref name="connection"/>
    /// </summary>
    /// <param name="connection"></param>
    public void DeleteConnection(TransitionGUI connection)
    {
        if (transitions.Remove(connection))
        {
            elementNamer.RemoveName(connection.identificator);
        }
    }

    /// <summary>
    /// Checks wether <paramref name="start"/> could ever reach <paramref name="end"/> in the <see cref="UtilitySystem"/> execution
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public bool ConnectedCheck(UtilityNode start, UtilityNode end)
    {
        foreach (TransitionGUI transition in transitions.FindAll(t => start.Equals(t.toNode)))
        {
            if (end.Equals((UtilityNode)transition.fromNode))
            {
                return true;
            }
            if (ConnectedCheck((UtilityNode)transition.fromNode, end))
                return true;
        }

        return false;
    }

    public override List<ClickableElement> GetSubElems()
    {
        List<ClickableElement> result = new List<ClickableElement>();

        foreach (UtilityNode node in nodes)
        {
            if (node.subElem != null)
            {
                result.AddRange(node.subElem.GetSubElems());
                result.Add(node.subElem);
            }
        }

        return result;
    }
}
