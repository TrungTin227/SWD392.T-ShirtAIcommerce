using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITranslateService
    {
        Task<string> TranslateAsync(string text, string targetLang = "en");
    }
}
