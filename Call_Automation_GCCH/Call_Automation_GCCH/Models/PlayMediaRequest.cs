namespace Call_Automation_GCCH.Models
{
    public class PlayMediaRequest
    {
        public required string CallConnectionId { get; set; }
        public MediaSourceType MediaSource { get; set; }
        public required string PlaySource { get; set; }
        public required string PlayTo { get; set; }
        public bool IsPlayAll { get; set; } = false;
    }
}
