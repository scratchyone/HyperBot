using System;
namespace HyperBot
{
    class UserError : Exception
    {
        public UserError(string message) : base(message)
        {
        }
    }
}