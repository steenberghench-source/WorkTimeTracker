using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTimeTracker.Storage
{
    public class WeekDto
    {
        public DateTime WeekStart { get; set; }
        public List<DagUrenDto> Dagen { get; set; } = new();
    }
}
