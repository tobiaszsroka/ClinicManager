using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class PatientService
{
    private readonly ApplicationDbContext _context;

    public PatientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PatientDto>> GetAllAsync(string? searchString)
    {
        var query = _context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(p => p.LastName.Contains(searchString) || p.Pesel.Contains(searchString));
        }

        var patients = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
        return patients.Select(PatientMapper.ToDto).ToList();
    }

    public async Task<PatientDetailsDto?> GetDetailsAsync(int id)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.Visits)
                .ThenInclude(v => v.Procedures)
            .Include(p => p.Visits)
                .ThenInclude(v => v.PrescribedMedications)
                    .ThenInclude(p => p.Medication)
            .Include(p => p.Visits)
                .ThenInclude(v => v.ClinicalNotes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null) return null;

        return new PatientDetailsDto
        {
            Patient = PatientMapper.ToDto(patient),
            Visits = patient.Visits.Select(VisitMapper.ToDto).ToList()
        };
    }

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return patient == null ? null : PatientMapper.ToDto(patient);
    }

    public Task<bool> PeselExistsAsync(string pesel, int? excludedId = null)
    {
        return _context.Patients.AnyAsync(p =>
            p.Pesel == pesel && (!excludedId.HasValue || p.Id != excludedId.Value));
    }

    public async Task<int> CreateAsync(PatientDto patient)
    {
        var entity = PatientMapper.ToEntity(patient);
        _context.Patients.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(PatientDto patient)
    {
        var entity = await _context.Patients.FindAsync(patient.Id);
        if (entity == null) return false;

        PatientMapper.UpdateEntity(patient, entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) return;

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();
    }
}
