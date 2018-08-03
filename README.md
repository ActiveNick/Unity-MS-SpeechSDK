# Unity-MS-SpeechSDK
Sample Unity project used to demonstrate Speech Recognition using the new [Microsoft Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/) (currently in Preview) via WebSockets. This is a work in progress. The Microsoft Speech Service is part of [Microsoft Azure Cognitive Services](https://www.microsoft.com/cognitive-services). 

**Unity version: 2018.2.1f1**
**Speech Service version: 0.5.0** (Preview)

## Implementation Notes
* This sample uses the WebSockets API to interact with the Speech Service and generate speech recognition hypotheses in real-time.
* This sample is compatible with both the new Cognitive Services Speech Service (Preview) and the classic Bing Speech API. The default and recommended approach is the new service.
* You will need an Azure Cognitive Services account to use this sample. [Create an account here](https://docs.microsoft.com/azure/cognitive-services/cognitive-services-apis-create-account).
* If you see any API keys in the code, these are either trial keys that will expire soon or temporary keys that may get invalidated. Please get your own key. Get your own trial key to Bing Speech or the new Speech Service at https://azure.microsoft.com/try/cognitive-services. A free tier is available allowing 5,000 transactions per month, at a rate of 20 per minute.
* This initial implementation uploads a speech audio file to perform the recognition. *Microphone integration coming soon*.

## Resource Links
* [Microsoft Cognitive Services](https://www.microsoft.com/cognitive-services) (formerly Project Oxford)
* [New Cognitive Services Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech-Service/) (currently in Preview)
* [Classic Bing Speech Service](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/home) (legacy)
