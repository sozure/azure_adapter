using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VGManager.Adapter.Models.Requests;

public class MultipleReleasePipelineRequest : BaseRequest
{
    public IEnumerable<string> Projects { get; set; } = null!;
    public string RepositoryName { get; set; } = null!;
    public string ConfigFile { get; set; } = null!;
}
