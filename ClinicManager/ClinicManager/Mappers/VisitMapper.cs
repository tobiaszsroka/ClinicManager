using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitMapper
{
    [MapProperty("Patient.FirstName", nameof(VisitDto.PatientFirstName))]
    [MapProperty("Patient.LastName", nameof(VisitDto.PatientLastName))]
    [MapProperty("Doctor.Email", nameof(VisitDto.DoctorEmail))]
    public static partial VisitDto ToDto(Visit visit);

    [MapperIgnoreTarget(nameof(Visit.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Visit.Patient))]
    [MapperIgnoreTarget(nameof(Visit.Doctor))]
    [MapperIgnoreTarget(nameof(Visit.Procedures))]
    [MapperIgnoreTarget(nameof(Visit.ClinicalNotes))]
    [MapperIgnoreTarget(nameof(Visit.PrescribedMedications))]
    public static partial Visit ToEntity(VisitDto visit);

    [MapperIgnoreTarget(nameof(Visit.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Visit.Patient))]
    [MapperIgnoreTarget(nameof(Visit.Doctor))]
    [MapperIgnoreTarget(nameof(Visit.Procedures))]
    [MapperIgnoreTarget(nameof(Visit.ClinicalNotes))]
    [MapperIgnoreTarget(nameof(Visit.PrescribedMedications))]
    public static partial void UpdateEntity(VisitDto visit, Visit target);

    public static partial MedicalProcedureDto ToDto(MedicalProcedure procedure);

    [MapperIgnoreTarget(nameof(MedicalProcedure.Visit))]
    public static partial MedicalProcedure ToEntity(MedicalProcedureDto procedure);

    [MapProperty("Medication.Name", nameof(PrescribedMedicationDto.MedicationName))]
    public static partial PrescribedMedicationDto ToDto(PrescribedMedication prescription);

    [MapperIgnoreTarget(nameof(PrescribedMedication.Medication))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Visit))]
    public static partial PrescribedMedication ToEntity(PrescribedMedicationDto prescription);

    [MapperIgnoreTarget(nameof(ClinicalNote.Visit))]
    public static partial ClinicalNote ToEntity(ClinicalNoteDto note);

    public static partial ClinicalNoteDto ToDto(ClinicalNote note);
}
