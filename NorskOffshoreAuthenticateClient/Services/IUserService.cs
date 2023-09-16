using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NorskOffshoreAuthenticateClient.Models;
using Microsoft.Graph.Models;
using System.Security.Claims;
using NOA.Common.Constants;

namespace NorskOffshoreAuthenticateClient.Services
{
    public interface IUserService
    {
        Task<UserItem> GetLoggedInUser();
        Task<bool> AuthenticateUser(string email);
        Task<UserStatus> GetUserStatus(string email);

    }
}
