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
        Task<User> GetGraphApiUser(string filter);
        Task<List<string>> GetAllGraphApiUsers();
    }
}
