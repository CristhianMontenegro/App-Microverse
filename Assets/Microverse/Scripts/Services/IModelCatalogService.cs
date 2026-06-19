using System.Collections.Generic;
using Microverse.Data;

namespace Microverse.Services
{
    public interface IModelCatalogService
    {
        IReadOnlyList<BiologicalModel> GetModels();
    }
}
