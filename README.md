# Unity-MS-SpeechSDK
Sample Unity project used to demonstrate Speech Recognition (aka Speech-to-Text) using the new [Microsoft Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/) (currently in Preview) via WebSockets. The Microsoft Speech Service is part of [Microsoft Azure Cognitive Services](https://www.microsoft.com/cognitive-services). **This is a work in progress**. 

* **Unity version:** 2018.2.1f1
* **Speech Service version:** 0.5.0 (Preview)
* **Target platforms tested:** Unity Editor/Mono (*to be tested*: Windows Desktop, UWP/WMR, Android, iOS)

## Implementation Notes
* This sample uses the [Speech Service WebSocket protocol](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/websocketprotocol) to interact with the Speech Service and generate speech recognition hypotheses in real-time.
* This sample is compatible with both the new Cognitive Services Speech Service (Preview) and the classic Bing Speech API. The default and recommended approach is the new service.
* You will need an Azure Cognitive Services account to use this sample: [Create an account here](https://docs.microsoft.com/azure/cognitive-services/cognitive-services-apis-create-account).
* If you see any API keys in the code, these are either trial keys that will expire soon or temporary keys that may get invalidated. Please get your own keys. [Get your own trial key to Bing Speech or the new Speech Service here](https://azure.microsoft.com/try/cognitive-services). A free tier is available allowing 5,000 transactions per month, at a rate of 20 per minute.
* This initial draft implementation uploads a speech audio file to perform the recognition. *Microphone integration coming soon*.
* A UI Canvas button is used to trigger the speech recognition job. The results are posted in the Unity Debug Console window.

## Resource Links
* [Microsoft Cognitive Services](https://www.microsoft.com/cognitive-services) (formerly Project Oxford)
* [New Cognitive Services Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/) (currently in Preview)
* [Classic Bing Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/home) (legacy)
* [Unity Speech Synthesis Sample](https://github.com/ActiveNick/Unity-Text-to-Speech) (aka Text-to-Speech)

## Follow Me
* Twitter: [@ActiveNick](http://twitter.com/ActiveNick)
* Blog: [AgeofMobility.com](http://AgeofMobility.com)
* SlideShare: [http://www.slideshare.net/ActiveNick](http://www.slideshare.net/ActiveNick)
