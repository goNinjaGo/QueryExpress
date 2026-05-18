namespace QueryExpress.Tests.Data.Models;

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal? LitersUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsEligibile { get; set; }
    public bool? IsUtilized { get; set; }
}
