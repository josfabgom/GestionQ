using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Data;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace GestionQ.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PosAuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PosAuthController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private async Task<PointOfSale> GetOrCreatePosAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) identifier = "UNKNOWN-POS";
            var pos = await _context.PointsOfSale.FirstOrDefaultAsync(p => p.PosIdentifier == identifier);
            if (pos == null)
            {
                pos = new PointOfSale
                {
                    Name = "Caja " + identifier,
                    PosNumber = _context.PointsOfSale.Max(p => (int?)p.PosNumber) + 1 ?? 1,
                    PosIdentifier = identifier,
                    MachineName = identifier,
                    IsActive = true,
                    SyncOnlyWithStock = true,
                    SyncCustomers = true
                };
                _context.PointsOfSale.Add(pos);
                await _context.SaveChangesAsync();
            }
            return pos;
        }

        [HttpPost("login")]
        public async Task<ActionResult<PosLoginResponseDto>> Login([FromBody] PosLoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Pin))
            {
                return Ok(new PosLoginResponseDto { Success = false, ErrorMessage = "PIN requerido." });
            }

            var users = await _userManager.Users.ToListAsync();
            IdentityUser? matchedUser = null;

            foreach (var user in users)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var pinClaim = claims.FirstOrDefault(c => c.Type == "UserPin");
                if (pinClaim != null && pinClaim.Value == request.Pin)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Cajero") || roles.Contains("Vendedor") || roles.Contains("Admin"))
                    {
                        matchedUser = user;
                        break;
                    }
                }
            }

            if (matchedUser == null)
            {
                return Ok(new PosLoginResponseDto { Success = false, ErrorMessage = "PIN incorrecto o usuario sin permisos." });
            }

            var matchedUserClaims = await _userManager.GetClaimsAsync(matchedUser);
            var fullNameClaim = matchedUserClaims.FirstOrDefault(c => c.Type == "FullName");

            return Ok(new PosLoginResponseDto
            {
                Success = true,
                UserId = matchedUser.Id,
                UserName = matchedUser.UserName,
                FullName = fullNameClaim?.Value
            });
        }

        [HttpPost("open-register")]
        public async Task<ActionResult<PosOpenRegisterResponseDto>> OpenRegister([FromBody] PosOpenRegisterRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return Ok(new PosOpenRegisterResponseDto { Success = false, ErrorMessage = "Usuario no válido." });

            var pos = await GetOrCreatePosAsync(request.PosIdentifier);

            var userOpenRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClosingDate == null);

            if (userOpenRegister != null)
            {
                return Ok(new PosOpenRegisterResponseDto { Success = false, ErrorMessage = "Ya tienes una caja abierta en otra terminal." });
            }

            var posOpenRegister = await _context.CashRegisters
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.PointOfSaleId == pos.Id && c.ClosingDate == null);

            if (posOpenRegister != null)
            {
                return Ok(new PosOpenRegisterResponseDto { Success = false, ErrorMessage = $"Este Punto de Venta ya tiene una caja abierta por el usuario {posOpenRegister.User?.UserName}." });
            }

            var newRegister = new CashRegister
            {
                UserId = user.Id,
                PointOfSaleId = pos.Id,
                InitialBalance = request.InitialBalance,
                OpeningDate = DateTime.Now
            };

            _context.CashRegisters.Add(newRegister);
            await _context.SaveChangesAsync();

            return Ok(new PosOpenRegisterResponseDto
            {
                Success = true,
                CashRegisterId = newRegister.Id
            });
        }

        [HttpPost("add-movement")]
        public async Task<IActionResult> AddMovement([FromBody] AddMovementDto request)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId);

            if (register == null) return NotFound(new { error = "Caja no encontrada" });

            if (request.Type == "Egreso")
            {
                decimal totalEfectivoVentas = register.Sales
                    .Where(s => !s.IsCancelled)
                    .SelectMany(s => s.Payments)
                    .Where(p => p.PaymentMethod != null && p.PaymentMethod.Name == "Efectivo")
                    .Sum(p => p.Amount);
                
                decimal totalIngresos = register.Movements.Where(m => m.Type == "Ingreso").Sum(m => m.Amount);
                decimal totalEgresos = register.Movements.Where(m => m.Type == "Egreso").Sum(m => m.Amount);

                decimal expectedCash = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;

                if (request.Amount > expectedCash)
                {
                    return BadRequest(new { error = $"Fondos insuficientes. Efectivo disponible en caja: $ {expectedCash:N2}" });
                }
            }

            var movement = new GestionQ.Domain.Entities.CashRegisterMovement
            {
                CashRegisterId = request.CashRegisterId,
                Amount = request.Amount,
                Description = request.Description,
                Type = request.Type,
                Date = DateTime.Now
            };

            _context.CashRegisterMovements.Add(movement);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, movementId = movement.Id });
        }

        [HttpPost("close-register")]
        public async Task<IActionResult> CloseRegister([FromBody] PosCloseRegisterRequestDto request)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(c => c.Id == request.CashRegisterId);

            if (register == null || register.ClosingDate != null)
            {
                return Ok(new { success = false, errorMessage = "Caja no encontrada o ya cerrada." });
            }

            decimal totalEfectivoVentas = register.Sales
                .SelectMany(s => s.Payments)
                .Where(p => p.PaymentMethod?.Name == "Efectivo")
                .Sum(p => p.Amount);

            decimal totalIngresos = register.Movements
                .Where(m => m.Type == "Ingreso")
                .Sum(m => m.Amount);

            decimal totalEgresos = register.Movements
                .Where(m => m.Type == "Egreso")
                .Sum(m => m.Amount);

            register.ExpectedCashBalance = register.InitialBalance + totalEfectivoVentas + totalIngresos - totalEgresos;
            register.FinalCashBalance = request.FinalCashBalance;
            register.Difference = request.FinalCashBalance - register.ExpectedCashBalance;
            register.ClosingDate = DateTime.Now;

            _context.CashRegisters.Update(register);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("print-register/{id}")]
        public async Task<IActionResult> PrintRegister(int id)
        {
            var register = await _context.CashRegisters
                .Include(c => c.Movements)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.VatRate)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.ElectronicInvoice)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (register == null) return NotFound();

            var ticketText = GestionQ.Web.Services.CashRegisterTicketGenerator.GenerateXReport(register);
            return Ok(new { success = true, ticketText = ticketText });
        }

        [HttpGet("status/{posIdentifier}")]
        public async Task<ActionResult<PosStatusResponseDto>> GetStatus(string posIdentifier)
        {
            var pos = await _context.PointsOfSale.FirstOrDefaultAsync(p => p.PosIdentifier == posIdentifier);
            if (pos == null)
            {
                return Ok(new PosStatusResponseDto { HasOpenRegister = false });
            }

            var openRegister = await _context.CashRegisters
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.PointOfSaleId == pos.Id && c.ClosingDate == null);

            if (openRegister != null)
            {
                var claims = await _userManager.GetClaimsAsync(openRegister.User);
                var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");

                return Ok(new PosStatusResponseDto
                {
                    HasOpenRegister = true,
                    CashRegisterId = openRegister.Id,
                    UserId = openRegister.UserId,
                    UserName = openRegister.User?.UserName,
                    FullName = fullNameClaim?.Value
                });
            }

            return Ok(new PosStatusResponseDto { HasOpenRegister = false });
        }
    }
}
