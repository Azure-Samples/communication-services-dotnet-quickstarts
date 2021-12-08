using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IncomingCallRouting
{
    public class Participants
    {
        static Queue<string> availaibleParticipants = null;
        static Dictionary<string, string> engagedParticipants;

        public Participants(string targetparticipants)
        {
            if (availaibleParticipants == null)
            {
                availaibleParticipants = new Queue<string>();
            }
            else
            {
                availaibleParticipants.Clear();
            }

            var participants = targetparticipants.Split(',');
            foreach (var participant in participants)
            {
                availaibleParticipants.Enqueue(participant.Trim());
            }

            engagedParticipants = new Dictionary<string, string>();
        }

        public string getTargetParticipant(string callconnectionid)
        {
            string targetParticipant = null;

            //Get participant from availaible list
            if (availaibleParticipants.Count > 0)
            {
                targetParticipant = availaibleParticipants.Dequeue();

                //register participant in the engaged list
                if(!engagedParticipants.ContainsKey(callconnectionid))
                {
                    engagedParticipants.Add(callconnectionid, string.Empty);
                }
                engagedParticipants[callconnectionid] = targetParticipant;
            }

            return targetParticipant;
        }

        public void UnRegisterParticipant(string callConnectionId)
        {
            if(engagedParticipants.ContainsKey(callConnectionId))
            {
                string targetParticipants = engagedParticipants[callConnectionId];
                availaibleParticipants.Enqueue(targetParticipants);
            }
        }

    }
}
