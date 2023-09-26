﻿using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using System.Security.Claims;
using NOA.Common.Constants;
using NOA.Common.Service.Model;

namespace NOA.Common.Service
{
    public interface IUserService
    {
        Task<UserItem> GetLoggedInUser();
        Task<bool> CanAuthenticateUser(string email);
        Task<UserStatus> GetUserStatus(string email);
        Task<bool> InviteUser(string emailAddress);
    }
}
