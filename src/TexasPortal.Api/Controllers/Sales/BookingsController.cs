using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Common;
using TexasPortal.Contracts.Sales.Bookings;

using TexasPortal.Application.Sales.Bookings.OtherServices.CreateOtherServiceBooking;
using TexasPortal.Application.Sales.Bookings.Hotels.CreateHotelBooking;
using TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;
using TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;
using TexasPortal.Application.Sales.Bookings.Vfs.CreateVfsAppointment;
using TexasPortal.Application.Sales.Bookings.Holidays.CreateHolidayPackage;
using TexasPortal.Application.Sales.Bookings.Transports.CreateTransportation;

namespace TexasPortal.Api.Controllers.Sales;

[ApiController]
[Authorize(Roles = "Salesman")]
[Route("api/sales/bookings")]
[Produces("application/json")]
public sealed class BookingsController : ControllerBase
{
    private readonly ISalesReadService _salesReadService;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _env;
    private readonly CreateOtherServiceBookingHandler _createOtherServiceBookingHandler;
    private readonly CreateHotelBookingHandler _createHotelBookingHandler;
    private readonly CreateFlightBookingHandler _createFlightBookingHandler;
    private readonly CreateVisaBookingHandler _createVisaBookingHandler;
    private readonly CreateVfsAppointmentHandler _createVfsAppointmentHandler;
    private readonly CreateHolidayPackageHandler _createHolidayPackageHandler;
    private readonly CreateTransportationHandler _createTransportationHandler;
    private readonly IAgentsApiClient _agentsApiClient;

    public BookingsController(
        ISalesReadService salesReadService, 
        ICurrentUser currentUser,
        IWebHostEnvironment env,
        CreateOtherServiceBookingHandler createOtherServiceBookingHandler,
        CreateHotelBookingHandler createHotelBookingHandler,
        CreateFlightBookingHandler createFlightBookingHandler,
        CreateVisaBookingHandler createVisaBookingHandler,
        CreateVfsAppointmentHandler createVfsAppointmentHandler,
        CreateHolidayPackageHandler createHolidayPackageHandler,
        CreateTransportationHandler createTransportationHandler,
        IAgentsApiClient agentsApiClient)
    {
        _salesReadService = salesReadService;
        _currentUser = currentUser;
        _env = env;
        _createOtherServiceBookingHandler = createOtherServiceBookingHandler;
        _createHotelBookingHandler = createHotelBookingHandler;
        _createFlightBookingHandler = createFlightBookingHandler;
        _createVisaBookingHandler = createVisaBookingHandler;
        _createVfsAppointmentHandler = createVfsAppointmentHandler;
        _createHolidayPackageHandler = createHolidayPackageHandler;
        _createTransportationHandler = createTransportationHandler;
        _agentsApiClient = agentsApiClient;
    }

