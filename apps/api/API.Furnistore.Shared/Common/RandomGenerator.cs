using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Furnistore.Shared.Common
{
    public static class RandomGenerator
    {
        public static string GenerateRandomString(int size)
        {
            var random = new Random();

            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz$#-_.";

            return new string(
                Enumerable.Repeat(chars, size).Select(s => s[random.Next(s.Length)]).ToArray()
            );
        }
    }
}
