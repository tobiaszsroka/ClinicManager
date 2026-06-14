using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class MedicationService
{
    private readonly ApplicationDbContext _context;

    public MedicationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<MedicationDto>> GetAllAsync()
    {
        var medications = await _context.Medications
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

        return medications.Select(MedicationMapper.ToDto).ToList();
    }

    public async Task<MedicationDto?> GetByIdAsync(int id)
    {
        var medication = await _context.Medications.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        return medication == null ? null : MedicationMapper.ToDto(medication);
    }

    public Task<bool> NameExistsAsync(string name, int? excludedId = null)
    {
        return _context.Medications.AnyAsync(m =>
            m.Name == name && (!excludedId.HasValue || m.Id != excludedId.Value));
    }

    public async Task CreateAsync(MedicationDto medication)
    {
        _context.Medications.Add(MedicationMapper.ToEntity(medication));
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(MedicationDto medication)
    {
        var entity = await _context.Medications.FindAsync(medication.Id);
        if (entity == null) return false;

        MedicationMapper.UpdateEntity(medication, entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> IsUsedAsync(int id)
    {
        return _context.PrescribedMedications.AnyAsync(p => p.MedicationId == id);
    }

    public async Task DeleteAsync(int id)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null) return;

        _context.Medications.Remove(medication);
        await _context.SaveChangesAsync();
    }
}
