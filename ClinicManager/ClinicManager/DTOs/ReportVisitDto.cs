namespace ClinicManager.DTOs;

public class ReportVisitDto
{
    public DateTime ScheduledDate { get; set; }
    public string? PatientFirstName { get; set; }
    public string? PatientLastName { get; set; }
    public string? DoctorEmail { get; set; }
    public decimal ProceduresCost { get; set; }
    public decimal MedicationsCost { get; set; }
    public decimal TotalCost => ProceduresCost + MedicationsCost;
}
