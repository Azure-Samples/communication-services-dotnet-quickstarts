namespace CallAutomation.Scenarios
{
    public static class Constants
    {
        public static class OperationContext
        {
            public const string MainMenu = "MainMenu";
            public const string EndCall = "EndCall";
            public const string AgentEndCall = "AgentEndCall";
            public const string AgentLeaveCall = "AgentLeaveCall";
            public const string AccountIdValidation = "AccountIdValidation";
            public const string CallerIdValidation = "CallerIdValidation";
            public const string WaitingForAgent = "WaitingForAgent";
            public const string AgentJoining = "AgentJoining";
            public const string SupervisorJoining = "SupervisorJoining";
            public const string PlayText = "PlayText";
            public const string Escalation = "Escalation";
            public const string Transfer = "Transfer";
            public const string AiPairing = "AiPairing";
            public const string ScheduledCallbackOffer = "ScheduledCallbackOffer";
            public const string ScheduledCallbackTimeSelectionMenu = "ScheduledCallbackTimeSelectionMenu";
            public const string ScheduledCallbackAccepted = "ScheduledCallbackAccepted";
            public const string ScheduledCallbackRejected = "ScheduledCallbackRejected";
            public const string ScheduledCallbackDialout = "ScheduledCallbackDialout";
            public const string ScheduledCallbackDialoutAccepted = "ScheduledCallbackDialoutAccepted";
            public const string ScheduledCallbackDialoutRejected = "ScheduledCallbackDialoutRejected";
            public const string HoldCall = "HoldCall";
            public const string ResumeCall = "ResumeCall";
        }

        public static class IvrTextKeys
        {
            public const string EndCall = "EndCall";
            public const string NoResponse = "NoResponse";
            public const string Greeting = "Greeting";
            public const string ThankYou = "ThankYou";
            public const string InvalidOption = "InvalidOption";
            public const string ScheduledCallbackOffered = "ScheduledCallbackOffered";
            public const string ScheduledCallbackTimeSelection = "ScheduledCallbackTimeSelection";
            public const string ScheduledCallbackAccepted = "ScheduledCallbackAccepted";
            public const string ScheduledCallbackRejected = "ScheduledCallbackRejected";
            public const string ScheduledCallbackDialout = "ScheduledCallbackDialout";
            public const string ScheduledCallbackDialoutAccepted = "ScheduledCallbackDialoutAccepted";
            public const string ScheduledCallbackDialoutRejected = "ScheduledCallbackDialoutRejected";
        }

        public static class IvrCallbackChoices
        {
            public const string Yes = "Yes";
            public const string No = "No";
            public const string One = "One";
            public const string Two = "Two";
            public const string Three = "Three";
        }

        public static class Departments
        {
            public const string Home = "home";
            public const string Mobile = "mobile";
            public const string Television = "television";
            public const string Other = "other";
        }


    }
    public static class QueueConstants
    {
        public const string MagentaDefault = "Magenta";
        public const string MagentaHome = "MagentaHome";
        public const string MagentaMobile = "MagentaMobile";
        public const string MagentaTV = "MagentaTV";
        public const string TimeZone = "TimeZone";
    }

    public static class AuthenticationConstants
    {
        public const string EventGridAuthenticationHeaderName = "X-EventGrid-AuthKey";
        public const string EventGridAuthenticationQueryParameterName = "EventGridAuthKey";
        public const string EventGridSecretName = "EventGrid";

        public const string GraphQueryParamName = "validationToken";
        public const string GraphSecretName = "Validation";
        public const string GraphAuthenticationHeaderName = "host";
        public const string GraphClaimName = "appid";

        public const string SystemAdminRoleRequired = "SystemAdminRoleRequired";
        public const string TeamMemberRoleRequired = "TeamMemberRoleRequired";
        public const string TeamLeaderRoleRequired = "TeamLeaderRoleRequired";
        public const string TeamLeaderOrSystemAdminRoleRequired = "TeamLeaderOrSystemAdminRoleRequired";
    }

}
