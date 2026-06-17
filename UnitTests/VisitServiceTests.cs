using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class VisitServiceTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly VisitService _visitService;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

        public VisitServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<IdentityUser>>();
            _userManagerMock = new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _visitService = new VisitService(_dbContext, _userManagerMock.Object);
        }

        [Fact]
        public async Task HasTimeConflictAsync_ShouldReturnTrue_WhenConflictExists()
        {
            // Arrange
            var date = new DateTime(2026, 06, 20, 10, 0, 0);
            var visit = new Visit
            {
                AssignedDoctorId = "doctor1",
                PatientId = 1,
                ScheduledDate = date,
                Status = VisitStatus.Scheduled
            };
            _dbContext.Visits.Add(visit);
            await _dbContext.SaveChangesAsync();

            // Act
            // Conflict exists if within 29 minutes for same doctor or patient
            var conflictExactTime = await _visitService.HasTimeConflictAsync("doctor1", 2, date);
            var conflict15MinsLate = await _visitService.HasTimeConflictAsync("doctor1", 2, date.AddMinutes(15));
            var conflictDifferentDoctorSamePatient = await _visitService.HasTimeConflictAsync("doctor2", 1, date);

            // Assert
            Assert.True(conflictExactTime);
            Assert.True(conflict15MinsLate);
            Assert.True(conflictDifferentDoctorSamePatient);
        }

        [Fact]
        public async Task HasTimeConflictAsync_ShouldReturnFalse_WhenNoConflict()
        {
            // Arrange
            var date = new DateTime(2026, 06, 20, 10, 0, 0);
            var visit = new Visit
            {
                AssignedDoctorId = "doctor1",
                PatientId = 1,
                ScheduledDate = date,
                Status = VisitStatus.Scheduled
            };
            _dbContext.Visits.Add(visit);
            await _dbContext.SaveChangesAsync();

            // Act
            // 30 minutes apart should be fine
            var noConflict = await _visitService.HasTimeConflictAsync("doctor1", 2, date.AddMinutes(30));
            var differentDoctorAndPatient = await _visitService.HasTimeConflictAsync("doctor2", 2, date);

            // Assert
            Assert.False(noConflict);
            Assert.False(differentDoctorAndPatient);
        }

        [Fact]
        public async Task AddPrescriptionAsync_ShouldSaveUnitPrice_WhenMedicationExists()
        {
            // Arrange
            var medication = new Medication { Name = "Apap", UnitPrice = 12.50m };
            _dbContext.Medications.Add(medication);
            await _dbContext.SaveChangesAsync();

            var prescription = new PrescribedMedicationDto
            {
                VisitId = 1,
                MedicationId = medication.Id,
                Quantity = 2,
                Dosage = "1x dziennie"
            };

            // Act
            var result = await _visitService.AddPrescriptionAsync(prescription);

            // Assert
            Assert.True(result);
            var savedPrescription = await _dbContext.PrescribedMedications.FirstOrDefaultAsync();
            Assert.NotNull(savedPrescription);
            Assert.Equal(12.50m, savedPrescription.UnitPriceAtPrescription); // Unit price copied from dict
            Assert.Equal(2, savedPrescription.Quantity);
        }

        [Fact]
        public async Task AddPrescriptionAsync_ShouldReturnFalse_WhenMedicationNotFound()
        {
            // Arrange
            var prescription = new PrescribedMedicationDto
            {
                VisitId = 1,
                MedicationId = 999, // Doesn't exist
                Quantity = 1,
                Dosage = "1x dziennie"
            };

            // Act
            var result = await _visitService.AddPrescriptionAsync(prescription);

            // Assert
            Assert.False(result);
            var count = await _dbContext.PrescribedMedications.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task AddProcedureAsync_ShouldSaveProcedureInDatabase()
        {
            // Arrange
            var procedureDto = new MedicalProcedureDto
            {
                VisitId = 10,
                Name = "Konsultacja",
                Description = "Zwykła",
                BaseCost = 200.0m,
                Discount = 50.0m
            };

            // Act
            await _visitService.AddProcedureAsync(procedureDto);

            // Assert
            var saved = await _dbContext.MedicalProcedures.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal(10, saved.VisitId);
            Assert.Equal("Konsultacja", saved.Name);
            Assert.Equal(200.0m, saved.BaseCost);
            Assert.Equal(50.0m, saved.Discount);
            Assert.Equal(150.0m, saved.FinalCost);
        }

        [Fact]
        public async Task AddNoteAsync_ShouldAssignAuthorAndTimestamp()
        {
            // Arrange
            var noteDto = new ClinicalNoteDto
            {
                VisitId = 5,
                Content = "Pacjent zdrowy"
            };

            // Act
            await _visitService.AddNoteAsync(noteDto, "author123");

            // Assert
            var saved = await _dbContext.ClinicalNotes.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("author123", saved.Author);
            Assert.Equal("Pacjent zdrowy", saved.Content);
            Assert.True(saved.Timestamp <= DateTime.Now && saved.Timestamp > DateTime.Now.AddSeconds(-2));
        }
    }
}
