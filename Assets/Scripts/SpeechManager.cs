using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using UnityEngine;

public class SpeechManager : MonoBehaviour {

    SpeechFactory factory;
    SpeechRecognizer recognizer;

	// Use this for initialization
	void Start () {

        // Creates an instance of a speech factory with specified
        // subscription key and service region. Replace with your own subscription key
        // and service region (e.g., "westus").
        factory = SpeechFactory.FromSubscription("f69d77d425e946e69a954c53db135f77", "westus");

        recognizer = factory.CreateSpeechRecognizer();

        StartSpeechRecognitionAsync();
    }

    // Update is called once per frame
    void Update () {
		
	}

    public async void StartSpeechRecognitionAsync()
    {
        // Performs recognition.
        // RecognizeAsync() returns when the first utterance has been recognized, so it is suitable 
        // only for single shot recognition like command or query. For long-running recognition, use
        // StartContinuousRecognitionAsync() instead.
        var result = await recognizer.RecognizeAsync();

        // Checks result.
        if (result.RecognitionStatus != RecognitionStatus.Recognized)
        {
            Debug.Log($"Recognition status: {result.RecognitionStatus.ToString()}");
            if (result.RecognitionStatus == RecognitionStatus.Canceled)
            {
                Debug.Log($"There was an error, reason: {result.RecognitionFailureReason}");
            }
            else
            {
                Debug.Log("No speech could be recognized.\n");
            }
        }
        else
        {
            Debug.Log($"We recognized: {result.Text}");
        }

    }
}
