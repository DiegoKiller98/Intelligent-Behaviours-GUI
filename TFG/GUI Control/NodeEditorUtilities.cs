﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

public class NodeEditorUtilities
{
    /// <summary>
    /// C#'s Script Icon [The one MonoBhevaiour Scripts have].
    /// </summary>
    private readonly static Texture2D scriptIcon = (EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D);

    static readonly string tab = "    ";
    static readonly string actionsEnding = "Action";
    static readonly string conditionsEnding = "SuccessCheck";
    static readonly string subFSMEnding = "_SubFSM";
    static readonly string subBtEnding = "_SubBT";

    static readonly string savesFolderName = "Intelligent Behaviours Saves";
    static readonly string scriptsFolderName = "Intelligent Behaviours Scripts";

    static UniqueNamer uniqueNamer;

    /// <summary>
    /// Generates a new C# script for an element.
    /// </summary>
    public static void GenerateElemScript(ClickableElement elem)
    {
        uniqueNamer = ScriptableObject.CreateInstance<UniqueNamer>();

        string path = "none";

        switch (elem.GetType().ToString())
        {
            case nameof(FSM):
                path = "FSM_Template.cs";
                break;
            case nameof(BehaviourTree):
                path = "BT_Template.cs";
                break;
        }
        string[] guids = AssetDatabase.FindAssets(path);
        if (guids.Length == 0)
        {
            Debug.LogWarning(path + ".txt not found in asset database");
            return;
        }
        string templatePath = AssetDatabase.GUIDToAssetPath(guids[0]);

        // Create Asset
        if (!AssetDatabase.IsValidFolder("Assets/" + scriptsFolderName))
            AssetDatabase.CreateFolder("Assets", scriptsFolderName);

        string scriptPath = EditorUtility.SaveFilePanel("Select a folder for the script", "Assets/" + scriptsFolderName, CleanName(elem.elementName) + ".cs", "CS");

        if (!string.IsNullOrEmpty(scriptPath))
        {
            UnityEngine.Object o = CreateScript(scriptPath, templatePath, elem);
            AssetDatabase.Refresh();
            ProjectWindowUtil.ShowCreatedAsset(o);
        }
    }

    /// <summary>
    /// Generates a new XML file for an element.
    /// </summary>
    public static void GenerateElemXML(ClickableElement elem)
    {
        // Create Asset
        if (!AssetDatabase.IsValidFolder("Assets/" + savesFolderName))
            AssetDatabase.CreateFolder("Assets", savesFolderName);

        string path = EditorUtility.SaveFilePanel("Select a folder for the save file", "Assets/" + savesFolderName, CleanName(elem.elementName) + "_savedData.xml", "XML");

        if (!string.IsNullOrEmpty(path))
        {
            UnityEngine.Object o = CreateXML(path, elem);
            AssetDatabase.Refresh();
            ProjectWindowUtil.ShowCreatedAsset(o);
        }
    }

    public static XMLElement LoadSavedData()
    {
        string path = EditorUtility.OpenFilePanel("Open a save file", "Assets/Intelligent Behaviours Saves", "XML");

        if (string.IsNullOrEmpty(path))
            return null;

        return LoadXML(path);
    }

