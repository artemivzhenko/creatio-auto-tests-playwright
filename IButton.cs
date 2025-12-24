using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatioAutoTestsPlaywright
{
    public interface IButton
    {
        string Title { get; }
        string Code { get; }

        Task<bool> CheckIfExistAsync(bool debug = false);
        bool CheckIfExist(bool debug = false);

        Task ClickAsync(bool debug = false);
        void Click(bool debug = false);
    }
}
