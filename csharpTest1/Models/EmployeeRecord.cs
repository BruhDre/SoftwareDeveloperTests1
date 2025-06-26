namespace CSharpTest1.Models
{
    public class EmployeeRecord
    {
        public string Id { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public DateTime StarTimeUtc { get; set; } = DateTime.Now;
        public DateTime EndTimeUtc { get; set; } = DateTime.Now;
        public string EntryNotes { get; set; } = "";
        public string DeletedOn { get; set; } = "";
    }
}