    private bool TryGetUserId(out long userId)
    {
        userId = 0;
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null) return false;
        userId = _currentUser.UserId.Value;
        return true;
    }

    private static IActionResult StatusFilterError() =>
        new BadRequestObjectResult("paymentStatus must be: pending, paid, or partial");

    // ─── Flights ──────────────────────────────────────────────────────────────
    /// <summary>Lists flight bookings for the authenticated salesman.</summary>
    [HttpGet("flights")]
    [ProducesResponseType(typeof(PagedResponse<FlightBookingSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetFlights(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
        var result = await _salesReadService.GetFlightBookingsAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    /// <summary>Creates a new flight booking with passenger records, idempotency, and financial checks.</summary>
    [HttpPost("flights")]
    [ProducesResponseType(typeof(FlightBookingResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateFlight(
        [FromBody] CreateFlightBookingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Idempotency-Key header is required."
            });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        try
        {
            var command = new CreateFlightBookingCommand(userId, idempotencyKey, request);
            var result = await _createFlightBookingHandler.HandleAsync(command, ct);

            return result.Status switch
            {
                CreateFlightBookingResultStatus.Success =>
                    Created($"/api/sales/bookings/flights/{result.Response!.Id}", result.Response),

                CreateFlightBookingResultStatus.ValidationError =>
                    ValidationProblem(new ValidationProblemDetails(result.Errors!)),

                CreateFlightBookingResultStatus.IdempotencyConflict =>
                    Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                    }),

                CreateFlightBookingResultStatus.InsufficientWallet =>
                    UnprocessableEntity(new ProblemDetails
                    {
                        Status = 422,
                        Title = "Unprocessable Entity",
                        Detail = "Insufficient wallet balance to perform this operation."
                    }),

                CreateFlightBookingResultStatus.HeadNotFound =>
                    UnprocessableEntity(new ProblemDetails
                    {
                        Status = 422,
                        Title = "Unprocessable Entity",
                        Detail = "Target customer head account not found or access denied."
                    }),

                _ => StatusCode(500, "An unexpected error occurred.")
            };
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Status = 500,
                Title = "Internal Server Error",
                Detail = ex.ToString()
            });
        }
    }

    // ─── Hotels ───────────────────────────────────────────────────────────────
    /// <summary>Lists hotel bookings for the authenticated salesman.</summary>
    [HttpGet("hotels")]
    [ProducesResponseType(typeof(PagedResponse<HotelBookingSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetHotels(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
        var result = await _salesReadService.GetHotelBookingsAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    /// <summary>Creates a new hotel booking with idempotency, concurrency, and financial checks.</summary>
    [HttpPost("hotels")]
    [ProducesResponseType(typeof(HotelBookingResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateHotel(
        [FromBody] CreateHotelBookingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Idempotency-Key header is required."
            });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var command = new CreateHotelBookingCommand(userId, idempotencyKey, request);
        var result = await _createHotelBookingHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateHotelBookingResultStatus.Success =>
                Created($"/api/sales/bookings/hotels/{result.Response!.Id}", result.Response),

            CreateHotelBookingResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateHotelBookingResultStatus.IdempotencyConflict =>
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateHotelBookingResultStatus.InsufficientWallet =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Insufficient wallet balance to perform this operation."
                }),

            CreateHotelBookingResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    // ─── Visas ────────────────────────────────────────────────────────────────
    /// <summary>Lists active visa countries configured in Master Accounts.</summary>
    [HttpGet("visas/countries")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<VisaCountryMasterResponse>), 200)]
    public async Task<IActionResult> GetVisaCountries(CancellationToken ct = default)
    {
        var result = await _agentsApiClient.GetVisaCountriesAsync(ct);
        return Ok(result);
    }

    /// <summary>Lists active visa packages configured in Master Accounts, optionally filtered by countryId.</summary>
    [HttpGet("visas/packages")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<VisaPackageMasterResponse>), 200)]
    public async Task<IActionResult> GetVisaPackages([FromQuery] long? countryId = null, CancellationToken ct = default)
    {
        var result = await _agentsApiClient.GetVisaPackagesAsync(countryId, ct);
        return Ok(result);
    }

    /// <summary>Lists visa applications for the authenticated salesman.</summary>
    [HttpGet("visas")]
    [ProducesResponseType(typeof(PagedResponse<VisaApplicationSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetVisas(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
        var result = await _salesReadService.GetVisaApplicationsAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    /// <summary>Creates a new visa booking application with applicants, idempotency, and financial checks.</summary>
    [HttpPost("visas")]
    [ProducesResponseType(typeof(VisaBookingResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateVisa(
        [FromBody] CreateVisaBookingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Idempotency-Key header is required."
            });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        if (request.Applicants != null && request.Applicants.Count > 0)
        {
            var updatedApplicants = new System.Collections.Generic.List<VisaApplicantRequest>();
            foreach (var app in request.Applicants)
            {
                var passport = app.PassportCopy;
                var iqama = app.IqamaHotelNo;
                var bankDoc = app.BankDoc;

                if (!string.IsNullOrWhiteSpace(passport) && passport.Contains("data:") && passport.Contains(";base64,"))
                {
                    passport = await Base64FileHelper.SaveFileAsync(passport, "passports", _env.WebRootPath, ct);
                }
                if (!string.IsNullOrWhiteSpace(iqama) && iqama.Contains("data:") && iqama.Contains(";base64,"))
                {
                    iqama = await Base64FileHelper.SaveFileAsync(iqama, "iqamas", _env.WebRootPath, ct);
                }
                if (!string.IsNullOrWhiteSpace(bankDoc) && bankDoc.Contains("data:") && bankDoc.Contains(";base64,"))
                {
                    bankDoc = await Base64FileHelper.SaveFileAsync(bankDoc, "bank_docs", _env.WebRootPath, ct);
                }

                updatedApplicants.Add(app with { PassportCopy = passport, IqamaHotelNo = iqama, BankDoc = bankDoc });
            }
            request = request with { Applicants = updatedApplicants };
        }

        var command = new CreateVisaBookingCommand(userId, idempotencyKey, request);
        var result = await _createVisaBookingHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateVisaBookingResultStatus.Success =>
                Created($"/api/sales/bookings/visas/{result.Response!.Id}", result.Response),

            CreateVisaBookingResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateVisaBookingResultStatus.IdempotencyConflict =>
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateVisaBookingResultStatus.InsufficientWallet =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = result.ConflictMessage ?? "Insufficient wallet balance to perform this operation."
                }),

            CreateVisaBookingResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    public record ApplicantDocReuploadRequest(
        long ApplicantId,
        string? PassportCopy,
        string? IqamaHotelNo,
        string? BankDoc
    );

    public record ReuploadVisaDocumentsRequest(
        System.Collections.Generic.List<ApplicantDocReuploadRequest> Applicants
    );

    /// <summary>Allows salesman to re-upload requested visa documents for applicants.</summary>
    [HttpPost("visas/{id:long}/reupload-documents")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> ReuploadVisaDocuments(
        [FromRoute] long id,
        [FromBody] ReuploadVisaDocumentsRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var db = HttpContext.RequestServices.GetRequiredService<TexasPortal.Application.Persistence.ISalesPortalDbContext>();
        var booking = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.SalesVisaApplications, x => x.Id == id && x.UserId == userId, ct);

        if (booking == null) return NotFound("Visa application not found or access denied.");

        if (request.Applicants != null && request.Applicants.Count > 0)
        {
            foreach (var item in request.Applicants)
            {
                var applicant = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    db.SalesVisaApplicants, a => a.Id == item.ApplicantId && a.SalesVisaApplicationId == id, ct);

                if (applicant != null)
                {
                    if (!string.IsNullOrWhiteSpace(item.PassportCopy))
                    {
                        var path = await Base64FileHelper.SaveFileAsync(item.PassportCopy, "passports", _env.WebRootPath, ct);
                        if (!string.IsNullOrWhiteSpace(path)) applicant.PassportCopy = path;
                    }
                    if (!string.IsNullOrWhiteSpace(item.IqamaHotelNo))
                    {
                        var path = await Base64FileHelper.SaveFileAsync(item.IqamaHotelNo, "iqamas", _env.WebRootPath, ct);
                        if (!string.IsNullOrWhiteSpace(path)) applicant.IqamaHotelNo = path;
                    }
                    if (!string.IsNullOrWhiteSpace(item.BankDoc))
                    {
                        var path = await Base64FileHelper.SaveFileAsync(item.BankDoc, "bank_docs", _env.WebRootPath, ct);
                        if (!string.IsNullOrWhiteSpace(path)) applicant.BankDoc = path;
                    }
                    applicant.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        booking.ProcessingStatus = "pending";
        booking.DocumentRequestNote = null;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok();
    }

    // ─── VFS Appointments ─────────────────────────────────────────────────────
    /// <summary>Lists VFS appointments for the authenticated salesman.</summary>
    [HttpGet("vfs")]
    [ProducesResponseType(typeof(PagedResponse<VfsAppointmentSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetVfsAppointments(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null,
        [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
        var result = await _salesReadService.GetVfsAppointmentsAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    // ─── Holiday Packages ─────────────────────────────────────────────────────
    /// <summary>Lists holiday packages for the authenticated salesman.</summary>
    [HttpGet("holidays")]
    [ProducesResponseType(typeof(PagedResponse<HolidayPackageSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetHolidayPackages(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
var result = await _salesReadService.GetHolidayPackagesAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    // ─── Transportations ─────────────────────────────────────────────────────
    /// <summary>Lists transportation bookings for the authenticated salesman.</summary>
    [HttpGet("transport")]
    [ProducesResponseType(typeof(PagedResponse<TransportationSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetTransportations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
var result = await _salesReadService.GetTransportationsAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    // ─── Other Services ───────────────────────────────────────────────────────
    /// <summary>Lists other services for the authenticated salesman.</summary>
    [HttpGet("other")]
    [ProducesResponseType(typeof(PagedResponse<OtherServiceSummaryResponse>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401)]
    public async Task<IActionResult> GetOtherServices(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null, [FromQuery] string? processingStatus = null,
        [FromQuery] string? searchTerm = null, [FromQuery] string? search = null, [FromQuery] bool? isDealClosed = null, [FromQuery] string? dealStatus = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid paging parameters.");
        var term = searchTerm ?? search;
        bool? effectiveDealClosed = isDealClosed;
        if (!effectiveDealClosed.HasValue && !string.IsNullOrWhiteSpace(dealStatus)) {
            if (dealStatus.Equals("non_deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("open", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = false;
            else if (dealStatus.Equals("deal_closed", StringComparison.OrdinalIgnoreCase) || dealStatus.Equals("closed", StringComparison.OrdinalIgnoreCase)) effectiveDealClosed = true;
        }
        var result = await _salesReadService.GetOtherServicesAsync(userId, new(page, pageSize, paymentStatus, processingStatus, term, effectiveDealClosed), ct);
        return Ok(result);
    }

    /// <summary>Creates a new other service booking with idempotency, concurrency, and financial checks.</summary>
    [HttpPost("other")]
    [HttpPost("other-services")]
    [ProducesResponseType(typeof(OtherServiceBookingResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateOtherService(
        [FromBody] CreateOtherServiceBookingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Idempotency-Key header is required."
            });
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var command = new CreateOtherServiceBookingCommand(userId, idempotencyKey, request);
        var result = await _createOtherServiceBookingHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateOtherServiceBookingResultStatus.Success => 
                Created($"/api/sales/bookings/other/{result.Response!.Id}", result.Response),

            CreateOtherServiceBookingResultStatus.ValidationError => 
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateOtherServiceBookingResultStatus.IdempotencyConflict => 
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateOtherServiceBookingResultStatus.InsufficientWallet => 
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Insufficient wallet balance to perform this operation."
                }),

            CreateOtherServiceBookingResultStatus.HeadNotFound => 
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    /// <summary>Creates a new VFS appointment booking with idempotency, concurrency, and financial checks.</summary>
    [HttpPost("vfs")]
    [HttpPost("vfs-appointments")]
    [ProducesResponseType(typeof(VfsAppointmentResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateVfs(
        [FromBody] CreateVfsAppointmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = "Idempotency-Key header is required." });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var command = new CreateVfsAppointmentCommand(userId, idempotencyKey, request);
        var result = await _createVfsAppointmentHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateVfsAppointmentResultStatus.Success =>
                Created($"/api/sales/bookings/vfs/{result.Response!.Id}", result.Response),

            CreateVfsAppointmentResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateVfsAppointmentResultStatus.IdempotencyConflict =>
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateVfsAppointmentResultStatus.InsufficientWallet =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = result.ConflictMessage ?? "Insufficient wallet balance to perform this operation."
                }),

            CreateVfsAppointmentResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    /// <summary>Creates a new Holiday Package booking with idempotency, concurrency, and financial checks.</summary>
    [HttpPost("holidays")]
    [HttpPost("holiday-packages")]
    [ProducesResponseType(typeof(HolidayPackageResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateHoliday(
        [FromBody] CreateHolidayPackageRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = "Idempotency-Key header is required." });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var command = new CreateHolidayPackageCommand(userId, idempotencyKey, request);
        var result = await _createHolidayPackageHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateHolidayPackageResultStatus.Success =>
                Created($"/api/sales/bookings/holidays/{result.Response!.Id}", result.Response),

            CreateHolidayPackageResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateHolidayPackageResultStatus.IdempotencyConflict =>
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateHolidayPackageResultStatus.InsufficientWallet =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = result.ConflictMessage ?? "Insufficient wallet balance to perform this operation."
                }),

            CreateHolidayPackageResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    /// <summary>Creates a new Transportation booking with idempotency, concurrency, and financial checks.</summary>
    [HttpPost("transportations")]
    [HttpPost("transport")]
    [ProducesResponseType(typeof(TransportationResponse), 201)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(409), ProducesResponseType(422)]
    public async Task<IActionResult> CreateTransportation(
        [FromBody] CreateTransportationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = "Idempotency-Key header is required." });

        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(request.PaymentProof))
        {
            try
            {
                var savedPath = await Base64FileHelper.SavePaymentProofAsync(request.PaymentProof, _env.WebRootPath, ct);
                request = request with { PaymentProof = savedPath };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = ex.Message });
            }
        }

        var command = new CreateTransportationCommand(userId, idempotencyKey, request);
        var result = await _createTransportationHandler.HandleAsync(command, ct);

        return result.Status switch
        {
            CreateTransportationResultStatus.Success =>
                Created($"/api/sales/bookings/transportations/{result.Response!.Id}", result.Response),

            CreateTransportationResultStatus.ValidationError =>
                ValidationProblem(new ValidationProblemDetails(result.Errors!)),

            CreateTransportationResultStatus.IdempotencyConflict =>
                Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = result.ConflictMessage ?? "A concurrency or idempotency error occurred."
                }),

            CreateTransportationResultStatus.InsufficientWallet =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = result.ConflictMessage ?? "Insufficient wallet balance to perform this operation."
                }),

            CreateTransportationResultStatus.HeadNotFound =>
                UnprocessableEntity(new ProblemDetails
                {
                    Status = 422,
                    Title = "Unprocessable Entity",
                    Detail = "Target customer head account not found or access denied."
                }),

            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    /// <summary>Gets details for a specific other service booking owned by the authenticated salesman.</summary>
    [HttpGet("other/{id}")]
    [ProducesResponseType(typeof(OtherServiceDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetOtherServiceDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _salesReadService.GetOtherServiceByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific flight booking owned by the authenticated salesman.</summary>
    [HttpGet("flights/{id}")]
    [ProducesResponseType(typeof(FlightBookingDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetFlightDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetFlightBookingByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific hotel booking owned by the authenticated salesman.</summary>
    [HttpGet("hotels/{id}")]
    [ProducesResponseType(typeof(HotelBookingDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetHotelDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetHotelBookingByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific visa application owned by the authenticated salesman.</summary>
    [HttpGet("visas/{id}")]
    [ProducesResponseType(typeof(VisaApplicationDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetVisaDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetVisaApplicationByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific VFS appointment owned by the authenticated salesman.</summary>
    [HttpGet("vfs/{id}")]
    [ProducesResponseType(typeof(VfsAppointmentDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetVfsDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetVfsAppointmentByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific holiday package owned by the authenticated salesman.</summary>
    [HttpGet("holidays/{id}")]
    [ProducesResponseType(typeof(HolidayPackageDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetHolidayDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetHolidayPackageByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gets details for a specific transportation booking owned by the authenticated salesman.</summary>
    [HttpGet("transport/{id}")]
    [ProducesResponseType(typeof(TransportationDetailResponse), 200)]
    [ProducesResponseType(401), ProducesResponseType(404)]
    public async Task<IActionResult> GetTransportationDetails([FromRoute] string id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _salesReadService.GetTransportationByIdAsync(userId, id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
