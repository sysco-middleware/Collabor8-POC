﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOA.Common.Constants
{
    public enum AddToGroupStatus
    {        
        Success,
        Failed,
        AlreadyMember,
        MissingParameters,
        PrerequisitesFailed
    }
}
