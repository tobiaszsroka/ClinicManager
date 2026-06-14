using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class MedicalRecordMapper
{
    public static partial MedicalRecordDto ToDto(MedicalRecord record);
    public static partial MedicalDocumentDto ToDto(MedicalDocument document);

    [MapperIgnoreTarget(nameof(MedicalRecord.Patient))]
    [MapperIgnoreTarget(nameof(MedicalRecord.Documents))]
    public static partial MedicalRecord ToEntity(MedicalRecordDto record);

    [MapperIgnoreTarget(nameof(MedicalRecord.Patient))]
    [MapperIgnoreTarget(nameof(MedicalRecord.Documents))]
    public static partial void UpdateEntity(MedicalRecordDto record, MedicalRecord target);
}
