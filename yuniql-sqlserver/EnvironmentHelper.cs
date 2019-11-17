﻿using System;

namespace Yuniql.SqlServer
{
    public static class EnvironmentHelper
    {
        public static string GetEnvironmentVariable(string name)
        {
            string result = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(result) && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                result = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
                if (string.IsNullOrEmpty(result))
                    result = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            }

            return result;
        }
    }
}