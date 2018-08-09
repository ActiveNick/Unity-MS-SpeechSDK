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
    string requestId;
    int maxRecordingDuration = 10;  // in seconds
    string region;

    // Microphone Recording Parameters
    int numChannels = 2;
    int samplingResolution = 16;
    int samplingRate = 44100;
    int recordingSamples = 0;
    List<byte> recordingData;

    // Use this for initialization
    void Start () {
        audio = GetComponent<AudioSource>();
        Debug.Log($"Audio settings playback rate currently set to {AudioSettings.outputSampleRate}Hz");
        // We need to make sure the microphone records at the same sampling rate as the audio
        // settings since we are using an audio filter to capture samples.
        samplingRate = AudioSettings.outputSampleRate;

        Debug.Log($"Initiating Cognitive Services Speech Recognition Service.");
        
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
    /// Feeds a pre-recorded speech audio file to the Speech Recognition service.
    /// Triggered from btnStartReco UI Canvas button.
    /// </summary>
    public void StartSpeechRecognitionFromFile()
    {
        // Replace this with your own file. Add it to the project and mark it as "Content" and "Copy if newer".
        string audioFilePath = Path.Combine(Application.streamingAssetsPath, "Thisisatest.wav");
        Debug.Log($"Using speech audio file located at {audioFilePath}");

        Debug.Log($"Creating Speech Recognition job from audio file.");
        Task<bool> recojob = recoServiceClient.CreateSpeechRecognitionJobFromFile(audioFilePath, auth.GetAccessToken(), region);

        StartCoroutine(CompleteSpeechRecognitionJob(recojob));
        Debug.Log($"Speech Recognition job started.");
    }

    /// <summary>
    /// Starts recording the user's voice via microphone and then feeds the audio data
    /// to the Speech Recognition service.
    /// Triggered from btnStartMicrophone UI Canvas button.
    /// </summary>
    public void StartSpeechRecognitionFromMicrophone()
    {
        Debug.Log($"Creating Speech Recognition job from microphone.");
        // Temporarily disabling this feature until it's fixed with the new sampling method

        //Task<bool> recojob = recoServiceClient.CreateSpeechRecognitionJobFromVoice(auth.GetAccessToken(), region);

        //StartCoroutine(WaitUntilRecoServiceIsReady());
    }

    /// <summary>
    /// CoRoutine that waits until the Speech Recognition job has been started.
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitUntilRecoServiceIsReady()
    {
        while (recoServiceClient.State != SpeechRecognitionClient.JobState.ReadyForAudioPackets &&
            recoServiceClient.State != SpeechRecognitionClient.JobState.Error)
        {
            yield return null;
        }

        if (recoServiceClient.State == SpeechRecognitionClient.JobState.ReadyForAudioPackets) {

            requestId = recoServiceClient.CurrentRequestId;

            Debug.Log("Initializing microphone for recording.");
            // Passing null for deviceName in Microphone methods to use the default microphone.
            audio.clip = Microphone.Start(null, true, maxRecordingDuration, samplingRate);
            audio.loop = true;

            // Wait until the microphone starts recording
            while (!(Microphone.GetPosition(null) > 0)) { };
            isRecording = true;
            audio.Play();
            Debug.Log("Microphone recording has started.");
        } 
        else
        {
            // Something went wrong during job initialization, handle it
            Debug.Log("Something went wrong during job initialization.");
        }
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

    void OnAudioFilterRead(float[] data, int channels)
    {
        //Debug.Log($"Received audio data of size: {data.Length} - First sample: {data[0]}");
        Debug.Log($"Received audio data: {channels} channel(s), size {data.Length} samples.");

        if (isRecording)
        {
            byte[] audiodata = ConvertAudioClipDataToInt16ByteArray(data);
            for (int i = 0; i < data.Length; i++)
            {
                // Mute all the samples to avoid audio feedback into the microphone
                data[i] = 0.0f;
            }
            recordingData.AddRange(audiodata);
            recordingSamples += audiodata.Length;
        }
        //recoServiceClient.SendAudioPacket(requestId, audiodata);
    }

    /// <summary>
    /// Converts audio data from Unity's array of floats to a WAV-compatible byte array.
    /// Thanks to my colleague David Douglas for this method from his WavUtility class.
    /// Source: https://github.com/deadlyfingers/UnityWav/blob/master/WavUtility.cs
    /// </summary>
    private static byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
    {
        MemoryStream dataStream = new MemoryStream();

        int x = sizeof(Int16);
        Int16 maxValue = Int16.MaxValue;
        int i = 0;
        while (i < data.Length)
        {
            dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
            ++i;
        }
        byte[] bytes = dataStream.ToArray();

        // Validate converted bytes
        Debug.AssertFormat(data.Length * x == bytes.Length, "Unexpected float[] to Int16 to byte[] size: {0} == {1}", data.Length * x, bytes.Length);

        dataStream.Dispose();
        return bytes;
    }

    /// <summary>
    /// Used only to record the microphone to a WAV file for testing purposes.
    /// </summary>
    public void StartRecording()
    {
        Debug.Log("Initializing microphone for recording to audio file.");
        recordingData = new List<byte>();
        recordingSamples = 0;

        // Passing null for deviceName in Microphone methods to use the default microphone.
        audio.clip = Microphone.Start(null, true, 1, samplingRate);
        audio.loop = true;

        // Wait until the microphone starts recording
        while (!(Microphone.GetPosition(null) > 0)) { };
        isRecording = true;
        audio.Play();
        Debug.Log("Microphone recording has started.");
    }

    /// <summary>
    /// Stops the microphone recording and saves to a WAV file. Used to validate WAV format.
    /// </summary>
    public void StopRecording()
    {
        Debug.Log("Stopping microphone recording.");
        audio.Stop();
        Microphone.End(null);
        isRecording = false;
        Debug.Log($"Microphone stopped recording at frequency {audio.clip.frequency}Hz.");

        var audioData = new byte[recordingData.Count];
        recordingData.CopyTo(audioData);
        WriteAudioDataToRiffWAVFile(audioData);
    }

    /// <summary>
    /// Saves a byte array of audio samples to a properly formatted WAV file.
    /// </summary>
    /// <param name="audiodata"></param>
    private void WriteAudioDataToRiffWAVFile(byte [] audiodata)
    {
        string filePath = Path.Combine(Application.temporaryCachePath, "recording.wav");
        Debug.Log($"Opening new WAV file for recording: {filePath}");

        FileStream fs = new FileStream(filePath, FileMode.Create);
        BinaryWriter wr = new BinaryWriter(fs);

        int bytesPerSample = samplingResolution / 8;

        // Writing WAV header
        Debug.Log($"Writing WAV header to file with a count of {recordingSamples} samples.");
        wr.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        // 36 is the total size of the header that follows. We don't count the RIFF line above (4) and this number (4).
        // The data already takes the number of channels and bytes per sample since we converted from Unity data.
        wr.Write(BitConverter.GetBytes(36 + recordingSamples), 0, 4); // * numChannels * bytesPerSample
        wr.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        wr.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);  // Format chunk marker. Includes trailing null 
        wr.Write(BitConverter.GetBytes(samplingResolution), 0, 4);    // e.g. 16 bits
        UInt16 audioFormat = 1;     // Type of format (1 is PCM) - 2 byte integer 
        wr.Write(BitConverter.GetBytes(audioFormat), 0, 2);     
        wr.Write(BitConverter.GetBytes(Convert.ToUInt16(numChannels)), 0, 2);
        wr.Write(BitConverter.GetBytes(samplingRate), 0, 4);
        wr.Write(BitConverter.GetBytes(samplingRate * bytesPerSample  * numChannels), 0, 4);     // byte rate  
        wr.Write(BitConverter.GetBytes(Convert.ToUInt16(bytesPerSample * numChannels)), 0, 2);  // block align
        wr.Write(BitConverter.GetBytes(Convert.ToUInt16(samplingResolution)), 0, 2);            // bit depth
        // Start of the data section
        wr.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);  // "data" chunk header. Marks the beginning of the data section. 
        wr.Write(BitConverter.GetBytes(recordingSamples), 0, 4);  // Size of the data section. // * bytesPerSample
        // Write the audio data to the main file body
        Debug.Log($"Writing {audiodata.Length} WAV data samples to file.");
        wr.Write(audiodata, 0, audiodata.Length);

        wr.Close();
        fs.Close();
        Debug.Log($"Completed writing {audiodata.Length} WAV data samples to file.");
    }
}
