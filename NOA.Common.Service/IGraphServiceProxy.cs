using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOA.Common.Service
{
    public interface IGraphServiceProxy
    {
        Task<List<DirectoryObject>> GetGroupMembers(string groupId);
        Task<bool> AddUserToGroup(string userMail, string groupId);
        Task<User?> GetGraphApiUser(string filter);
        Task<List<string>> GetAllGraphApiUsers();
        Task<Invitation?> InviteUser(string email, string redirectUrl);
    }
}
