using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PatientMapper
{
    public static partial PatientDto ToDto(Patient patient);

    [MapperIgnoreTarget(nameof(Patient.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Patient.Visits))]
    public static partial Patient ToEntity(PatientDto patient);

    [MapperIgnoreTarget(nameof(Patient.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Patient.Visits))]
    public static partial void UpdateEntity(PatientDto patient, Patient target);
}
