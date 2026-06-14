using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class VisitService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public VisitService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<VisitDto>> GetAllAsync(string? doctorId)
    {
        var query = _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .AsQueryable();

        if (doctorId != null)
        {
            query = query.Where(v => v.AssignedDoctorId == doctorId);
        }

        var visits = await query.OrderBy(v => v.ScheduledDate).ToListAsync();
        return visits.Select(VisitMapper.ToDto).ToList();
    }

    public async Task<VisitDto?> GetDetailsAsync(int id)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.Procedures)
            .Include(v => v.PrescribedMedications)
                .ThenInclude(p => p.Medication)
            .Include(v => v.ClinicalNotes)
            .FirstOrDefaultAsync(v => v.Id == id);

        return visit == null ? null : VisitMapper.ToDto(visit);
    }

    public async Task<VisitDto?> GetByIdAsync(int id)
    {
        var visit = await _context.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        return visit == null ? null : VisitMapper.ToDto(visit);
    }

    public async Task<VisitDto?> GetForDeleteAsync(int id)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstOrDefaultAsync(v => v.Id == id);

        return visit == null ? null : VisitMapper.ToDto(visit);
    }

    public async Task<VisitDto?> GetByProcedureIdAsync(int procedureId)
    {
        var visit = await _context.MedicalProcedures
            .AsNoTracking()
            .Where(p => p.Id == procedureId)
            .Select(p => p.Visit)
            .FirstOrDefaultAsync();

        return visit == null ? null : VisitMapper.ToDto(visit);
    }

    public async Task<VisitDto?> GetByPrescriptionIdAsync(int prescriptionId)
    {
        var visit = await _context.PrescribedMedications
            .AsNoTracking()
            .Where(p => p.Id == prescriptionId)
            .Select(p => p.Visit)
            .FirstOrDefaultAsync();

        return visit == null ? null : VisitMapper.ToDto(visit);
    }

    public async Task<VisitFormOptionsDto> GetFormOptionsAsync()
    {
        var patients = await _context.Patients
            .AsNoTracking()
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new SelectOptionDto(
                p.Id.ToString(),
                p.FirstName + " " + p.LastName + " (" + p.Pesel + ")"))
            .ToListAsync();

        var doctors = await _userManager.GetUsersInRoleAsync("Lekarz");
        var medications = await _context.Medications
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new SelectOptionDto(
                m.Id.ToString(),
                m.Name + " (" + m.UnitPrice.ToString("0.00") + " zł)"))
            .ToListAsync();

        return new VisitFormOptionsDto
        {
            Patients = patients,
            Doctors = doctors
                .OrderBy(d => d.Email)
                .Select(d => new SelectOptionDto(d.Id, d.Email ?? d.UserName ?? d.Id))
                .ToList(),
            Medications = medications
        };
    }

    public async Task<int> CreateAsync(VisitDto visit)
    {
        var entity = VisitMapper.ToEntity(visit);
        _context.Visits.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(VisitDto visit)
    {
        var entity = await _context.Visits.FindAsync(visit.Id);
        if (entity == null) return false;

        VisitMapper.UpdateEntity(visit, entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, VisitStatus status)
    {
        var entity = await _context.Visits.FindAsync(id);
        if (entity == null) return false;

        entity.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Visits.FindAsync(id);
        if (entity == null) return;

        _context.Visits.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasTimeConflictAsync(
        string doctorId,
        int patientId,
        DateTime date,
        int? excludedVisitId = null)
    {
        var startTime = date.AddMinutes(-29);
        var endTime = date.AddMinutes(29);

        return _context.Visits.AnyAsync(v =>
            v.Status != VisitStatus.Cancelled &&
            v.Status != VisitStatus.Completed &&
            (!excludedVisitId.HasValue || v.Id != excludedVisitId.Value) &&
            (v.AssignedDoctorId == doctorId || v.PatientId == patientId) &&
            v.ScheduledDate >= startTime &&
            v.ScheduledDate <= endTime);
    }

    public async Task AddProcedureAsync(MedicalProcedureDto procedure)
    {
        _context.MedicalProcedures.Add(VisitMapper.ToEntity(procedure));
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProcedureAsync(int procedureId)
    {
        var procedure = await _context.MedicalProcedures.FindAsync(procedureId);
        if (procedure == null) return;

        _context.MedicalProcedures.Remove(procedure);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> AddPrescriptionAsync(PrescribedMedicationDto prescription)
    {
        var medication = await _context.Medications.FindAsync(prescription.MedicationId);
        if (medication == null) return false;

        prescription.UnitPriceAtPrescription = medication.UnitPrice;
        _context.PrescribedMedications.Add(VisitMapper.ToEntity(prescription));
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeletePrescriptionAsync(int prescriptionId)
    {
        var prescription = await _context.PrescribedMedications.FindAsync(prescriptionId);
        if (prescription == null) return;

        _context.PrescribedMedications.Remove(prescription);
        await _context.SaveChangesAsync();
    }

    public async Task AddNoteAsync(ClinicalNoteDto note, string authorId)
    {
        note.Author = authorId;
        note.Timestamp = DateTime.Now;
        _context.ClinicalNotes.Add(VisitMapper.ToEntity(note));
        await _context.SaveChangesAsync();
    }
}
