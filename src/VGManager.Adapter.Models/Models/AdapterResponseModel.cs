using System.ComponentModel.DataAnnotations;
using VGManager.Adapter.Models.StatusEnums;

namespace VGManager.Adapter.Models.Models;

public class AdapterResponseModel<T>
{
    [Required]
    public AdapterStatus Status { get; set; }

    [Required]
    public T Data { get; set; } = default!;
}

public class AdapterResponseModel<T, K>
{
    [Required]
    public AdapterStatus Status { get; set; }

    [Required]
    public T Data { get; set; } = default!;

    [Required]
    public K AdditionalData { get; set; } = default!;
}
