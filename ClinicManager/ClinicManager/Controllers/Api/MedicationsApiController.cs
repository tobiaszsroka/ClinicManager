using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/medications")]
[Authorize(Roles = "Admin,Rejestratorka")]
[Produces("application/json")]
public class MedicationsApiController : ControllerBase
{
    private readonly MedicationService _medicationService;

    public MedicationsApiController(MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    /// <summary>Pobiera katalog leków.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<MedicationDto>>> GetAll()
    {
        return Ok(await _medicationService.GetAllAsync());
    }

    /// <summary>Pobiera lek z katalogu.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationDto>> GetById(int id)
    {
        var medication = await _medicationService.GetByIdAsync(id);
        return medication == null ? NotFound() : Ok(medication);
    }

    /// <summary>Dodaje lek do katalogu.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MedicationDto>> Create([FromBody] MedicationDto medication)
    {
        medication.Name = medication.Name.Trim();
        if (await _medicationService.NameExistsAsync(medication.Name))
        {
            ModelState.AddModelError(nameof(medication.Name), "Lek o tej nazwie już istnieje.");
            return ValidationProblem(ModelState);
        }

        medication.Id = await _medicationService.CreateAsync(medication);
        return CreatedAtAction(nameof(GetById), new { id = medication.Id }, medication);
    }

    /// <summary>Aktualizuje lek w katalogu.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] MedicationDto medication)
    {
        if (id != medication.Id) return BadRequest();

        medication.Name = medication.Name.Trim();
        if (await _medicationService.NameExistsAsync(medication.Name, medication.Id))
        {
            ModelState.AddModelError(nameof(medication.Name), "Lek o tej nazwie już istnieje.");
            return ValidationProblem(ModelState);
        }

        return await _medicationService.UpdateAsync(medication) ? NoContent() : NotFound();
    }

    /// <summary>Usuwa nieużywany lek z katalogu.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _medicationService.GetByIdAsync(id) == null) return NotFound();
        if (await _medicationService.IsUsedAsync(id))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Nie można usunąć leku.",
                Detail = "Lek występuje na zapisanej recepcie.",
                Status = StatusCodes.Status409Conflict
            });
        }

        await _medicationService.DeleteAsync(id);
        return NoContent();
    }
}
