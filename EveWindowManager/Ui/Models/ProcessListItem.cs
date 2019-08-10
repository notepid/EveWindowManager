using System.Diagnostics;

namespace EveWindowManager.Ui.Models
{
    public class ProcessListItem
    {
        public Process Process { get; set; }
        public bool IsSaved { get; set; }
        public bool HasBeenRestored { get; set; }
    }
}
