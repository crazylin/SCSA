using System.Collections.Generic;
using SCSA.Models;

namespace SCSA.ViewModels.Messages;

public sealed class RequestWriteParametersMessage(List<Parameter> parameters)
{
    public List<Parameter> Parameters { get; } = parameters;
}