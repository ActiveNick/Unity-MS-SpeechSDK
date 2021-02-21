using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [SerializeField] private CommandList commandList;
    [SerializeField] private List<string> recognizedPhrases;
    [SerializeField] private string currentPhrase;
    public void SetRecognizedText(string text)
    {
        recognizedPhrases.Add(text);
        string output = new string(text.Where(c => !char.IsPunctuation(c)).ToArray());
        var splittedText = output.Split();
        foreach (var word in splittedText)
        {
            Command foundCommand = commandList.commands.FirstOrDefault(command => command.text.ToLower() == word.ToLower());
            if (foundCommand != null)
            {
                RunCommand(foundCommand);
            }
        }
    }

    public void SetCurrentText(string text)
    {
        currentPhrase = text;
    }

    private void RunCommand(Command command)
    {
        switch (command.commandEvent.commandType)
        {
            case CommandType.Spawn:
                SpawnObject(command.commandEvent.prefab);
                break;
        }
    }

    private void SpawnObject(GameObject prefab)
    {
        Instantiate(prefab, transform);
    }

    private void Start()
    {
       var testCommand = commandList.commands.FirstOrDefault(command => command.text == "Поставить"); 
       Debug.Log(testCommand.commandEvent.color.ToString());
    }
}
