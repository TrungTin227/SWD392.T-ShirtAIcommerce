using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IAiImageService
    {
        Task<string> GenerateDesignImageAsync(string prompt);
    }
}
