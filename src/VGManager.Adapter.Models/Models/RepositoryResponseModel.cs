using System.ComponentModel.DataAnnotations;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Models.Models;

public class RepositoryResponseModel<T>
{
    [Required]
    public RepositoryStatus Status { get; set; }

    [Required]
    public IEnumerable<T> Data { get; set; } = null!;
}