    /// <summary>
    /// Creates Script from Template's path.
    /// </summary>
    private static UnityEngine.Object CreateScript(string pathName, string templatePath, object obj)
    {
        string templateText = string.Empty;

        string folderPath = pathName.Substring(0, pathName.LastIndexOf("/") + 1);

        UTF8Encoding encoding = new UTF8Encoding(true, false);

        if (File.Exists(templatePath))
        {
            // Read procedures
            StreamReader reader = new StreamReader(templatePath);
            templateText = reader.ReadToEnd();
            reader.Close();

            if (obj is ClickableElement)
            {
                ClickableElement elem = (ClickableElement)obj;

                // Replace the tags with the corresponding parts
                List<ClickableElement> subElems = new List<ClickableElement>();

                templateText = templateText.Replace("#SCRIPTNAME#", CleanName(elem.elementName));

                switch (elem.GetType().ToString())
                {
                    case nameof(FSM):
                        templateText = templateText.Replace("#ENDING#", "_FSM");
                        templateText = templateText.Replace("#FSMCREATE#", GetFSMCreate(elem, "_FSM", false, ref subElems, folderPath));
                        templateText = GetAllSubElemsRecursive(templateText, ref subElems, folderPath);
                        templateText = templateText.Replace("#SUBELEMCREATE#", string.Empty);
                        break;
                    case nameof(BehaviourTree):
                        templateText = templateText.Replace("#ENDING#", "_BT");
                        templateText = templateText.Replace("#BTCREATE#", GetBTCreate(elem, "_BT", false, ref subElems));
                        templateText = GetAllSubElemsRecursive(templateText, ref subElems, folderPath);
                        templateText = templateText.Replace("#SUBELEMCREATE#", string.Empty);
                        break;
                }
                templateText = templateText.Replace("#ACTIONS#", GetMethods(elem));

                // SubFSM
                templateText = templateText.Replace("#SUBELEM1#", GetSubElemDecl(elem, subElems));
                templateText = templateText.Replace("#SUBELEM2#", GetSubElemInit(elem, subElems));
                templateText = templateText.Replace("#SUBELEM3#", GetSubFSMUpdate(elem, subElems));
            }
            else if (obj is string)
            {
                string elemName = obj.ToString();

                templateText = templateText.Replace("#CUSTOMNAME#", elemName);
            }

            /// You can replace as many tags you make on your templates, just repeat Replace function
            /// e.g.:
            /// templateText = templateText.Replace("#NEWTAG#", "MyText");

            // Write procedures
            StreamWriter writer = new StreamWriter(Path.GetFullPath(pathName), false, encoding);
            writer.Write(templateText);
            writer.Close();

            AssetDatabase.ImportAsset(pathName);
            return AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
        }
        else
        {
            Debug.LogError(string.Format("The template file was not found: {0}", templatePath));
            return null;
        }
    }

    /// <summary>
    /// Creates XML object and serializes it to a file.
    /// </summary>
    private static UnityEngine.Object CreateXML(string pathName, ClickableElement elem)
    {
        var data = elem.ToXMLElement();

        // Serialize to XML
        using (var stream = new FileStream(pathName, FileMode.Create))
        {
            XmlSerializer serial = new XmlSerializer(typeof(XMLElement));
            serial.Serialize(stream, data);
        }

        return null;
    }

    /// <summary>
    /// Loads an XML file and converts it to XMLElement
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private static XMLElement LoadXML(string fileName)
    {
        XmlSerializer serial = new XmlSerializer(typeof(XMLElement));

        serial.UnknownNode += new XmlNodeEventHandler(UnknownNode);
        serial.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttribute);

        FileStream fs = new FileStream(fileName, FileMode.Open);

