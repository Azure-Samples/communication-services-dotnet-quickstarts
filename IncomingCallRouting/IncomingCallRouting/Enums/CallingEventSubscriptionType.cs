namespace IncomingCallRouting.Enums
{
    /// <summary>
    /// Type of events which can be subscribed to.
    /// </summary>
    public enum CallingEventSubscriptionType
    {
        /// <summary>
        /// The participants updated event.
        /// </summary>
        ParticipantsUpdated,

        /// <summary>
        /// The DTMF tone event.
        /// </summary>
        ToneReceived
    }
}
