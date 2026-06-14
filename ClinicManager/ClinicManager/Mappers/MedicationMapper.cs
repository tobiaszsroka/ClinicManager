using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class MedicationMapper
{
    public static partial MedicationDto ToDto(Medication medication);

    [MapperIgnoreTarget(nameof(Medication.Prescriptions))]
    public static partial Medication ToEntity(MedicationDto medication);

    [MapperIgnoreTarget(nameof(Medication.Prescriptions))]
    public static partial void UpdateEntity(MedicationDto medication, Medication target);
}
