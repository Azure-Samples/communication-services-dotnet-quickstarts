﻿using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomation_AppointmentBooking.Interfaces
{
    public interface ITopLevelMenuService
    {
        Task InvokeTopLevelMenu(CommunicationIdentifier originalTarget, CallConnection callConnection, string serverCallId);
    }
}
