using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkTimeTracker.ViewModels;

namespace WorkTimeTracker.Storage
{
    public class DagUrenDto
    {
        public DateTime Datum { get; set; }
        public TimeSpan? StartTijd { get; set; }
        public TimeSpan? EindTijd { get; set; }
        public string? Projectnaam { get; set; }
        public string? ExtraInformatie { get; set; }
        public string? Locatie { get; set; }
        public DagStatus Status { get; set; }
        public bool? MagInvoeren { get; set; }
        public bool? AllesAlsOveruren { get; set; }
    }
}
