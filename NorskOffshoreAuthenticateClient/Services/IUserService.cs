using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NorskOffshoreAuthenticateClient.Models;
using Microsoft.Graph.Models;
using System.Security.Claims;

namespace NorskOffshoreAuthenticateClient.Services
{
    public interface IUserService
    {
        Task<UserItem> GetLoggedInUser();
        Task<String> AuthenticateUser(string email);
        Task<String> GetUserStatus(string email);

    }
}
