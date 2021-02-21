using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

[CreateAssetMenu(fileName = "Command", menuName = "ScriptableObjects/CommandList", order = 1)]
public class CommandList : ScriptableObject
{
    public List<Command> commands;
}

[Serializable]
public class Command
{
    public string text;
    public CommandExecution commandEvent;
}

[Serializable]
public class CommandExecution
{
    public GameObject prefab;
    public Color color;
    public CommandType commandType;
}

[Serializable]
public enum CommandType
{
    Spawn,
    Delete,
    Find,
    Change
}