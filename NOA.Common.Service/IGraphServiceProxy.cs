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
        Task<bool> RemoveUserFromGroup(string userMail, string groupId);
        Task<List<DirectoryObject>> GetGroupMembers(string groupId);
        Task<bool> AddUserToGroup(string userMail, string groupId);
        Task<User?> GetGraphApiUser(string filter);
        Task<List<string>> GetAllUsersUPN();
        Task<List<User>> GetAllUsers();
        Task<Invitation?> InviteUser(string email, string redirectUrl);
    }
}
