namespace IncomingCallRouting.Enums
{
    /// <summary>
    /// The states of a call connection.
    /// </summary>
    public enum CallConnectionState
    {
        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The call connection is in progress after initiating or accepting the call.
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// The call is connected.
        /// </summary>
        Connected = 2,

        /// <summary>
        /// The call has initiated a transfer.
        /// </summary>
        Transferring = 3,

        /// <summary>
        /// The transfer is accepted.
        /// </summary>
        TransferAccepted = 4,

        /// <summary>
        /// The call is disconnecting.
        /// </summary>
        Disconnecting = 5,

        /// <summary>
        /// The call has disconnected.
        /// </summary>
        Disconnected = 6,
    }
}
