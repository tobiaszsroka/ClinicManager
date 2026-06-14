using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ReportMapper
{
    [MapProperty("Patient.FirstName", nameof(ReportVisitDto.PatientFirstName))]
    [MapProperty("Patient.LastName", nameof(ReportVisitDto.PatientLastName))]
    [MapProperty("Doctor.Email", nameof(ReportVisitDto.DoctorEmail))]
    [MapperIgnoreTarget(nameof(ReportVisitDto.ProceduresCost))]
    [MapperIgnoreTarget(nameof(ReportVisitDto.MedicationsCost))]
    public static partial ReportVisitDto ToDto(Visit visit);
}