        return (XMLElement)serial.Deserialize(fs);
    }

    private static void UnknownNode(object sender, XmlNodeEventArgs e)
    {
        Debug.LogError("[XMLSerializer] Unknown Node:" + e.Name + "\t" + e.Text);
    }

    private static void UnknownAttribute(object sender, XmlAttributeEventArgs e)
    {
        System.Xml.XmlAttribute attr = e.Attr;
        Debug.LogError("[XMLSerializer] Unknown attribute " +
        attr.Name + "='" + attr.Value + "'");
    }

    /// <summary>
    /// Modifies the given string to be usable in code
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string CleanName(string name)
    {
        string result;
        var numberChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        var spacesAndNewlines = new[] { ' ', '\n' };

        result = name.Trim(spacesAndNewlines);
        result = string.Concat(result.Where(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsSymbol(c)));
        result = result.TrimStart(numberChars);

        return result;
    }

    private static string GetAllSubElemsRecursive(string templateText, ref List<ClickableElement> subElems, string folderPath = null)
    {
        List<ClickableElement> subElemsCopy = new List<ClickableElement>();
        foreach (ClickableElement sub in subElems)
        {
            if (sub is FSM)
                templateText = templateText.Replace("#SUBELEMCREATE#", GetFSMCreate(sub, subFSMEnding, true, ref subElemsCopy, folderPath));
            if (sub is BehaviourTree)
                templateText = templateText.Replace("#SUBELEMCREATE#", GetBTCreate(sub, subBtEnding, true, ref subElemsCopy));
        }
        if (subElemsCopy.Count > 0)
        {
            templateText = GetAllSubElemsRecursive(templateText, ref subElemsCopy, folderPath);
            subElems.AddRange(subElemsCopy);
        }

        return templateText;
    }

    private static string GetSubElemDecl(ClickableElement elem, List<ClickableElement> subElems)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;

        foreach (ClickableElement sub in subElems)
        {
            string engineEnding = sub is FSM ? subFSMEnding : sub is BehaviourTree ? subBtEnding : "";
            string type = sub is FSM ? "StateMachineEngine" : sub is BehaviourTree ? "BehaviourTreeEngine" : "";
            string elemName = CleanName(sub.elementName);
            result += "private " + type + " " + elemName + engineEnding + ";\n" + tab;
        }

        return result;
    }

    private static string GetSubElemInit(ClickableElement elem, List<ClickableElement> subElems)
    {

        string result = string.Empty;

        for (int i = subElems.Count - 1; i >= 0; i--)
        {
            string engineEnding = subElems[i] is FSM ? subFSMEnding : subElems[i] is BehaviourTree ? subBtEnding : "";
            string elemName = CleanName(subElems[i].elementName) + engineEnding;
            result += "Create" + elemName + "();\n" + tab + tab;
        }

        return result;
    }

    private static string GetFSMCreate(ClickableElement elem, string engineEnding, bool isSub, ref List<ClickableElement> subElems, string folderPath = null)
    {
        string className = CleanName(elem.elementName);
        string machineName = className + engineEnding;
        string createdName = isSub ? machineName : "StateMachine";
        string isSubStr = isSub ? "true" : "false";
        string templateSub = "\n"
        + tab + "private void Create" + createdName + "()\n"
        + tab + "{\n"
        + tab + tab + machineName + " = new StateMachineEngine(" + isSubStr + ");\n"
        + tab + tab + "\n"
        + tab + tab + "// Perceptions\n"
        + tab + tab + "// Modify or add new Perceptions, see the guide for more\n"
        + tab + tab + "#PERCEPIONS#\n"
        + tab + tab + "// States\n"
        + tab + tab + "#STATES#\n"
        + tab + tab + "// Transitions#TRANSITIONS#\n"
        + tab + "}";

        if (isSub)
            templateSub += "#SUBELEMCREATE#";

        templateSub = templateSub.Replace("#PERCEPIONS#", GetPerceptions(elem, engineEnding, folderPath));
        templateSub = templateSub.Replace("#STATES#", GetStates(elem, engineEnding, ref subElems));
        templateSub = templateSub.Replace("#TRANSITIONS#", GetTransitions(elem, engineEnding));

        return templateSub;
    }

    private static string GetBTCreate(ClickableElement elem, string engineEnding, bool isSub, ref List<ClickableElement> subElems)
    {
        string className = CleanName(elem.elementName);
        string machineName = className + engineEnding;
        string createdName = isSub ? machineName : "BehaviourTree";
        string isSubStr = isSub ? "true" : "false";
        string templateSub = "\n"
        + tab + "private void Create" + createdName + "()\n"
        + tab + "{\n"
        + tab + tab + machineName + " = new BehaviourTreeEngine(" + isSubStr + ");\n"
        + tab + tab + "\n"
        + tab + tab + "// Nodes\n"
        + tab + tab + "#NODES#\n"
        + tab + tab + "// Child adding#CHILDS#\n"
        + tab + tab + "// SetRoot\n"
        + tab + tab + "#SETROOT#\n"
        + tab + "}";

        if (isSub)
            templateSub += "#SUBELEMCREATE#";

        templateSub = templateSub.Replace("#NODES#", GetNodes(elem, engineEnding, ref subElems));
        templateSub = templateSub.Replace("#CHILDS#", GetChilds(elem, ref subElems));
        templateSub = templateSub.Replace("#SETROOT#", GetSetRoot(elem, engineEnding));

        return templateSub;
    }

    private static string GetSubFSMUpdate(ClickableElement elem, List<ClickableElement> subElems)
    {
        string result = string.Empty;

        foreach (ClickableElement sub in subElems)
        {
            string engineEnding = sub is FSM ? subFSMEnding : sub is BehaviourTree ? subBtEnding : "";
            string elemName = CleanName(sub.elementName);
            result += "\n" + tab + tab + elemName + engineEnding + ".Update();";
        }

        return result;
    }

    #region FSM
    private static string GetPerceptions(ClickableElement elem, string engineEnding, string folderPath = null)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;
        string machineName = className + engineEnding;

        foreach (TransitionGUI transition in ((FSM)elem).transitions)
        {
            string transitionName = CleanName(transition.transitionName);

            result += RecursivePerceptionsCode(transition.rootPerception, transitionName, machineName, folderPath);
        }

        return result;
    }

    private static string RecursivePerceptionsCode(PerceptionGUI perception, string transitionName, string machineName, string folderPath)
    {
        string res = "";
        string auxAndOr = "";

        if (perception.type == perceptionType.And || perception.type == perceptionType.Or)
        {
            auxAndOr = perception.type.ToString();
            res += RecursivePerceptionsCode(perception.firstChild, transitionName, machineName, folderPath);
            res += RecursivePerceptionsCode(perception.secondChild, transitionName, machineName, folderPath);
        }

        string typeName;
        if (perception.type == perceptionType.Custom)
        {
            typeName = CleanName(perception.customName);

            string scriptName = typeName + "Perception.cs";

            // Generate the script for the custom perception if it doesn't exist already

            string[] assets = AssetDatabase.FindAssets(scriptName);
            if (assets.Length == 0)
            {
                string path = "CustomPerception_Template.cs";

                string[] guids = AssetDatabase.FindAssets(path);
                if (guids.Length == 0)
                {
                    Debug.LogWarning(path + ".txt not found in asset database");
                }
                else
                {
                    string templatePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    UnityEngine.Object o = CreateScript(folderPath + scriptName, templatePath, typeName);
                }
            }
        }
        else
        {
            typeName = perception.type.ToString();
        }

        uniqueNamer.AddName(perception.identificator, transitionName + "_" + typeName + "Perception");
        res += "Perception " + uniqueNamer.GetName(perception.identificator) + " = " + machineName + ".Create" + auxAndOr + "Perception<" + typeName + "Perception" + ">(" + GetPerceptionParameters(perception) + ");\n" + tab + tab;

        return res;
    }

    private static string GetPerceptionParameters(PerceptionGUI perception)
    {
        string result = "";

        switch (perception.type)
        {
            case perceptionType.Timer:
                result = perception.timerNumber.ToString();
                break;
            case perceptionType.IsInState:
                result = CleanName(perception.elemName) + subFSMEnding + ", " + "\"" + perception.stateName + "\"";
                break;
            case perceptionType.BehaviourTreeStatus:
                result = CleanName(perception.elemName) + subBtEnding + ", " + "ReturnValues." + perception.status.ToString();
                break;
            case perceptionType.And:
            case perceptionType.Or:
                result = uniqueNamer.GetName(perception.firstChild.identificator) + ", " + uniqueNamer.GetName(perception.secondChild.identificator);
                break;
            case perceptionType.Custom:
                result = "new " + uniqueNamer.GetName(perception.identificator) + "()";
                break;
        }

        return result;
    }

    private static string GetStates(ClickableElement elem, string engineEnding, ref List<ClickableElement> subElems)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;
        string machineName = className + engineEnding;

        foreach (StateNode node in ((FSM)elem).states)
        {
            string nodeName = CleanName(node.nodeName);
            if (node.subElem is FSM)
            {
                result += "State " + nodeName + " = " + machineName + ".CreateSubStateMachine(\"" + node.nodeName + "\", " + nodeName + subFSMEnding + ");\n" + tab + tab;
                subElems.Add(node.subElem);
            }
            else if (node.subElem is BehaviourTree)
            {
                result += "State " + nodeName + " = " + machineName + ".CreateSubStateMachine(\"" + node.nodeName + "\", " + nodeName + subBtEnding + ");\n" + tab + tab;
                subElems.Add(node.subElem);

            }
            else if (node.type == stateType.Entry)
            {
                result += "State " + nodeName + " = " + machineName + ".CreateEntryState(\"" + node.nodeName + "\", " + nodeName + actionsEnding + ");\n" + tab + tab;
            }
            else
            {
                result += "State " + nodeName + " = " + machineName + ".CreateState(\"" + node.nodeName + "\", " + nodeName + actionsEnding + ");\n" + tab + tab;
            }
        }

        return result;
    }

    private static string GetTransitions(ClickableElement elem, string engineEnding)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;
        string machineName = className + engineEnding;

        foreach (TransitionGUI transition in ((FSM)elem).transitions)
        {
            string transitionName = CleanName(transition.transitionName);
            string fromNodeName = CleanName(transition.fromNode.nodeName);
            string toNodeName = CleanName(transition.toNode.nodeName);

            string typeName = "";

            if (transition.rootPerception.type == perceptionType.Custom)
            {
                typeName = transition.rootPerception.customName;
            }
            else
            {
                typeName = transition.rootPerception.type.ToString();
            }

            if (((StateNode)transition.fromNode).subElem != null)
            {
                string ending;

                if (((StateNode)transition.fromNode).subElem is FSM)
                    ending = "_SubFSM";
                else
                    ending = "_SubBT";

                string subClassName = CleanName(((StateNode)transition.fromNode).subElem.elementName);

                result += "\n" + tab + tab + subClassName + ending + ".CreateExitTransition(\"" + transition.transitionName + "\", " + fromNodeName + ", " + uniqueNamer.GetName(transition.rootPerception.identificator) + ", " + toNodeName + ");";
            }
            else
            {
                result += "\n" + tab + tab + machineName + ".CreateTransition(\"" + transition.transitionName + "\", " + fromNodeName + ", " + uniqueNamer.GetName(transition.rootPerception.identificator) + ", " + toNodeName + ");";
            }
        }

        if (elem.parent is BehaviourTree)
            result += "\n" + tab + tab + machineName + ".CreateExitTransition(\"" + machineName + " Exit" + "\", null /*Change this for a node*/, null /*Change this for a perception*/, ReturnValues.Succeed);";

        return result;
    }

    #endregion

    #region Behaviour Tree

    private static string GetNodes(ClickableElement elem, string engineEnding, ref List<ClickableElement> subElems)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;
        string machineName = className + engineEnding;

        foreach (BehaviourNode node in ((BehaviourTree)elem).nodes.FindAll(n => n.type <= BehaviourNode.behaviourType.Leaf))
        {
            string nodeName = CleanName(node.nodeName);

            switch (node.type)
            {
                case BehaviourNode.behaviourType.Selector:
                    result += "SelectorNode " + nodeName + " = " + machineName + ".CreateSelectorNode(\"" + node.nodeName + "\");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.Sequence:
                    result += "SequenceNode " + nodeName + " = " + machineName + ".CreateSequenceNode(\"" + node.nodeName + "\", " + (node.isRandomSequence ? "true" : "false") + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.Leaf:
                    if (node.subElem is FSM)
                    {
                        result += "LeafNode " + nodeName + " = " + machineName + ".CreateSubBehaviour(\"" + node.nodeName + "\", " + nodeName + subFSMEnding + ");\n" + tab + tab;
                        subElems.Add(node.subElem);
                    }
                    else if (node.subElem is BehaviourTree)
                    {
                        result += "LeafNode " + nodeName + " = " + machineName + ".CreateSubBehaviour(\"" + node.nodeName + "\", " + nodeName + subBtEnding + ");\n" + tab + tab;
                        subElems.Add(node.subElem);
                    }
                    else
                    {
                        result += "LeafNode " + nodeName + " = " + machineName + ".CreateLeafNode(\"" + node.nodeName + "\", " + nodeName + actionsEnding + ", " + nodeName + conditionsEnding + ");\n" + tab + tab;
                    }
                    break;
            }
        }

        // Decorator nodes
        // We check every node from the root so it is written in order in the generated code

        foreach (BehaviourNode node in ((BehaviourTree)elem).nodes.FindAll(o => o.isRootNode))
        {
            RecursiveDecorators(ref result, machineName, elem, node);
        }

        return result;
    }

    /// <summary>
    /// Check if it's a decorator and if it is, it writes it
    /// </summary>
    /// <param name="result"></param>
    /// <param name="machineName"></param>
    /// <param name="elem"></param>
    /// <param name="node"></param>
    private static void RecursiveDecorators(ref string result, string machineName, ClickableElement elem, BehaviourNode node)
    {
        foreach (BehaviourNode childNode in ((BehaviourTree)elem).connections.FindAll(o => o.fromNode.Equals(node)).Select(o => o.toNode))
        {
            RecursiveDecorators(ref result, machineName, elem, childNode);
        }

        if (node.type > BehaviourNode.behaviourType.Leaf)
        {
            string nodeName = CleanName(node.nodeName);
            TransitionGUI decoratorConnection = ((BehaviourTree)elem).connections.Where(t => node.Equals(t.fromNode)).FirstOrDefault();
            string subNodeName = CleanName(decoratorConnection.toNode.nodeName);
            TransitionGUI decoratorConnectionsub;

            switch (((BehaviourNode)decoratorConnection.toNode).type)
            {
                case BehaviourNode.behaviourType.LoopN:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "LoopN_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
                case BehaviourNode.behaviourType.LoopUntilFail:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "LoopUntilFail_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
                case BehaviourNode.behaviourType.Inverter:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "Inverter_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
                case BehaviourNode.behaviourType.DelayT:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "Timer_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
                case BehaviourNode.behaviourType.Succeeder:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "Succeeder_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
                case BehaviourNode.behaviourType.Conditional:
                    decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                    subNodeName = "Conditional_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                    break;
            }

            switch (node.type)
            {
                case BehaviourNode.behaviourType.LoopN:
                    string loopNodeName = "LoopN_" + subNodeName;
                    result += "LoopDecoratorNode " + loopNodeName + " = " + machineName + ".CreateLoopNode(\"" + loopNodeName + "\", " + subNodeName + ", " + node.NProperty + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.LoopUntilFail:
                    string loopUntilNodeName = "LoopUntilFail_" + subNodeName;
                    result += "LoopUntilFailDecoratorNode " + loopUntilNodeName + " = " + machineName + ".CreateLoopUntilFailNode(\"" + loopUntilNodeName + "\", " + subNodeName + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.Inverter:
                    string InverterNodeName = "Inverter_" + subNodeName;
                    result += "InverterDecoratorNode " + InverterNodeName + " = " + machineName + ".CreateInverterNode(\"" + InverterNodeName + "\", " + subNodeName + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.DelayT:
                    string TimerNodeName = "Timer_" + subNodeName;
                    result += "TimerDecoratorNode " + TimerNodeName + " = " + machineName + ".CreateTimerNode(\"" + TimerNodeName + "\", " + subNodeName + ", " + node.NProperty + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.Succeeder:
                    string SucceederNodeName = "Succeeder_" + subNodeName;
                    result += "SucceederDecoratorNode " + SucceederNodeName + " = " + machineName + ".CreateSucceederNode(\"" + SucceederNodeName + "\", " + subNodeName + ");\n" + tab + tab;
                    break;
                case BehaviourNode.behaviourType.Conditional:
                    string ConditionalNodeName = "Conditional_" + subNodeName;
                    result += "ConditionalDecoratorNode " + ConditionalNodeName + " = " + machineName + ".CreateConditionalNode(\"" + ConditionalNodeName + "\", " + subNodeName + ", null /*Change this for a perception*/);\n" + tab + tab;
                    break;
            }
        }
    }

    private static string GetChilds(ClickableElement elem, ref List<ClickableElement> subElems)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;

        foreach (BehaviourNode node in ((BehaviourTree)elem).nodes.Where(n => n.type < BehaviourNode.behaviourType.Leaf && ((BehaviourTree)elem).ChildrenCount(n) > 0))
        {
            string nodeName = CleanName(node.nodeName);
            result += "\n" + tab + tab;
            foreach (BehaviourNode toNode in ((BehaviourTree)elem).connections.FindAll(t => node.Equals(t.fromNode)).Select(o => o.toNode))
            {
                string toNodeName = CleanName(toNode.nodeName);
                TransitionGUI decoratorConnection = ((BehaviourTree)elem).connections.Where(t => toNode.Equals(t.fromNode)).FirstOrDefault();
                if (decoratorConnection != null)
                {
                    string subNodeName = CleanName(decoratorConnection.toNode.nodeName);
                    TransitionGUI decoratorConnectionsub;

                    switch (((BehaviourNode)decoratorConnection.toNode).type)
                    {
                        case BehaviourNode.behaviourType.LoopN:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "LoopN_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                        case BehaviourNode.behaviourType.LoopUntilFail:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "LoopUntilFail_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                        case BehaviourNode.behaviourType.Inverter:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "Inverter_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                        case BehaviourNode.behaviourType.DelayT:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "Timer_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                        case BehaviourNode.behaviourType.Succeeder:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "Succeeder_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                        case BehaviourNode.behaviourType.Conditional:
                            decoratorConnectionsub = ((BehaviourTree)elem).connections.Where(t => decoratorConnection.toNode.Equals(t.fromNode)).FirstOrDefault();
                            subNodeName = "Conditional_" + CleanName(decoratorConnectionsub.toNode.nodeName);
                            break;
                    }

                    switch (toNode.type)
                    {
                        case BehaviourNode.behaviourType.LoopN:
                            toNodeName = "LoopN_" + subNodeName;
                            break;
                        case BehaviourNode.behaviourType.LoopUntilFail:
                            toNodeName = "LoopUntilFail_" + subNodeName;
                            break;
                        case BehaviourNode.behaviourType.Inverter:
                            toNodeName = "Inverter_" + subNodeName;
                            break;
                        case BehaviourNode.behaviourType.DelayT:
                            toNodeName = "Timer_" + subNodeName;
                            break;
                        case BehaviourNode.behaviourType.Succeeder:
                            toNodeName = "Succeeder_" + subNodeName;
                            break;
                        case BehaviourNode.behaviourType.Conditional:
                            toNodeName = "Conditional_" + subNodeName;
                            break;
                    }
                }

                result += nodeName + ".AddChild(" + toNodeName + ");\n" + tab + tab;
            }
        }

        return result;
    }

    private static string GetSetRoot(ClickableElement elem, string engineEnding)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;
        string machineName = className + engineEnding;

        foreach (BehaviourNode node in ((BehaviourTree)elem).nodes)
        {
            if (node.isRootNode)
                result += machineName + ".SetRootNode(" + CleanName(node.nodeName) + ");";
        }

        return result;
    }

    #endregion

    private static string GetMethods(ClickableElement elem)
    {
        string className = CleanName(elem.elementName);
        string result = string.Empty;

        switch (elem.GetType().ToString())
        {
            case nameof(FSM):
                foreach (StateNode node in ((FSM)elem).states)
                {
                    if (node.subElem != null)
                    {
                        result += GetMethods(node.subElem);
                    }
                    else
                    {
                        string nodeName = CleanName(node.nodeName);
                        result += "\n" + tab +
                          "private void " + nodeName + actionsEnding + "()\n"
                          + tab + "{\n"
                          + tab + tab + "\n"
                          + tab + "}\n"
                          + tab;
                    }
                }
                break;
            case nameof(BehaviourTree):
                foreach (BehaviourNode node in ((BehaviourTree)elem).nodes.FindAll(n => n.type == BehaviourNode.behaviourType.Leaf))
                {
                    if (node.subElem != null)
                    {
                        result += GetMethods(node.subElem);
                    }
                    else
                    {
                        string nodeName = CleanName(node.nodeName);
                        result += "\n" + tab +
                          "private void " + nodeName + actionsEnding + "()\n"
                          + tab + "{\n"
                          + tab + tab + "\n"
                          + tab + "}\n"
                          + tab;

                        result += "\n" + tab +
                          "private ReturnValues " + nodeName + conditionsEnding + "()\n"
                          + tab + "{\n"
                          + tab + tab + "//Write here the code for the success check for " + nodeName + "\n"
                          + tab + tab + "return ReturnValues.Failed;\n"
                          + tab + "}\n"
                          + tab;
                    }
                }
                break;
        }

        return result;
    }
}
