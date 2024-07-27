using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reserve.API.Model;

public class ReserveItem
{
    public int Id { get; set; }

    public bool Game { get; set; }

    [Column(TypeName = "date")]
    [Required(ErrorMessage = "Datum ist erforderlich")]
    public DateTime DateReservation { get; set; }

    [Column(TypeName = "time")]
    [Required(ErrorMessage = "Uhrzeit ist erforderlich")]
    public TimeSpan TimeReservation { get; set; }

    public bool EatAndPlay { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(500, ErrorMessage = "Name darf nicht länger als 100 Zeichen sein")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Nachname ist erforderlich")]
    [StringLength(500, ErrorMessage = "Nachname darf nicht länger als 100 Zeichen sein")]
    public string Surname { get; set; } = null!;

    [Required(ErrorMessage = "Telefonnummer ist erforderlich")]
    public string PhoneNumber { get; set; } = null!;

    [Range(1, 6, ErrorMessage = "Anzahl der Personen muss zwischen 1 und 6 liegen")]
    public int CountPersons { get; set; }

    [StringLength(500, ErrorMessage = "Nachricht darf nicht länger als 500 Zeichen sein")]
    public string? Message { get; set; }
    
}