using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EzyTaskin.Data.Db;

[PrimaryKey($"{nameof(Provider)}Id", $"{nameof(Category)}Id")]
public class ProviderCategory
{
    [Required]
    public required Provider Provider { get; set; }

    [Required]
    public required Category Category { get; set; }
}
