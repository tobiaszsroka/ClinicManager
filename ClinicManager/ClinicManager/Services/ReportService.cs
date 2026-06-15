using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ReportVisitDto>> GetCompletedVisitsAsync(DateTime startDate, DateTime endDate)
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.Procedures)
            .Include(v => v.PrescribedMedications)
            .Where(v => v.Status == VisitStatus.Completed &&
                        v.ScheduledDate >= startDate &&
                        v.ScheduledDate <= endDate)
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync();

        return visits.Select(visit =>
        {
            var dto = ReportMapper.ToDto(visit);
            dto.ProceduresCost = visit.Procedures.Sum(p => p.FinalCost);
            dto.MedicationsCost = visit.PrescribedMedications.Sum(p => p.TotalCost);
            return dto;
        }).ToList();
    }

    public async Task<IReadOnlyCollection<ReportVisitDto>> GetUpcomingVisitsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.Status == VisitStatus.Scheduled &&
                        v.ScheduledDate >= startDate &&
                        v.ScheduledDate <= endDate)
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync(cancellationToken);

        return visits.Select(ReportMapper.ToDto).ToList();
    }
}
