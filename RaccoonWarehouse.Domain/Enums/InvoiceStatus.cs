using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum InvoiceStatus
{
    Draft,        // POS open
    Completed,    // Payment selected
    Posted,       // Finalized
    Cancelled,
    Returned,
    OnHold
}
