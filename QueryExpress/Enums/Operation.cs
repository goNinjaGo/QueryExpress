using System;
using System.Collections.Generic;
using System.Text;

namespace QueryExpress.Enums
{
    public enum Operation
    {
        Equals, 
        NotEquals, 
        Between,
        LessThan, 
        LessThanOrEqual, 
        GreaterThan, 
        GreaterThanOrEqual, 
        Contains, 
        DoesNotContain,
        StartsWith, 
        EndsWith
    }
}
