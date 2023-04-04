using Azure.Communication.Identity;
using Azure.Communication;

namespace CallAutomation_AppointmentReminder
{
    /// <summary>
    /// Configuration assoociated with the call.
    /// </summary>
    public class CallConfiguration
    {
        public CallConfiguration()
        {

        }

        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The phone number to add to the call
        /// </summary>
        public string TargetPhoneNumber { get; set; }

        /// <summary>
        /// The phone number associated with the source. 
        /// </summary>
        public string SourcePhoneNumber { get; set; }

        /// <summary>
        /// The phone number to add Participant to the call. 
        /// </summary>
        public string AddParticipantNumber { get; set; }

        /// <summary>
        /// The base url of the applicaiton.
        /// </summary>
        public string AppBaseUri { get; set; }

        /// <summary>
        /// The base url of the applicaiton.
        /// </summary>
        public string EventCallBackRoute { get; set; }

        /// <summary>
        /// The callback url of the application where notification would be received.
        /// </summary>
        public string CallbackEventUri => $"{AppBaseUri}" + EventCallBackRoute;

        /// <summary>
        /// Appointment reminder menu audio file route
        /// </summary>
        public string AppointmentReminderMenuAudio { get; set; }

        /// <summary>
        /// Appointment confirmed audio file route
        /// </summary>
        public string AppointmentConfirmedAudio { get; set; }

        /// <summary>
        /// Appointment cancelled audio file route
        /// </summary>
        public string AppointmentCancelledAudio { get; set; }

        /// <summary>
        /// Appointment to add AgentAudio audio file route
        /// </summary>
        public string AgentAudio { get; set; }

        /// <summary>
        /// Appointment to AddParticipant audio file route
        /// </summary>
        public string AddParticipant { get; set; }

        /// <summary>
        /// Appointment to remove RemoveParticipant audio file route
        /// </summary>
        public string RemoveParticipant { get; set; }

        /// <summary>
        /// Invalid input audio file route
        /// </summary>
        public string InvalidInputAudio { get; set; }

        /// <summary>
        /// Time out audio file route
        /// </summary>
        public string TimedoutAudio { get; set; }
    }
}
