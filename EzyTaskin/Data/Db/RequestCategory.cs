using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Data.Db;

[PrimaryKey($"{nameof(Request)}Id", $"{nameof(Category)}Id")]
public class RequestCategory
{
    [Required]
    public required Request Request { get; set; }

    [Required]
    public required Category Category { get; set; }
}
