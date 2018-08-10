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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SpeechRecognitionService
{
    public class SpeechRecognitionClient
    {
        // The various job states of the service. Ready & Completed both indicate the service is ready to
        // receive a new job. The latter indicates a job was completed and a new one is ready.
        public enum JobState { Initializing, Ready, PreparingJob, ReadyForAudioPackets, ProcessingAudio, Completed, Error }

        // Public Fields
        public string RecognizedText { get; set; }
        public SpeechServiceResult LastMessageReceived { get; set; }
        public ClientWebSocket SpeechWebSocketClient { get; set; }
        public string CurrentRequestId { get; set; }
        public JobState State
        {
            get { return state; }
        }

        // Private fields
        bool useClassicBingSpeechService = false;
        private JobState state;

        // Events Definition
        public delegate void MessageReceived(SpeechServiceResult result);
        public event MessageReceived OnMessageReceived;

        public SpeechRecognitionClient(bool usebingspeechservice = false)
        {
            state = JobState.Initializing;
            // Set usebingspeechservice to true in  the client constructor if you want to use the old Bing Speech SDK
            // instead of the new Speech Service.
            useClassicBingSpeechService = usebingspeechservice;

            state = JobState.Ready;
        }

        public async Task<bool> CreateSpeechRecognitionJobFromFile(string audioFilePath, string authenticationToken, string region)
        {
            try
            {
                state = JobState.PreparingJob;

                SpeechWebSocketClient = await InitializeSpeechWebSocketClient(authenticationToken, region);

                var receiving = Receiving(SpeechWebSocketClient);

                var sending = Task.Run(async () =>
                {
                    // Create a unique request ID, must be a UUID in "no-dash" format
                    var requestId = Guid.NewGuid().ToString("N");

                    ArraySegment<byte> buffer = CreateSpeechConfigMessagePayloadBuffer(requestId);

                    if (SpeechWebSocketClient.State != WebSocketState.Open) return;

                    Debug.Log("Sending speech.config...");
                    // Send speech.config to Speech Service
                    await SpeechWebSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, new CancellationToken());
                    Debug.Log("speech.config sent successfully!");

                    // SENDING AUDIO TO SPEECH SERVICE:
                    // Speech-enabled client applications send audio to Speech Service by converting the audio stream
                    // into a series of audio chunks. Each chunk of audio carries a segment of the spoken audio that's
                    // to be transcribed by the service. The maximum size of a single audio chunk is 8,192 bytes.
                    // Audio stream messages are Binary WebSocket messages.
                    Debug.Log($"Preparing to send audio file: {audioFilePath}");
                    FileInfo audioFileInfo = new FileInfo(audioFilePath);
                    FileStream audioFileStream = audioFileInfo.OpenRead();

                    state = JobState.ProcessingAudio;

                    byte[] headerBytes;
                    byte[] headerHead;
                    for (int cursor = 0; cursor < audioFileInfo.Length; cursor++)
                    {
                        headerBytes = BuildAudioPacketHeader(requestId);
                        headerHead = BuildAudioPacketHeaderHead(headerBytes);

                        // PCM audio must be sampled at 16 kHz with 16 bits per sample and one channel (riff-16khz-16bit-mono-pcm).
                        var byteLen = 8192 - headerBytes.Length - 2;
                        var fbuff = new byte[byteLen];
                        audioFileStream.Read(fbuff, 0, byteLen);

                        var arr = headerHead.Concat(headerBytes).Concat(fbuff).ToArray();
                        var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);

                        Debug.Log($"Sending audio data from position: {cursor}");
                        if (SpeechWebSocketClient.State != WebSocketState.Open) return;
                        cursor += byteLen;
                        var end = cursor >= audioFileInfo.Length;
                        await SpeechWebSocketClient.SendAsync(arrSeg, WebSocketMessageType.Binary, true, new CancellationToken());
                        Debug.Log($"Audio data from file {audioFilePath} sent successfully!");

                        var dt = Encoding.ASCII.GetString(arr);
                    }
                    await SendEmptyAudioMessageToWebSocketClient(SpeechWebSocketClient, requestId);
                    audioFileStream.Dispose();
                });

                // Wait for tasks to complete
                await Task.WhenAll(sending, receiving);
                if (sending.IsFaulted)
                {
                    state = JobState.Error;
                    var err = sending.Exception;
                    throw err;
                }
                if (receiving.IsFaulted)
                {
                    state = JobState.Error;
                    var err = receiving.Exception;
                    throw err;
                }

                return true;
            }
            catch (Exception ex)
            {
                state = JobState.Error;
                Debug.Log($"An exception occurred during creation of Speech Recognition job from audio file {audioFilePath}:" 
                    + Environment.NewLine + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// prepares a new speech recognition job, sending the proper headers including the audio data header
        /// </summary>
        /// <param name="authenticationToken"></param>
        /// <param name="region"></param>
        /// <param name="resolution"></param>
        /// <param name="channels"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public async Task<bool> CreateSpeechRecognitionJobFromVoice(string authenticationToken, string region, int resolution, int channels, int rate)
        {
            try
            {
                state = JobState.PreparingJob;

                SpeechWebSocketClient = await InitializeSpeechWebSocketClient(authenticationToken, region);

                var receiving = Receiving(SpeechWebSocketClient);

                var sending = Task.Run(async () =>
                {
                    // Create a unique request ID, must be a UUID in "no-dash" format
                    CurrentRequestId = Guid.NewGuid().ToString("N");

                    ArraySegment<byte> buffer = CreateSpeechConfigMessagePayloadBuffer(CurrentRequestId);

                    if (SpeechWebSocketClient.State != WebSocketState.Open) return;

                    Debug.Log("Sending speech.config...");
                    // Send speech.config to Speech Service
                    await SpeechWebSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, new CancellationToken());
                    Debug.Log("speech.config sent successfully!");

                    // SENDING AUDIO TO SPEECH SERVICE:
                    // Speech-enabled client applications send audio to Speech Service by converting the audio stream
                    // into a series of audio chunks. Each chunk of audio carries a segment of the spoken audio that's
                    // to be transcribed by the service. The maximum size of a single audio chunk is 8,192 bytes.
                    // Audio stream messages are Binary WebSocket messages.
                    // First we need to send an audio packet with the RIFF PCM (WAV) data header. Note that we don't know how
                    // many samples we'll have since we are recording live, so we set nbsamples to zero.

                    // The WebSocket Speech protocol docs state that PCM audio must be sampled at 16 kHz with 16 bits per sample
                    // and one channel (riff-16khz-16bit-mono-pcm) but 48kHz-16-bit-stereo works too, more testing is required.
                    var wavHeader = BuildRiffWAVHeader(0, resolution, channels, rate);
                    SendAudioPacket(CurrentRequestId, wavHeader);
                    Debug.Log($"First Audio data paket with WAV header sent successfully!");

                    Debug.Log($"WebSocket Client is now ready to receive audio packets from the microphone.");

                    state = JobState.ReadyForAudioPackets;
                });

                // Wait for tasks to complete
                await Task.WhenAll(sending, receiving);
                if (sending.IsFaulted)
                {
                    var err = sending.Exception;
                    throw err;
                }
                if (receiving.IsFaulted)
                {
                    var err = receiving.Exception;
                    throw err;
                }


                return true;
            }
            catch (Exception ex)
            {
                Debug.Log($"An exception occurred during creation of Speech Recognition job from microphone:" 
                    + Environment.NewLine + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Prepares the WebSocket Client with the proper header and Uri for the Speech Service.
        /// </summary>
        /// <param name="authenticationToken"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private async Task<ClientWebSocket> InitializeSpeechWebSocketClient(string authenticationToken, string region)
        {
            // Configuring Speech Service Web Socket client header
            Debug.Log("Connecting to Speech Service via Web Socket.");
            ClientWebSocket websocketClient = new ClientWebSocket();

            string connectionId = Guid.NewGuid().ToString("N");

            // Make sure to change the region & culture to match your recorded audio file.
            string lang = "en-US";
            websocketClient.Options.SetRequestHeader("X-ConnectionId", connectionId);
            websocketClient.Options.SetRequestHeader("Authorization", "Bearer " + authenticationToken);

            // Clients must use an appropriate endpoint of Speech Service. The endpoint is based on recognition mode and language.
            // The supported recognition modes are:
            //  - interactive
            //  - conversation
            //  - dictation
            var url = "";
            if (!useClassicBingSpeechService)
            {
                // New Speech Service endpoint. 
                url = $"wss://{region}.stt.speech.microsoft.com/speech/recognition/interactive/cognitiveservices/v1?format=simple&language={lang}";
            }
            else
            {
                // Bing Speech endpoint
                url = $"wss://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?format=simple&language={lang}";
            }

            await websocketClient.ConnectAsync(new Uri(url), new CancellationToken());
            Debug.Log("Web Socket successfully connected.");

            return websocketClient;
        }

        /// <summary>
        /// Prepares the payload (with headers) for the very first config message to be sent over WebSocket.
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        private static ArraySegment<byte> CreateSpeechConfigMessagePayloadBuffer(string requestId)
        {
            dynamic SpeechConfigPayload = CreateSpeechConfigPayload();

            // Convert speech.config payload to JSON
            var SpeechConfigPayloadJson = JsonConvert.SerializeObject(SpeechConfigPayload, Formatting.None);

            // Create speech.config message from required headers and JSON payload
            StringBuilder speechMsgBuilder = new StringBuilder();
            speechMsgBuilder.Append("path:speech.config" + Environment.NewLine);
            speechMsgBuilder.Append("x-requestid:" + requestId + Environment.NewLine);
            speechMsgBuilder.Append($"x-timestamp:{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")}" + Environment.NewLine);
            speechMsgBuilder.Append($"content-type:application/json; charset=utf-8" + Environment.NewLine);
            speechMsgBuilder.Append(Environment.NewLine);
            speechMsgBuilder.Append(SpeechConfigPayloadJson);
            var strh = speechMsgBuilder.ToString();

            var encoded = Encoding.UTF8.GetBytes(speechMsgBuilder.ToString());
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            return buffer;
        }

        /// <summary>
        /// CONFIGURING SPEECH SERVICE
        /// The payload of the speech.config message is a JSON structure
        /// that contains information about the application.
        /// </summary>
        /// <returns></returns>
        private static dynamic CreateSpeechConfigPayload()
        {
            return new
            {
                context = new
                {
                    system = new
                    {
                        version = "1.0.00000"
                    },
                    os = new
                    {
                        platform = "Speech Service WebSocket Console App",
                        name = "Sample",
                        version = "1.0.00000"
                    },
                    device = new
                    {
                        manufacturer = "Microsoft",
                        model = "SpeechSample",
                        version = "1.0.00000"
                    }
                }
            };
        }

        public void SendAudioPacket(string requestId, byte[] data)
        {
            byte[] headerBytes;
            byte[] headerHead;
            headerBytes = BuildAudioPacketHeader(requestId);
            headerHead = BuildAudioPacketHeaderHead(headerBytes);

            // The WebSocket Speech protocol docs state that PCM audio must be sampled at 16 kHz with 16 bits per sample
            // and one channel (riff-16khz-16bit-mono-pcm) but 48kHz-16-bit-stereo works too, more testing is required.
            var arr = headerHead.Concat(headerBytes).Concat(data).ToArray();
            var arrSeg = new ArraySegment<byte>(arr, 0, arr.Length);

            Debug.Log($"Sending audio data sample from microphone.");
            if (SpeechWebSocketClient.State != WebSocketState.Open) return;
            SpeechWebSocketClient.SendAsync(arrSeg, WebSocketMessageType.Binary, true, new CancellationToken());
            Debug.Log($"Audio data packet from microphone sent successfully!");

            //var dt = Encoding.ASCII.GetString(arr);

        }

        /// <summary>
        /// Send an audio message with a zero-length body. This message tells the service that the client knows
        /// that the user stopped speaking, the utterance is finished, and the microphone is turned off.
        /// </summary>
        /// <param name="websocketClient"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        private async Task SendEmptyAudioMessageToWebSocketClient(ClientWebSocket websocketClient, string requestId)
        {
            byte[] headerBytes;
            byte[] headerHead;
            headerBytes = BuildAudioPacketHeader(requestId);
            headerHead = BuildAudioPacketHeaderHead(headerBytes);
            var arrEnd = headerHead.Concat(headerBytes).ToArray();
            await websocketClient.SendAsync(new ArraySegment<byte>(arrEnd, 0, arrEnd.Length), WebSocketMessageType.Binary, true, new CancellationToken());
        }

        /// <summary>
        /// Creates the header for an audio message  to be sent via WebSockets.
        /// </summary>
        /// <param name="requestid"></param>
        /// <returns></returns>
        private byte[] BuildAudioPacketHeader(string requestid)
        {
            StringBuilder speechMsgBuilder = new StringBuilder();
            // Clients use the audio message to send an audio chunk to the service.
            speechMsgBuilder.Append("path:audio" + Environment.NewLine);
            speechMsgBuilder.Append($"x-requestid:{requestid}" + Environment.NewLine);
            speechMsgBuilder.Append($"x-timestamp:{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")}" + Environment.NewLine);
            speechMsgBuilder.Append($"content-type:audio/x-wav");

            return Encoding.ASCII.GetBytes(speechMsgBuilder.ToString());
        }

        private byte[] BuildAudioPacketHeaderHead(byte[] headerBytes)
        {
            var headerbuffer = new ArraySegment<byte>(headerBytes, 0, headerBytes.Length);
            var str = "0x" + (headerBytes.Length).ToString("X");
            var headerHeadBytes = BitConverter.GetBytes((UInt16)headerBytes.Length);
            var isBigEndian = !BitConverter.IsLittleEndian;
            var headerHead = !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
            return headerHead;
        }

        /// <summary>
        /// Builds properly formatted header data for RIFF PCM (WAV) audio data
        /// </summary>
        /// <param name="nbsamples">The number of audio samples that will be saved. 
        ///             This value can be 0 when building a header for streaming data</param>
        /// <param name="resolution">The resolution of the audio data (e.g. 8, 16, 24 bits)</param>
        /// <param name="channels">The number of audio channels (e.g. 1 for mono, 2 for stereo, etc.)</param>
        /// <param name="rate">The sampling rate of the audio data in Hz (e.g. 22050Hz, 44100Hz (CD quality), etc.)</param>
        /// <returns></returns>
        public byte[] BuildRiffWAVHeader(int nbsamples, int resolution, int channels, int rate)
        {
            List<byte> header = new List<byte>();

            header.AddRange(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            // 36 is the total size of the header that follows. We don't count the RIFF line above (4) and this number (4).
            // The data already takes the number of channels and bytes per sample since we converted from Unity data.
            header.AddRange(BitConverter.GetBytes(36 + nbsamples)); // * numChannels * bytesPerSample
            header.AddRange(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            header.AddRange(System.Text.Encoding.UTF8.GetBytes("fmt "));  // Format chunk marker. Includes trailing null 
            header.AddRange(BitConverter.GetBytes(resolution));    // e.g. 16 bits
            UInt16 audioFormat = 1;     // Type of format (1 is PCM) - 2 byte integer 
            header.AddRange(BitConverter.GetBytes(audioFormat));
            header.AddRange(BitConverter.GetBytes(Convert.ToUInt16(channels)));
            header.AddRange(BitConverter.GetBytes(rate));
            int bytesPerSample = resolution / 8;
            header.AddRange(BitConverter.GetBytes(rate * bytesPerSample * channels));    // byte rate  
            header.AddRange(BitConverter.GetBytes(Convert.ToUInt16(bytesPerSample * channels)));  // block align
            header.AddRange(BitConverter.GetBytes(Convert.ToUInt16(resolution)));            // bit depth
                                                                                             // Start of the data section
            header.AddRange(System.Text.Encoding.UTF8.GetBytes("data"));  // "data" chunk header. Marks the beginning of the data section. 
            header.AddRange(BitConverter.GetBytes(nbsamples));  // Size of the data section. // * bytesPerSample

            return header.ToArray();
        }

        /// <summary>
        /// Allows the WebSocket client to receive messages in a background task.
        /// </summary>
        private async Task Receiving(ClientWebSocket client)
        {
            try
            {
                var buffer = new byte[512];
                bool isReceiving = true;

                while (isReceiving)
                {

                    var wsResult = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    SpeechServiceResult wssr;

                    var resStr = Encoding.UTF8.GetString(buffer, 0, wsResult.Count);

                    switch (wsResult.MessageType)
                    {
                        // Incoming text messages can be hypotheses about the words the service recognized or the final
                        // phrase, which is a recognition result that won't change.
                        case WebSocketMessageType.Text:
                            wssr = ParseWebSocketSpeechResult(resStr);
                            Debug.Log(resStr + Environment.NewLine + "*** Message End ***" + Environment.NewLine);

                            // Set the recognized text field in the client for future lookup, this can be stored
                            // in either the Text property (for hypotheses) or DisplayText (for final phrases).
                            if (wssr.Path == SpeechServiceResult.SpeechMessagePaths.SpeechHypothesis)
                            {
                                RecognizedText = wssr.Result.Text;
                            }
                            else if(wssr.Path == SpeechServiceResult.SpeechMessagePaths.SpeechPhrase)
                            {
                                RecognizedText = wssr.Result.DisplayText;
                                state = JobState.Completed;
                            }
                            // Raise an event with the message we just received.
                            // We also keep the last message received in case the client app didn't subscribe to the event.
                            LastMessageReceived = wssr;
                            if (OnMessageReceived != null)
                            {
                                OnMessageReceived.Invoke(wssr);
                            }
                            break;

                        case WebSocketMessageType.Binary:
                            Debug.Log("Binary messages are not suppported by this application.");
                            break;

                        case WebSocketMessageType.Close:
                            string description = client.CloseStatusDescription;
                            Debug.Log($"Closing WebSocket with Status: {description}");
                            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            isReceiving = false;
                            break;

                        default:
                            Debug.Log("The message type was not recognized.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("An exception occurred while receiving a message:" + Environment.NewLine + ex.Message);
            }
        }
        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        static SpeechServiceResult ParseWebSocketSpeechResult(string result)
        {
            SpeechServiceResult wssr = new SpeechServiceResult();

            using (StringReader sr = new StringReader(result))
            {
                int linecount = 0;
                string line;
                bool isBodyStarted = false;
                string bodyJSON = "";

                // Parse each line in the WebSocket results to extra the headers and JSON body.
                // The header is in the first 3 lines of the response, the rest is the body.
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length > 0)
                    {
                        switch (linecount)
                        {
                            case 0:  // X-RequestID
                                if (line.Substring(0, 11).ToLower() == "x-requestid")
                                {
                                    wssr.RequestId = line.Substring(12);
                                }
                                break;

                            case 1:  // Content-Type & charset on the same line, separated by a semi-colon
                                var sublines = line.Split(new[] { ';' });

                                if (sublines[0].Trim().Substring(0, 12).ToLower() == "content-type")
                                {
                                    wssr.ContentType = sublines[0].Trim().Substring(13);

                                    if (sublines.Length > 1)
                                    {
                                        if (sublines[1].Trim().Substring(0, 7).ToLower() == "charset")
                                        {
                                            wssr.CharSet = sublines[1].Trim().Substring(8);

                                        }
                                    }
                                }
                                break;

                            case 2:  // Path
                                if (line.Substring(0, 4).ToLower() == "path")
                                {
                                    string pathStr = line.Substring(5).Trim().ToLower();
                                    switch (pathStr)
                                    {
                                        case "turn.start":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.TurnStart;
                                            break;
                                        case "speech.startdetected":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.SpeechStartDetected;
                                            break;
                                        case "speech.hypothesis":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.SpeechHypothesis;
                                            break;
                                        case "speech.enddetected":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.SpeechEndDetected;
                                            break;
                                        case "speech.phrase":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.SpeechPhrase;
                                            break;
                                        case "turn.end":
                                            wssr.Path = SpeechServiceResult.SpeechMessagePaths.SpeechEndDetected;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;

                            default:
                                if (!isBodyStarted)
                                {
                                    // For all non-empty lines past the first three (header), once we encounter an opening brace '{'
                                    // we treat the rest of the response as the main results body which is formatted in JSON.
                                    if (line.Substring(0, 1) == "{")
                                    {
                                        isBodyStarted = true;
                                        bodyJSON += line + Environment.NewLine;
                                    }
                                }
                                else
                                {
                                    bodyJSON += line + Environment.NewLine;
                                }
                                break;
                        } 
                    }

                    linecount++;
                }

                // Once the full response has been parsed between header and body components,
                // we need to parse the JSON content of the body itself.
                if (bodyJSON.Length > 0)
                {
                    RecognitionContent srr = JsonConvert.DeserializeObject<RecognitionContent>(bodyJSON);
                    if (srr != null)
                    {
                        wssr.Result = srr;
                    }
                }
            }

            return wssr;
        }
    }
}
