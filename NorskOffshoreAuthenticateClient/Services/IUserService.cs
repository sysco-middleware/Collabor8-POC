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


        Task<UserItem> GetAsync(int id);
        Task<IEnumerable<string>> GetAllGraphUsersAsync();
        Task<UserItem> GetLoggedInUser();

    }
}
