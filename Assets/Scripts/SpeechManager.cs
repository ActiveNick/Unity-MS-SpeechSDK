//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): 
// https://www.microsoft.com/cognitive-services
//
// New Speech Service: 
// https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/
// Old Bing Speech SDK: 
// https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/home
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Comment the following line if you want to use the old Bing Speech SDK
// instead of the new Speech Service.
#define USENEWSPEECHSDK

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using SpeechRecognitionService;

public class SpeechManager : MonoBehaviour {

    // Public fields
    public Text DisplayLabel;

    // Private fields
    CogSvcSocketAuthentication auth;
    SpeechRecognitionClient recoServiceClient;
    AudioSource audio;
    bool isAuthenticated = false;
    bool isRecording = false;
    int maxRecordingDuration = 10;  // in seconds
    string region;

    // Use this for initialization
    void Start () {
        Debug.Log($"Initiating Cognitive Services Speech Recognition Service.");

        audio = GetComponent<AudioSource>();

        InitializeSpeechRecognitionService();
    }

    // Update is called once per frame
    void Update () {
		
	}

    /// <summary>
    /// InitializeSpeechRecognitionService
    /// </summary>
    private void InitializeSpeechRecognitionService()
    {
        // If you see an API key below, it's a trial key and will either expire soon or get invalidated. Please get your own key.
        // Get your own trial key to Bing Speech or the new Speech Service at https://azure.microsoft.com/try/cognitive-services
        // Create an Azure Cognitive Services Account: https://docs.microsoft.com/azure/cognitive-services/cognitive-services-apis-create-account

        // DELETE THE NEXT THREE LINE ONCE YOU HAVE OBTAINED YOUR OWN SPEECH API KEY
        //Debug.Log("You forgot to initialize the sample with your own Speech API key. Visit https://azure.microsoft.com/try/cognitive-services to get started.");
        //Console.ReadLine();
        //return;
        // END DELETE
#if USENEWSPEECHSDK
        bool useClassicBingSpeechService = false;
        //string authenticationKey = @"INSERT-YOUR-NEW-SPEECH-API-KEY-HERE";
        string authenticationKey = @"f69d77d425e946e69a954c53db135f77";
#else
        bool useClassicBingSpeechService = true;
        //string authenticationKey = @"INSERT-YOUR-BING-SPEECH-API-KEY-HERE";
        string authenticationKey = @"4d5a1beefe364f8986d63a877ebd51d5";
#endif

        Debug.Log($"Instantiating Cognitive Services Speech Recognition Service client.");
        recoServiceClient = new SpeechRecognitionClient(useClassicBingSpeechService);
        
        // Make sure to match the region to the Azure region where you created the service.
        // Note the region is NOT used for the old Bing Speech service
        region = "westus";

        auth = new CogSvcSocketAuthentication();
        Task<string> authenticating = auth.Authenticate(authenticationKey, region, useClassicBingSpeechService);

        // Since the authentication process needs to run asynchronously, we run the code in a coroutine to
        // avoid blocking the main Unity thread.
        // Make sure you have successfully obtained a token before making any Speech Service calls.
        StartCoroutine(AuthenticateSpeechService(authenticating));

        // Register an event to capture recognition events
        Debug.Log($"Registering Speech Recognition event handler.");
        recoServiceClient.OnMessageReceived += RecoServiceClient_OnMessageReceived;
    }

    /// <summary>
    /// CoRoutine that checks to see if the async authentication process has completed. Once it completes,
    /// retrieves the token that will be used for subsequent Cognitive Services Text-to-Speech API calls.
    /// </summary>
    /// <param name="authenticating"></param>
    /// <returns></returns>
    private IEnumerator AuthenticateSpeechService(Task<string> authenticating)
    {
        // Yield control back to the main thread as long as the task is still running
        while (!authenticating.IsCompleted)
        {
            yield return null;
        }

        try
        {
            isAuthenticated = true;
            Debug.Log($"Authentication token obtained: {auth.GetAccessToken()}");
        }
        catch (Exception ex)
        {
            Debug.Log("Failed authentication.");
            Debug.Log(ex.ToString());
            Debug.Log(ex.Message);
        }
    }

    /// <summary>
    /// Triggered from btnStartReco UI Canvas button.
    /// </summary>
    public void StartSpeechRecognitionFromFile()
    {
        // Replace this with your own file. Add it to the project and mark it as "Content" and "Copy if newer".
        string audioFilePath = Path.Combine(Application.streamingAssetsPath, "Thisisatest.wav");
        Debug.Log($"Using speech audio file located at {audioFilePath}");

        Debug.Log($"Creating Speech Recognition job.");
        Task<bool> recojob = recoServiceClient.CreateSpeechRecognitionJob(audioFilePath, auth.GetAccessToken(), region);

        StartCoroutine(CompleteSpeechRecognitionJob(recojob));
        Debug.Log($"Speech Recognition job started.");
    }

    /// <summary>
    /// Triggered from btnStartMicrophone UI Canvas button.
    /// </summary>
    public void StartSpeechRecognitionFromMicrophone()
    {
        audio.clip = Microphone.Start(Microphone.devices[0], true, maxRecordingDuration, 22050);
        audio.loop = true;

        
        
        // Wait until the microphone starts recording
        while (!(Microphone.GetPosition(null) > 0)) { } ;
        isRecording = true;
    }

    IEnumerator CompleteSpeechRecognitionJob(Task<bool> recojob)
    {
        // Yield control back to the main thread as long as the task is still running
        while (!recojob.IsCompleted)
        {
            yield return null;
        }
        Debug.Log($"Speech Recognition job completed.");
    }

    /// <summary>
    /// RecoServiceClient_OnMessageReceived event handler:
    /// This event handler gets fired every time a new message comes back via WebSocket.
    /// </summary>
    /// <param name="result"></param>
    private void RecoServiceClient_OnMessageReceived(SpeechServiceResult result)
    {
        if (result.Path == SpeechServiceResult.SpeechMessagePaths.SpeechHypothesis)
        {
            DisplayLabel.text = result.Result.Text;
            DisplayLabel.fontStyle = FontStyle.Italic;
        }
        else if (result.Path == SpeechServiceResult.SpeechMessagePaths.SpeechPhrase)
        {
            DisplayLabel.text = result.Result.DisplayText;
            DisplayLabel.fontStyle = FontStyle.Normal;

            Debug.Log("* RECOGNITION STATUS: " + result.Result.RecognitionStatus);
            Debug.Log("* FINAL RESULT: " + result.Result.DisplayText);
        }
    }

}
