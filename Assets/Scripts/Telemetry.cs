using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionService
{
    /// <summary>
    /// Used to send Telemetry after a speech recognition job has completed.
    /// Currently not in use (yet) in the sample. Work in progress.
    /// </summary>
    public class Telemetry
    {
        public Receivedmessage[] ReceivedMessages { get; set; }
        public Metric[] Metrics { get; set; }

        public Telemetry()
        {
            
        }
    }

    public class Receivedmessage
    {
        public DateTime[] speechhypothesis { get; set; }
        public DateTime speechendDetected { get; set; }
        public DateTime speechphrase { get; set; }
        public DateTime turnend { get; set; }
    }

    public class Metric
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
