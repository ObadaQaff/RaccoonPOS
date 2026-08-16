using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum InvoiceStatus
{
    Draft = 0,        // POS open
    Completed = 1,    // Payment selected
    Posted = 2,       // Finalized
    Cancelled = 3,
    Returned = 4,
    OnHold = 5,
    Unknown = 6,      // Endpoint order received
    InProcess = 7     // Endpoint order preparation started
}
