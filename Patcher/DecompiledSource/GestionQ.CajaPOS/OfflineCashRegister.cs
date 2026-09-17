using System;

namespace GestionQ.CajaPOS;

public class OfflineCashRegister
{
	public int Id { get; set; }

	public Guid GlobalId { get; set; } = Guid.NewGuid();


	public string UserId { get; set; } = string.Empty;


	public DateTime OpeningDate { get; set; } = DateTime.Now;


	public DateTime? ClosingDate { get; set; }

	public decimal InitialBalance { get; set; }

	public decimal? FinalCashBalance { get; set; }

	public bool IsSynced { get; set; }

	public int? ServerCashRegisterId { get; set; }
}
